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