//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
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

let config () =
    Configuration.parse "akka.logLevel = DEBUG"

[<Fact>]
let ``Graph DSL operators should work`` () =
    test (config ())
    <| fun tck ->
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

        let pickMaxOf3 =
            Graph.create
            <| fun b ->
                graph b {
                    let! zip1 = ZipWith.create max<int>
                    let! zip2 = ZipWith.create max<int>

                    b.From zip1.Out =>> zip2.In0 |> ignore

                    return UniformFanInShape(zip2.Out, zip1.In0, zip1.In1, zip2.In1)
                }

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
            |> Graph.create1 (fun b sink ->
                graph b {
                    let! pm3 = pickMaxOf3

                    let! s1 = Source.singleton 1
                    let! s2 = Source.singleton 2
                    let! s3 = Source.singleton 3

                    b.From s1 =>> pm3.In(0) |> ignore
                    b.From s2 =>> pm3.In(1) |> ignore
                    b.From s3 =>> pm3.In(2) |> ignore

                    b.From(pm3.Out) =>> sink |> ignore
                })
            |> Graph.runnable
            |> Graph.run mat
            |> Async.RunSynchronously

        max |> equals 3

open Akka.Streams.TestKit
open Akkling.Streams
open Akkling.Streams.TestKit
open Akkling.Streams.TestKit.ManualSubscriberProbe

[<Fact>]
let ``A deduplicate must remove consecutive duplicates`` () =
    test (config ())
    <| fun tck ->
        use mat = tck.Sys.Materializer()
        let probe = manualSubscriberProbe tck

        Source.ofList [ 1; 1; 1; 2; 2; 1; 1; 3 ]
        |> Source.deduplicate (=)
        |> Source.runWith mat (Sink.ofSubscriber probe)

        let sub = expectSubscription probe
        sub.Request 1000L

        probe
        |> expectNext 1
        |> expectNext 2
        |> expectNext 1
        |> expectNext 3
        |> expectComplete
        |> ignore

[<Fact>]
let ``A concat must emit elements in correct order`` () =
    test (config ())
    <| fun tck ->
        use mat = tck.Sys.Materializer()

        Source.ofList [ 1; 2; 3 ]
        |> Source.concat (Source.ofList [ 4; 5; 6 ])
        |> Source.runFold mat (fun acc x -> x :: acc) []
        |> Async.RunSynchronously
        |> List.rev
        |> equals [ 1; 2; 3; 4; 5; 6 ]

[<Fact>]
let ``Expand must pass through elements unchanged when there is no rate difference`` () =
    test (config ())
    <| fun tck ->
        use mat = tck.Sys.Materializer()
        let publisher = TestPublisher.CreatePublisherProbe(tck)
        let subscriber = TestSubscriber.CreateSubscriberProbe(tck)

        Source.FromPublisher publisher
        |> Source.expand (fun x -> List.replicate 200 x)
        |> Source.toSink (Sink.ofSubscriber subscriber)
        |> Graph.run mat
        |> ignore

        for i in 1..100 do
            // Order is important here: If the request comes first it will be extrapolated!
            publisher.SendNext i |> ignore
            subscriber.RequestNext i |> ignore

        subscriber.Cancel() |> ignore
