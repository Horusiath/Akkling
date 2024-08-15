//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Tests.Actors

open Akkling
open Akkling.TestKit
open Akkling.Extensions
open Akka.Actor
open System
open Xunit

[<Fact>]
let ``Actor defined by recursive function responds on series of primitive messagess`` () : unit =
    testDefault
    <| fun tck ->
        let echo = spawn tck "actor" (props Behaviors.echo)

        echo <! 1
        echo <! 2
        echo <! 3

        expectMsg tck 1 |> ignore
        expectMsg tck 2 |> ignore
        expectMsg tck 3 |> ignore

[<Fact>]
let ``Actor defined by recursive function stops on return Stop`` () : unit =
    testDefault
    <| fun tck ->
        let aref =
            spawn tck "actor"
            <| props (fun mailbox ->
                let rec loop () =
                    actor {
                        let! msg = mailbox.Receive()

                        match msg with
                        | "stop" -> return Stop
                        | x ->
                            mailbox.Sender() <! x
                            return! loop ()
                    }

                loop ())

        monitor tck aref

        aref <! "a"
        aref <! "b"
        aref <! "stop"
        aref <! "c"

        expectMsg tck "a" |> ignore
        expectMsg tck "b" |> ignore
        expectTerminated tck aref |> ignore
        expectNoMsg tck

[<Fact>]
let ``Actor defined by recursive function sends message to EventStream on return Unhandled`` () : unit =
    testDefault
    <| fun tck ->
        let aref =
            spawn tck "actor"
            <| props (fun mailbox ->
                let rec loop () =
                    actor {
                        let! msg = mailbox.Receive()

                        match msg with
                        | "unhandled" -> return Unhandled
                        | x ->
                            mailbox.Sender() <! x
                            return! loop ()
                    }

                loop ())

        let subscriber = tck.CreateTestProbe()

        tck.Sys.EventStream.Subscribe(subscriber.Ref, typeof<Akka.Event.UnhandledMessage>)
        |> ignore

        aref <! "a"
        aref <! "b"
        aref <! "unhandled"
        aref <! "c"
        expectMsg tck "a" |> ignore
        expectMsg tck "b" |> ignore
        subscriber.ExpectMsg<Akka.Event.UnhandledMessage>() |> ignore
        expectMsg tck "c" |> ignore
        expectNoMsg tck

[<Fact>]
let ``<|> combinator executes right side when left side was unhandled`` () =
    testDefault
    <| fun tck ->
        let left (ctx: Actor<_>) =
            function
            | "unhandled" -> unhandled ()
            | n -> ctx.Sender() <! n |> ignored

        let right (ctx: Actor<_>) =
            function
            | "unhandled" -> ctx.Sender() <! "handled" |> ignored
            | n -> ctx.Sender() <! n |> ignored

        let combined = left <|> right

        let aref = spawnAnonymous tck <| props (actorOf2 combined)

        aref <! "ok"
        aref <! "unhandled"

        expectMsg tck "ok" |> ignore
        expectMsg tck "handled" |> ignore
        expectNoMsg tck

[<Fact>]
let ``<&> combinator executes right side when left side was handled`` () =
    testDefault
    <| fun tck ->
        let left (ctx: Actor<_>) =
            function
            | "unhandled" -> unhandled ()
            | n -> ctx.Sender() <! n |> ignored

        let right (ctx: Actor<_>) =
            function
            | "unhandled" -> ctx.Sender() <! "handled" |> ignored
            | n -> ctx.Sender() <! n + " again" |> ignored

        let combined = left <&> right

        let aref = spawnAnonymous tck <| props (actorOf2 combined)

        aref <! "hello"
        aref <! "unhandled"

        expectMsg tck "hello" |> ignore
        expectMsg tck "hello again" |> ignore
        expectNoMsg tck

