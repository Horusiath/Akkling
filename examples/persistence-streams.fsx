#load "../.paket/load/netstandard2.0/Akka.Serialization.Hyperion.fsx"
#load "../.paket/load/netstandard2.0/Akka.Persistence.fsx"
#load "../.paket/load/netstandard2.0/Akka.Streams.fsx"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../src/Akkling.Streams/bin/Debug/netstandard2.0/Akka.Streams.dll"
#r "../src/Akkling.Streams/bin/Debug/netstandard2.0/Akkling.Streams.dll"
#r "../src/Akkling.Persistence/bin/Debug/netstandard2.0/Akkling.Persistence.dll"


open System
open Akka.Streams
open Akka.Streams.Dsl
open Reactive.Streams
open Akka.Persistence
open Newtonsoft

open Akkling
open Akkling.Persistence
open Akkling.Streams

let configWith() =
    let config = Configuration.parse("""
        akka {
            persistence {
                view.auto-update-interval = 100

                query.journal.sql {
                  # Implementation class of the SQL ReadJournalProvider
                  class = "Akka.Persistence.Query.Sql.SqlReadJournalProvider, Akka.Persistence.Query.Sql"

                  # Absolute path to the write journal plugin configuration entry that this
                  # query journal will connect to.
                  # If undefined (or "") it will connect to the default journal as specified by the
                  # akka.persistence.journal.plugin property.
                  write-plugin = ""

                  # The SQL write journal is notifying the query side as soon as things
                  # are persisted, but for efficiency reasons the query side retrieves the events
                  # in batches that sometimes can be delayed up to the configured `refresh-interval`.
                  refresh-interval = 1s

                  # How many events to fetch in one query (replay) and keep buffered until they
                  # are delivered downstreams.
                  max-buffer-size = 100
                }
            }
        }
        """)
    config.WithFallback <| Configuration.defaultConfig()


let system = System.create "persisting-streams-sys" <| configWith()
let mat = system.Materializer()

type CounterChanged =
    { Delta : int }

type CounterCommand =
    | Inc
    | Dec
    | GetState

type CounterMessage =
    | Command of CounterCommand
    | Event of CounterChanged

let persistActor (queue: ISourceQueue<CounterChanged>) =
    propsPersist(fun mailbox ->
        let rec loop state =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | Event(changed) ->
                    queue.AsyncOffer(changed) |!> retype mailbox.Self
                    return! loop (state + changed.Delta)
                | Command(cmd) ->
                    match cmd with
                    | GetState ->
                        mailbox.Sender() <! state
                        return! loop state
                    | Inc -> return Persist (Event { Delta = 1 })
                    | Dec -> return Persist (Event { Delta = -1 })
            }
        loop 0)

let persistentQueue<'T> system pid (overflowStrategy: OverflowStrategy) (maxBuffer: int) =
    Source.queue overflowStrategy maxBuffer
    |> Source.mapMaterializedValue(persistActor >> spawn system pid)

let source = persistentQueue<CounterChanged> system "pa1" OverflowStrategy.DropNew 1000

let actorRef, arr = async {
                        return source
                                |> Source.toMat (Sink.forEach(printfn "Piu: %A")) Keep.both
                                |> Graph.run mat
                    }
                    |> Async.RunSynchronously

arr |> Async.Start

let persistView pid (queue: ISourceQueue<obj>) =
    propsView pid (fun mailbox ->
        let rec loop state =
            actor {
                let! msg = mailbox.Receive()
                match msg with
                | Event(changed) ->
                    queue.AsyncOffer(changed) |!> retype mailbox.Self
                    return! loop (state + changed.Delta)
                | Command(cmd) ->
                    match cmd with
                    | GetState ->
                        mailbox.Sender() <! state
                        return! loop state
                    | _ -> return! loop (state) // ignoring
            }
        loop 0)

let persistentViewQueue system pid (overflowStrategy: OverflowStrategy) (maxBuffer: int) =
    Source.queue overflowStrategy maxBuffer
    |> Source.mapMaterializedValue(persistView pid >> spawnAnonymous system)

let sourceView = persistentViewQueue system "pa1" OverflowStrategy.DropNew 1000

let aa, aar = async {
                    return sourceView
                            |> Source.toMat (Sink.forEach(printfn "Piu2: %A")) Keep.both
                            |> Graph.run mat
                }
                |> Async.RunSynchronously

aar |> Async.Start

retype aa <! (Akka.Persistence.Update true)


actorRef <! Command Inc
actorRef <! Command Inc
actorRef <! Command Dec
async { let! reply = actorRef <? Command GetState
        printfn "Current state of %A: %i" actorRef reply } |> Async.RunSynchronously

async { let! reply = aa <? Command GetState
        printfn "Current state of %A: %i" actorRef reply } |> Async.RunSynchronously

