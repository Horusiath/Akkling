#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../src/Akkling.Streams/bin/Debug/Reactive.Streams.dll"
#r "../src/Akkling.Streams/bin/Debug/Akka.Streams.dll"
#r "../src/Akkling.Streams/bin/Debug/Akkling.Streams.dll"
#r "../src/Akkling.Streams/bin/Debug/System.Collections.Immutable.dll"

open System
open Akka.Streams
open Akka.Streams.Dsl
open Akkling
open Akkling.Streams

let text = """
       Lorem Ipsum is simply dummy text of the printing and typesetting industry.
       Lorem Ipsum has been the industry's standard dummy text ever since the 1500s,
       when an unknown printer took a galley of type and scrambled it to make a type
       specimen book."""

let system = System.create "streams-sys" <| Configuration.defaultConfig()
let mat = system.Materializer()

Source.ofArray (text.Split())
|> Source.map (fun x -> x.ToUpper())
|> Source.filter (String.IsNullOrWhiteSpace >> not)
|> Source.runForEach mat (printfn "%s")
|> Async.RunSynchronously

let behavior targetRef (m:Actor<_>) =
    let rec loop () = actor {
        let! msg = m.Receive ()
        targetRef <! msg
        return! loop ()
    }
    loop ()

let spawnActor targetRef =
    spawnAnonymous system <| props (behavior targetRef)
let s = Source.actorRef OverflowStrategy.DropNew 1000
        |> Source.mapMaterializedValue(spawnActor)
        |> Source.toMat(Sink.forEach(fun s -> printfn "Received: %s" s)) Keep.left
        |> Graph.run mat

s <! "Boo"