[<Fact>]
let ``pipeTo operator doesn't block`` () =
    testDefault
    <| fun tck ->
        let behavior (ctx: Actor<_>) =
            function
            | "start" ->
                async { return 1 } |!> (ctx.Sender())
                Ignore
            | _ -> Unhandled

        let aref = spawnAnonymous tck <| props (actorOf2 behavior)

        aref <! "start"

        expectMsg tck 1 |> ignore

[<Fact>]
let ``Slash operator works for ActorPath concatenation`` () =
    let path = ActorPath.Parse("akka.tcp://system@localhost:9000/user")
    let sub = path / "child" / "grandchild"
    Assert.Equal(ActorPath.Parse("akka.tcp://system@localhost:9000/user/child/grandchild"), sub)

[<Fact>]
let ``LifecycleEvent should be fired`` () =
    testDefault
    <| fun tck ->
        let aref =
            spawn tck "actor"
            <| props (fun mailbox ->
                let rec loop () =
                    actor {
                        let! (msg: obj) = mailbox.Receive()

                        match msg with
                        | String "stop" -> return Stop
                        | PreStart -> ()
                        | PostStop -> mailbox.Sender() <! "poststop"
                        | x -> mailbox.Sender() <! x
                    }

                loop ())

        let subscriber = tck.CreateTestProbe()

        tck.Sys.EventStream.Subscribe(subscriber.Ref, typeof<Akka.Event.UnhandledMessage>)
        |> ignore

        aref <! box "a"
        aref <! box "b"
        aref <! box "stop"

        expectMsg tck "a" |> ignore
        expectMsg tck "b" |> ignore
        expectMsg tck "poststop" |> ignore
        expectNoMsg tck

[<Fact>]
let ``LifecycleEvent should be fired with become`` () =
    testDefault
    <| fun tck ->
        let aref =
            spawn tck "actor"
            <| props (fun mailbox ->
                let rec loop () =
                    actor {
                        let! (msg: obj) = mailbox.Receive()

                        match msg with
                        | PreStart -> ()
                        | x ->
                            mailbox.Sender() <! x

                            let rec loop () =
                                actor {
                                    let! (msg: obj) = mailbox.Receive()

                                    match msg with
                                    | String "unhandled" -> return Unhandled
                                    | String "stop" -> return Stop
                                    | PostStop ->
                                        mailbox.Sender() <! "poststop"
                                        return! loop ()
                                    | x ->
                                        mailbox.Sender() <! x
                                        return! loop ()
                                }

                            return! loop ()
                    }

                loop ())

        let subscriber = tck.CreateTestProbe()

        tck.Sys.EventStream.Subscribe(subscriber.Ref, typeof<Akka.Event.UnhandledMessage>)
        |> ignore

        aref <! box "a"
        aref <! box "b"
        aref <! box "unhandled"
        aref <! box "stop"

        expectMsg tck "a" |> ignore
        expectMsg tck "b" |> ignore
        subscriber.ExpectMsg<Akka.Event.UnhandledMessage>() |> ignore
        expectMsg tck "poststop" |> ignore
        subscriber.ExpectNoMsg()
        expectNoMsg tck

[<Fact>]
let ``LifecycleEvent should be suppressed`` () =
    testDefault
    <| fun tck ->
        let aref =
            spawn tck "actor"
            <| props (fun mailbox ->
                let rec loop () =
                    actor {
                        match! mailbox.Receive() with
                        | "unhandled" -> return Unhandled
                        | "stop" -> return Stop
                        | x ->
                            mailbox.Sender() <! x
                            return! loop ()
                    }

                loop ())

        let subscriber = tck.CreateTestProbe()

        tck.Sys.EventStream.Subscribe(subscriber.Ref, typeof<Akka.Event.UnhandledMessage>)
        |> ignore

        aref <! "a"
        aref <! "b"
        aref <! "unhandled"
        aref <! "stop"

        expectMsg tck "a" |> ignore
        expectMsg tck "b" |> ignore
        subscriber.ExpectMsg<Akka.Event.UnhandledMessage>() |> ignore
        expectNoMsg tck
        subscriber.ExpectNoMsg()
