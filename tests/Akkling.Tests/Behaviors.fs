//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Tests.Behaviors

open Akkling
open Akkling.TestKit
open Akka.Actor
open System
open Xunit

type Authorization =
    | Authorized of string
    | Unauthorized of string

[<Fact>]
let ``Receive <&> should execute both actions on effect other than Unhandled`` () = testDefault <| fun tck ->
    let authorize _ = function
        | Authorized(msg) -> ignored()
        | Unauthorized(msg) -> unhandled()
    let respond (ctx: Actor<_>) = function
        | Authorized(msg) -> ctx.Sender() <! msg |> ignored
        | Unauthorized(msg) -> ctx.Sender() <! msg |> ignored

    let aref = spawn tck "test" (props (actorOf2 (authorize <&> respond)))
    aref <! Authorized "a1"
    aref <! Unauthorized "a2"

    expectMsg tck "a1"
    expectNoMsg tck
    
[<Fact>]
let ``Receive <|> should execute both actions on effect equal Unhandled`` () = testDefault <| fun tck ->
    let authorize _ = function
        | Authorized(msg) -> ignored()
        | Unauthorized(msg) -> unhandled()
    let respond (ctx: Actor<_>) = function
        | Authorized(msg) -> ctx.Sender() <! msg |> ignored
        | Unauthorized(msg) -> ctx.Sender() <! msg |> ignored

    let aref = spawn tck "test" (props (actorOf2 (authorize <|> respond)))
    aref <! Authorized "a1"
    aref <! Unauthorized "a2"

    expectMsg tck "a2"
    expectNoMsg tck