//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.AsyncSupport

open Akkling
open Akkling.TestKit
open Akka.Actor
open System
open Xunit
open Akkling.Persistence

[<Fact>]
let ``actor builder supports bind to async`` () = testDefault <| fun tck ->
    let ref = 
        spawn tck "actor"
        <| props (fun ctx ->
            let rec loop state =
                actor {
                    let! msg = ctx.Receive ()
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
let ``actor supports multiple async computations`` () : unit = testDefault <| fun tck ->
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
let ``persistentActor builder supports bind to async`` () = testDefault <| fun tck ->
    let ref = 
        spawn tck "actor"
        <| propsPersist (fun ctx ->
            let rec loop state =
                actor {
                    let! msg = ctx.Receive ()
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
let ``persistentActor supports multiple async computations`` () : unit = testDefault <| fun tck ->
    let ref = 
        spawn tck "actor"
        <| propsPersist (fun ctx ->
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