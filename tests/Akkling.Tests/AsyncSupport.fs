﻿//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.AsyncSupport

open Akkling
open Akkling.TestKit
open Akka.Actor
open System
open Xunit
open Akkling.Persistence
open System.Threading.Tasks

[<Fact>]
let ``actor builder supports bind to async`` () =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| props (fun ctx ->
                let rec loop state =
                    actor {
                        let! msg = ctx.Receive()
                        let! newState = async { return state + 1 }
                        let expected = state
                        ctx.Sender() <! newState
                        return! loop newState
                    }

                loop 1)

        ref <! ""
        ref <! ""
        ref <! ""
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectMsg tck 4 |> ignore

[<Fact>]
let ``actor supports multiple async computations`` () : unit =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| props (fun ctx ->
                let rec loop () =
                    actor {
                        let! _ = ctx.Receive()
                        ctx.Sender() <! 0
                        do! Async.Sleep 1
                        ctx.Sender() <! 1
                        do! Async.Sleep 1
                        let! x = async { return 2 }
                        ctx.Sender() <! x
                        do! Async.Sleep 1
                        let! x = async { return 3 }
                        ctx.Sender() <! x
                        return! loop ()
                    }

                loop ())

        ref <! ""
        expectMsg tck 0 |> ignore
        expectMsg tck 1 |> ignore
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``persistentActor builder supports bind to async`` () =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| propsPersist (fun ctx ->
                let rec loop state =
                    actor {
                        let! msg = ctx.Receive()
                        let! newState = async { return state + 1 }
                        let expected = state
                        ctx.Sender() <! newState
                        return! loop newState
                    }

                loop 1)

        ref <! ""
        ref <! ""
        ref <! ""
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectMsg tck 4 |> ignore

[<Fact>]
let ``persistentActor supports multiple async computations`` () : unit =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| propsPersist (fun ctx ->
                let rec loop () =
                    actor {
                        let! _ = ctx.Receive()
                        ctx.Sender() <! 0
                        do! Async.Sleep 1
                        ctx.Sender() <! 1
                        do! Task.Delay 1
                        let! x = async { return 2 }
                        ctx.Sender() <! x
                        do! Async.Sleep 1
                        let! x = async { return 3 }
                        do! Task.Delay 1
                        ctx.Sender() <! x
                        return! loop ()
                    }

                loop ())

        ref <! ""
        expectMsg tck 0 |> ignore
        expectMsg tck 1 |> ignore
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``persistentActor async continuation can access actor properties`` () : unit =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| propsPersist (fun ctx ->
                let rec loop () =
                    actor {
                        let! _ = ctx.Receive()
                        do! Async.Sleep 1
                        ctx.Sender() <! ctx.Pid
                        return! loop ()
                    }

                loop ())

        ref <! ""
        expectMsg tck "actor" |> ignore
        expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``persistentActor supports persist after async computations`` () : unit =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| propsPersist (fun ctx ->
                let rec loop () =
                    actor {
                        let! msg = ctx.Receive()

                        if ctx.HasPersisted() then
                            return! loop ()
                        else
                            ctx.Sender() <! 0
                            do! Async.Sleep 1
                            ctx.Sender() <! 1
                            do! Async.Sleep 1000
                            ctx.Sender() <! 2
                            return PersistAsync msg |> Effects.andThen (fun () -> ctx.Sender() <! 3)
                    }

                loop ())

        ref <! ""
        expectMsg tck 0 |> ignore
        expectMsg tck 1 |> ignore
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``persistentActor supports persist after task computations`` () : unit =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| propsPersist (fun ctx ->
                let rec loop () =
                    actor {
                        let! msg = ctx.Receive()

                        if ctx.HasPersisted() then
                            return! loop ()
                        else
                            ctx.Sender() <! 0
                            do! Task.Delay 1
                            ctx.Sender() <! 1
                            do! Task.Delay 1000
                            ctx.Sender() <! 2
                            return PersistAsync msg |> Effects.andThen (fun () -> ctx.Sender() <! 3)
                    }

                loop ())

        ref <! ""
        expectMsg tck 0 |> ignore
        expectMsg tck 1 |> ignore
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``persistentActor supports persist after async and task computations`` () : unit =
    testDefault
    <| fun tck ->
        let ref =
            spawn tck "actor"
            <| propsPersist (fun ctx ->
                let rec loop () =
                    actor {
                        let! msg = ctx.Receive()

                        if ctx.HasPersisted() then
                            return! loop ()
                        else
                            ctx.Sender() <! 0
                            do! Async.Sleep 500
                            ctx.Sender() <! 1
                            do! Task.Delay 500
                            ctx.Sender() <! 2
                            return PersistAsync msg |> Effects.andThen (fun () -> ctx.Sender() <! 3)
                    }

                loop ())

        ref <! ""
        expectMsg tck 0 |> ignore
        expectMsg tck 1 |> ignore
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore
        expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``sender must be set when sending message in async blocks`` () : unit =
    testDefault
    <| fun tck ->
        let behaviorC (ctx: Actor<string>) =
            let rec loop () =
                actor {
                    let! _ = ctx.Receive()
                    let sender = ctx.Sender()
                    Assert.NotEqual<string>(sender.Path.Name, "deadLetters")
                    do! task { return sender <! "whatever" }
                    return! loop ()
                }

            loop ()

        let C = spawn tck "C" <| props behaviorC

        let behaviorB (ctx: Actor<string>) =
            let rec loop () =
                actor {
                    let! _ = ctx.Receive()
                    do! Async.Sleep 10
                    C <<! "something"
                    return! loop ()
                }

            loop ()

        let B = spawn tck "B" <| props behaviorB

        let behaviorA targetActor (ctx: Actor<string>) =
            let rec loop () =
                actor {
                    let! msg = ctx.Receive()

                    match msg with
                    | "Go" ->
                        do! Task.Delay 10
                        B <! "forward something to C"
                        return! loop ()
                    | _ ->
                        do! async { return targetActor <! "It's working" }
                        return! loop ()
                }

            loop ()

        let A = spawn tck "A" <| props (behaviorA (typed tck.TestActor))

        A <! "Go"
        expectMsg tck "It's working" |> ignore
