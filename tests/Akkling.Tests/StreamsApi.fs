//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Tests.StreamsApi

open Akkling
open Akkling.Streams
open Akkling.Streams.Operators
open Akkling.TestKit
open Akka.Actor
open System
open Xunit
open Akka.Streams.Dsl

[<Fact>]
let ``Graph DSL operators should work`` () = testDefault <| fun tck ->
    use mat = tck.Sys.Materializer()
    let g = Graph.runnable(fun b -> 
        let i = Source.ofList [1..10] |> Source.mapMaterializedValue(fun () -> async.Return 0) |> b.From
        let o = Sink.reduce (+)

        i => o  |> ignore)
    let actual = g |> Graph.run mat |> Async.RunSynchronously
    actual |> equals 55