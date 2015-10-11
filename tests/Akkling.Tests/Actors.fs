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
let ``Actor defined by recursive function responds on series of primitive messagess`` () = testDefault <| fun tck -> 
    let echo = spawn tck "actor" (actorOf2 <| fun mailbox msg -> mailbox.Sender() <! msg)

    echo <! 1
    echo <! 2
    echo <! 3
    
    expectMsg tck 1 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck 3 |> ignore

[<Fact>]
let ``Actor defined by recursive function stops on return escape`` () = testDefault <| fun tck -> 
    let aref = 
        spawn tck "actor"
        <| fun mailbox ->
            let rec loop () =
                actor {
                    let! msg = mailbox.Receive ()
                    match msg with
                    | "stop" -> return 0
                    | "unhandled" -> unhandled
                    | x -> 
                        mailbox.Sender() <! x
                        return! loop ()
                }
            loop ()

    monitor tck aref

    aref <! "a"
    aref <! "b"
    aref <! "stop"
    aref <! "unhandled"
    aref <! "c"
    
    expectMsg tck "a" |> ignore
    expectMsg tck "b" |> ignore
    expectTerminated tck aref |> ignore
    expectNoMsg tck 