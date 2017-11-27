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
open Akka.Streams

let config = Configuration.parse "akka.logLevel = DEBUG"

[<Fact>]
let ``Graph DSL operators should work`` () = test config <| fun tck ->
    use mat = tck.Sys.Materializer()
    
    let pickMaxOf3 = Graph.create (fun b-> 
        let zip1 = ZipWith.create max<int> |> b.Add
        let zip2 = ZipWith.create max<int> |> b.Add

        b.From(zip1.Out) => zip2.In0

        UniformFanInShape(zip2.Out, zip1.In0, zip1.In1, zip2.In1))

    let max = 
        Sink.head<int> 
        |> Graph.create1(fun b sink -> 
            let pm3 = pickMaxOf3 |> b.Add

            let s1 = Source.singleton 1 |> b.Add
            let s2 = Source.singleton 2 |> b.Add
            let s3 = Source.singleton 3 |> b.Add

            b.From s1 => pm3.In(0)
            b.From s2 => pm3.In(1)
            b.From s3 => pm3.In(2)

            b.From(pm3.Out) => sink

            ClosedShape.Instance) 
        |> Graph.runnable
        |> Graph.run mat
        |> Async.RunSynchronously

    max |> equals 3