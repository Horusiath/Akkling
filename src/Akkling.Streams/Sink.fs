//-----------------------------------------------------------------------
// <copyright file="Sink.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open System.IO
open Akkling
open Akka.IO
open Akka.Streams
open Akka.Streams.IO
open Akka.Streams.Dsl
open Reactive.Streams

[<RequireQualifiedAccess>]
module Sink =

    /// A graph with the shape of a sink logically is a sink, this method makes it so also in type.
    let inline wrap (graph: #IGraph<_, _>) : Sink<_, _> = Sink.Wrap(graph)

    /// Helper to create sink from subscriber.
    let inline ofSubscriber (sub: #ISubscriber<_>) : Sink<_, unit> = Sink.FromSubscriber(sub).MapMaterializedValue(Func<_,_>(ignore))

    /// A sink that materializes into an Async computation of the first value received.
    let inline head<'t> : Sink<'t, Async<'t>> = Sink.First<'t>().MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that materializes into an Async computation of the first value received.
    let inline headOption<'t> : Sink<'t, Async<'t>> = Sink.FirstOrDefault<'t>().MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that materializes into an Async computation of the last value received.
    let inline tail<'t> : Sink<'t, Async<'t>>  = Sink.Last<'t>().MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that materializes into an Async computation of the last value received.
    let inline tailOption<'t> : Sink<'t, Async<'t>>  = Sink.LastOrDefault<'t>().MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that materializes into a publisher that can handle one subscriber.
    let inline publisher<'t> : Sink<'t, IPublisher<'t>> = Sink.Publisher<'t>()
    
    /// A sink that materializes into an Observable.
    let inline observable<'t> : Sink<'t, IObservable<'t>> = Sink.AsObservable<'t>() 
    
    /// A sink that materializes into a publisher that can handle multiple subscribers.
    let inline fanoutPublisher<'t> : Sink<'t, IPublisher<'t>> = Sink.FanoutPublisher<'t>()

    /// A sink that will consume the stream and discard the elements.
    let inline ignore<'t> : Sink<'t, Async<unit>> = Sink.Ignore<'t>().MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that will invoke the given function for each received element. 
    /// The sink is materialized into an Async computation will be completed with success when reaching the
    /// normal end of the stream, or completed with a failure if there is a failure signaled in
    /// the stream.
    let inline forEach (fn: 't -> unit) : Sink<'t, Async<unit>> = Sink.ForEach(Action<_>(fn)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that will invoke the given function 
    /// to each of the elements as they pass in. 
    /// The sink is materialized into an Async computation.
    let inline forEachParallel (parallelism: int) (fn: 't -> unit) : Sink<'t, Async<unit>> = Sink.ForEachParallel(parallelism, Action<_>(fn)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that will invoke the given folder function for every received element, 
    /// giving it its previous output (or the given zero value) and the element as input.
    /// The returned Async computation will be completed with value of the final
    /// function evaluation when the input stream ends, or completed with the streams exception
    /// if there is a failure signaled in the stream.
    let inline fold (zero: 'state) (folder: 'state -> 'elem -> 'state) : Sink<'elem, Async<'state>>  = Sink.Aggregate(zero, Func<_,_,_>(folder)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that will invoke the given folder for every received element, giving it its previous
    /// output (from the second element) and the element as input.
    /// The returned Async computation will be completed with value of the final
    /// function evaluation when the input stream ends, or completed with `Failure`
    /// if there is a failure signaled in the stream.
    let inline reduce (folder: 't -> 't -> 't) : Sink<'t, Async<'t>> = Sink.Sum(Func<_,_,_>(folder)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// A sink that when the flow is completed, either through a failure or normal
    /// completion, apply the provided function with None or Some exn.
    let inline onComplete (fn: exn option -> unit) : Sink<'t, unit> = Sink.OnComplete(Action(fun () -> fn(None)), Action<_>(fun e -> fn(Some e))).MapMaterializedValue(Func<_,_>(Microsoft.FSharp.Core.Operators.ignore))

    /// Sends the elements of the stream to the given Actor ref.
    /// If the target actor terminates the stream will be canceled.
    /// When the stream is completed successfully the given completeMsg
    /// will be sent to the destination actor.
    /// When the stream is completed with failure a Status.Failure
    /// message will be sent to the destination actor.
    let inline toActorRef (completeMsg: 't) (ref: IActorRef<'t>) : Sink<'t, unit> = Sink.ActorRef(untyped ref, completeMsg).MapMaterializedValue(Func<_,_>(Microsoft.FSharp.Core.Operators.ignore))

    /// Sends the elements of the stream to the given actor ref that sends back back-pressure signal.
    /// First element is always initMsg, then stream is waiting for acknowledgement message
    /// ackMsg from the given actor which means that it is ready to process
    /// elements. It also requires ackMsg message after each stream element
    /// to make backpressure work.
    let inline toActorRefAck (initMsg: 't) (ackMsg: 't) (completeMsg: 't) (failMsgFn: exn -> 't) (ref: IActorRef<'t>) : Sink<'t, unit> = 
        Sink.ActorRefWithAck(untyped ref, initMsg, ackMsg, completeMsg, Func<_,_>(failMsgFn >> box)).MapMaterializedValue(Func<_,_>(Microsoft.FSharp.Core.Operators.ignore))

    /// Creates a sink that is materialized to an actor ref which points to an Actor
    /// created according to the passed in props. Actor created by the props should
    /// be ActorSubscriberSink<'t>
    let inline ofProps (props: Props<'t>) : Sink<'t, IActorRef<'t>> = Sink.ActorSubscriber(props.ToProps()).MapMaterializedValue(Func<_,_>(typed))

    /// Creates a sink that is materialized as an ISinkQueue<'t>.
    /// ISinkQueue{TIn}.AsyncPull method is pulling element from the stream and returns Async<'t option>,
    /// which completes when element is available.
    let inline queue<'t> : Sink<'t, ISinkQueue<'t>> = Sink.Queue<'t>()

    /// A graph with the shape of a sink logically is a sink, this method makes it so also in type.
    let inline ofGraph (graph: #IGraph<_,_>) = Sink.FromGraph(graph)

    /// A sink that immediately cancels its upstream after materialization.
    let inline canceled<'t> = Sink.Cancelled<'t>()

    /// A sink that materializes into a publisher.
    /// If fanout is true, the materialized publisher will support multiple subscribers and
    /// the size of the ActorMaterializerSettings.MaxInputBufferSize configured for this stage becomes the maximum number of elements that
    /// the fastest subscriber can be ahead of the slowest one before slowing
    /// the processing down due to back pressure.
    let inline toPublisher (fanout: bool) : Sink<'t, IPublisher<'t>> = Sink.AsPublisher fanout

    /// Creates a Sink which writes incoming ByteString elements to the given file and either overwrites
    /// or appends to it.
    let inline toFile (filePath: string) : Sink<ByteString, Async<IOResult>> = 
        FileIO.ToFile(FileInfo filePath).MapMaterializedValue(Func<_,_>(Async.AwaitTask))
        
    /// Creates a Sink which writes incoming ByteString elements to the given file and either overwrites
    /// or appends to it.
    let inline toFileWithMode (fileMode: FileMode) (filePath: string) : Sink<ByteString, Async<IOResult>> = 
        FileIO.ToFile(FileInfo filePath, Nullable fileMode).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// Creates a Sink which writes incoming ByteStrings to a stream created by the given function.
    let inline ofStream (streamFn: unit -> Stream) : Sink<ByteString, Async<IOResult>> =
        StreamConverters.FromOutputStream(Func<_>(streamFn)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))
        
    /// Creates a Sink which writes incoming ByteStrings to a stream created by the given function.
    /// Stream will be flushed whenever a byte array is written.
    let inline ofStreamFlushed (streamFn: unit -> Stream) : Sink<ByteString, Async<IOResult>> =
        StreamConverters.FromOutputStream(Func<_>(streamFn), true).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// Creates a Sink which when materialized will return an stream which it is possible
    /// to read the values produced by the stream this Sink is attached to.
    /// This Sink is intended for inter-operation with legacy APIs since it is inherently blocking.
    let inline asStream (): Sink<ByteString, Stream> = StreamConverters.AsInputStream()
    
    /// Creates a Sink which when materialized will return an stream which it is possible
    /// to read the values produced by the stream this Sink is attached to.
    /// This Sink is intended for inter-operation with legacy APIs since it is inherently blocking.
    let inline asStreamWithTimeout (timeout: TimeSpan): Sink<ByteString, Stream> = StreamConverters.AsInputStream(Nullable timeout)
    
    /// Transform the materialized value of this Source, leaving all other properties as they were.
    let inline mapMatValue (fn: 'mat -> 'mat2) (sink: Sink<'t,'mat>) = sink.MapMaterializedValue(Func<'mat,'mat2> fn)

    /// A BroadcastHub is a special streaming hub that is able to broadcast streamed elements 
    /// to a dynamic set of consumers.
    let inline broadcastHub (perConsumerBufferSize: int) : Sink<'t, Source<'t, unit>> = 
        BroadcastHub.Sink(perConsumerBufferSize)
        |> mapMatValue (fun source -> source.MapMaterializedValue(Func<_,_> (fun _ -> ())))

    let statefulPartition 
        (bufferSize: int) 
        (startAfterNrOfConsumers: int) 
        (initialState: 'state) 
        (partitioner: PartitionHub.IConsumerInfo -> 'state -> 't -> 'state * int64) : Sink<'t, Source<'t, unit>> =
            let partitionFunc : Func<Func<PartitionHub.IConsumerInfo, 't, int64>> = Func<_> (fun () ->
                let mutable state = initialState
                let picker info elem = 
                    let (newState, bucket) = partitioner info state elem
                    state <- newState
                    bucket

                Func<_,_,_> picker)

            PartitionHub.StatefulSink(partitionFunc, startAfterNrOfConsumers, bufferSize)
            |> mapMatValue (fun source -> source.MapMaterializedValue(Func<_,_> (fun _ -> ())))

    let partition (bufferSize: int) (startAfterNrOfConsumers: int) (partitioner: int -> 't -> int) : Sink<'t, Source<'t, unit>> =
        PartitionHub.Sink (Func<_,_,_>(partitioner), startAfterNrOfConsumers, bufferSize)
        |> mapMatValue (fun source -> source.MapMaterializedValue(Func<_,_> (fun _ -> ())))
        
    /// A local sink which materializes a source ref which can be used by other streams (including remote ones),
    /// to consume data from this local stream, as if they were attached in the spot of the local Sink directly.
    let ref<'t> : Sink<'t, Async<ISourceRef<'t>>> =
        StreamRefs.SourceRef().MapMaterializedValue(Func<_,_>(Async.AwaitTask<ISourceRef<'t>>))
        
    let inline ofRef (sourceRef: ISinkRef<'t>) : Sink<'t, unit> =
        sourceRef.Sink.MapMaterializedValue(Func<_,_>(Microsoft.FSharp.Core.Operators.ignore))