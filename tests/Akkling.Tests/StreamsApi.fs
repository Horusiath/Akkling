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
//open Akka.Streams.TestKit

let config = Configuration.parse "akka.logLevel = DEBUG"

[<Fact>]
let ``Graph DSL operators should work`` () = test config <| fun tck ->
    use mat = tck.Sys.Materializer()

    /// VISUAL Explanation:
    (**
                +-------------------------------+
                |           pickMaxOf3          |
                |  +----------+                 |
      zip1.in0 ====>          |   +---------+   |
                |  |   zip1   ====>         |   |
      zip1.in1 ====>          |   |         |   |
                |  +----------+   |   zip2  ====> zip2.out
      zip2.in1 ===================>         |   |
                |                 +---------+   |              
                +-------------------------------+
    *)
    
    let pickMaxOf3 = Graph.create (fun b-> 
        let zip1 = ZipWith.create max<int> |> b.Add
        let zip2 = ZipWith.create max<int> |> b.Add

        b.From(zip1.Out) => zip2.In0 |> ignore

        UniformFanInShape(zip2.Out, zip1.In0, zip1.In1, zip2.In1))
        
    /// VISUAL Explanation:
    (**
            +------------------------------------------+
            |        +--------------+                  |
            | s1 ====>              |   +------------+ | 
            |        |              |   |            | |
            | s2 ====>  pickMaxOf3  ====>  Sink.head ==> max
            |        |              |   |            | |
            | s3 ====>              |   +------------+ |
            |        +--------------+                  |
            +------------------------------------------+
    *)

    let max = 
        Sink.head<int> 
        |> Graph.create1(fun b sink -> 
            let pm3 = pickMaxOf3 |> b.Add

            let s1 = Source.singleton 1 |> b.Add
            let s2 = Source.singleton 2 |> b.Add
            let s3 = Source.singleton 3 |> b.Add

            b.From s1 => pm3.In(0) |> ignore
            b.From s2 => pm3.In(1) |> ignore
            b.From s3 => pm3.In(2) |> ignore

            b.From(pm3.Out) => sink |> ignore

            ClosedShape.Instance) 
        |> Graph.runnable
        |> Graph.run mat
        |> Async.RunSynchronously

    max |> equals 3
    
//[<Fact>]
//let ``A deduplicate must remove consecutive duplicates`` () = test config <| fun tck ->
//    use mat = tck.Sys.Materializer()
//    let probe = tck.CreateManualSubscriberProbe<int>()
//    Source.ofList [1; 1; 1; 2; 2; 1; 1; 3]
//    |> Source.deduplicate (=)
//    |> Source.runWith mat (Sink.ofSubscriber probe)
//
//    let sub = probe.ExpectSubscription()
//    sub.request 1000
//    probe.ExpectNext 1
//    probe.ExpectNext 2
//    probe.ExpectNext 1
//    probe.ExpectNext 3  
//    probe.ExpectComplete()