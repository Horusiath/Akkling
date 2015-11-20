//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Tests.Actors

open Akkling
open Akkling.TestKit
open Akka.Actor
open System
open Xunit

[<Fact>]
let ``Actor defined by recursive function responds on series of primitive messagess`` () : unit = testDefault <| fun tck -> 
    let echo = spawn tck "actor" (actorOf2 <| fun mailbox msg -> mailbox.Sender() <! msg)

    echo <! 1
    echo <! 2
    echo <! 3
    
    expectMsg tck 1 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck 3 |> ignore

[<Fact>]
let ``Actor defined by recursive function stops on return Stop`` () : unit = testDefault <| fun tck -> 
    let aref = 
        spawn tck "actor"
        <| fun mailbox ->
            let rec loop () =
                actor {
                    let! msg = mailbox.Receive ()
                    match msg with
                    | "stop" -> return Stop
                    | x -> 
                        mailbox.Sender() <! x
                        return! loop ()
                }
            loop ()

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
let ``Actor defined by recursive function unhandles message on return Unhandled`` () : unit = testDefault <| fun tck ->
    let aref = 
        spawn tck "actor"
        <| fun mailbox ->
            let rec loop () =
                actor {
                    let! msg = mailbox.Receive ()
                    match msg with
                    | "unhandled" -> return Unhandled
                    | x -> 
                        mailbox.Sender() <! x
                        return! loop ()
                }
            loop ()

    expectEvent 1 
    <| deadLettersEvents tck
    <| fun () ->
        aref <! "a"
        aref <! "b"
        aref <! "unhandled"
        aref <! "c"

        expectMsg tck "a" |> ignore
        expectMsg tck "b" |> ignore
        expectMsg tck "c" |> ignore
        expectNoMsg tck