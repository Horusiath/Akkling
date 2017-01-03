//-----------------------------------------------------------------------
// <copyright file="Source.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open System.IO
open Akkling
open Akka.IO
open Akka.Streams
open Akka.Streams.Stage
open Akka.Streams.IO
open Akka.Streams.Dsl
open Reactive.Streams

[<RequireQualifiedAccess>]
module Source =

    /// Creates a source from an stream created by the given function.
    let inline ofStream (streamFn: unit -> Stream) : Source<ByteString, Async<IOResult>> =
        StreamConverters.FromInputStream(Func<_>(streamFn)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// Creates a source from an stream created by the given function.
    /// Emitted elements are chunk sized ByteString elements,
    /// except the final element, which will be up to chunkSize in size.
    let inline ofStreamChunked (chunkSize: int) (streamFn: unit -> Stream) : Source<ByteString, Async<IOResult>> =
        StreamConverters.FromInputStream(Func<_>(streamFn)).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// Creates a source which when materialized will return an stream which it is possible
    /// to write the ByteStrings to the stream this Source is attached to.
    /// This Source is intended for inter-operation with legacy APIs since it is inherently blocking.
    let inline asStream () : Source<ByteString, Stream> = StreamConverters.AsOutputStream()

    /// Creates a source which when materialized will return an stream which it is possible
    /// to write the ByteStrings to the stream this Source is attached to.
    /// This Source is intended for inter-operation with legacy APIs since it is inherently blocking.
    let inline asStreamWithTimeout (timeout: TimeSpan) : Source<ByteString, Stream> = StreamConverters.AsOutputStream(Nullable timeout)

    /// Creates a Source from a Files contents.
    let inline ofFile (filePath: string) : Source<ByteString, Async<IOResult>> =
        FileIO.FromFile(FileInfo filePath).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// Creates a Source from a Files contents.
    /// Emitted elements are chunkSize sized ByteString elements,
    /// except the final element, which will be up to chunkSize in size.
    let inline ofFileChunked (chunkSize: int) (filePath: string) : Source<ByteString, Async<IOResult>> =
        FileIO.FromFile(FileInfo filePath, chunkSize).MapMaterializedValue(Func<_,_>(Async.AwaitTask))

    /// Construct a transformation starting with given publisher. The transformation steps
    /// are executed by a series of processor instances
    /// that mediate the flow of elements downstream and the propagation of
    /// back-pressure upstream.
    let inline ofPublisher (p: #IPublisher<'t>) : Source<'t, unit> = Source.FromPublisher(p).MapMaterializedValue(Func<_,_>(ignore))

    /// Helper to create source from seq.
    let inline ofSeq (elements: 't seq) : Source<'t, unit> = Source.From(elements).MapMaterializedValue(Func<_,_>(ignore))

    /// Helper to create source from array.
    let inline ofArray (elements: 't []) : Source<'t, unit> = Source.From(elements).MapMaterializedValue(Func<_,_>(ignore))

    /// Helper to create source from list.
    let inline ofList (elements: 't list) : Source<'t, unit> = Source.From(elements).MapMaterializedValue(Func<_,_>(ignore))

    /// Create a source with one element.
    let inline single (elem: 't) : Source<'t, unit> = Source.Single(elem).MapMaterializedValue(Func<_,_>(ignore))

    /// Transforms only the materialized value of the Source.
    let inline mapMaterializedValue (fn: 'mat -> 'mat2) (source: Source<'t, 'mat>) : Source<'t, 'mat2> =
        source.MapMaterializedValue(Func<_,_>(fn))

    /// A graph with the shape of a source logically is a source, this method makes
    /// it so also in type.
    let inline ofGraph (graph: #IGraph<SourceShape<'t>, 'mat>) : Source<'t, 'mat> = Source.FromGraph(graph)

    /// Start a new source from the given Async computation. The stream will consist of
    /// one element when the computation is completed with a successful value, which
    /// may happen before or after materializing the flow.
    /// The stream terminates with a failure if the task is completed with a failure.
    let inline ofAsync (a: Async<'t>) : Source<'t, unit> =
        Source.FromTask(a |> Async.StartAsTask).MapMaterializedValue(Func<_,_>(ignore))

    /// Elements are emitted periodically with the specified interval.
    /// The tick element will be delivered to downstream consumers that has requested any elements.
    /// If a consumer has not requested any elements at the point in time when the tick
    /// element is produced it will not receive that tick element later. It will
    /// receive new tick elements as soon as it has requested more elements.
    let inline tick (after: TimeSpan) (every: TimeSpan) (elem: 't) : Source<'t, Akka.Actor.ICancelable> =
        Source.Tick(after, every, elem)

    /// Create a source that will continually emit the given element.
    let inline replicate (elem: 't) : Source<'t, unit> =
        Source.Repeat(elem).MapMaterializedValue(Func<_,_>(ignore))

    /// Create a source that will unfold a value of one type into
    /// a pair of the next state and output elements of type another type
    let inline unfold (fn: 's -> 's * 'e) (state: 's) : Source<'e, unit> =
        Source.Unfold(state, Func<_,_>(fn)).MapMaterializedValue(Func<_,_>(ignore))

    /// Same as unfold, but uses an async function to generate the next state-element tuple.
    let inline asyncUnfold (fn: 's -> Async<'s * 'e>) (state: 's) : Source<'e, unit> =
        Source.UnfoldAsync(state, Func<_, _>(fun x -> fn(x) |> Async.StartAsTask)).MapMaterializedValue(Func<_,_>(ignore))

    /// Simpler unfold, for infinite sequences.
    let inline infiniteUnfold (fn: 's -> 's * 'e) (state: 's) : Source<'e, unit> =
        Source.UnfoldInfinite(state, Func<_,_>(fn)).MapMaterializedValue(Func<_,_>(ignore))

    /// A source with no elements, i.e. an empty stream that is completed immediately for every connected sink.
    let inline empty<'t> : Source<'t, unit> =
        Source.Empty<'t>().MapMaterializedValue(Func<_,_>(ignore))

    /// Create a source which materializes a completion result which controls what element
    /// will be emitted by the Source.
    let inline option<'t> : Source<'t, System.Threading.Tasks.TaskCompletionSource<'t>> = Source.Maybe<'t>()

    /// Create a source that immediately ends the stream with the error to every connected sink.
    let inline raise (error: exn) : Source<'t, unit> =
        Source.Failed(error).MapMaterializedValue(Func<_,_>(ignore))

    /// Creates a source that is materialized as a subscriber
    let inline subscriber<'t> : Source<'t, ISubscriber<'t>> = Source.AsSubscriber<'t>()

    /// Creates a source that is materialized to an actor ref which points to an Actor
    /// created according to the passed in props. Actor created by the props must
    /// be ActorPublisher<'t>.
    let inline ofProps (props: Props<'t>) : Source<'t, IActorRef<'t>> =
        Source.ActorPublisher(props.ToProps()).MapMaterializedValue(Func<_,_>(typed))

    /// Creates a source that is materialized as an actor ref
    /// Messages sent to this actor will be emitted to the stream if there is demand from downstream,
    /// otherwise they will be buffered until request for demand is received.
    let inline actorRef (overflowStrategy: OverflowStrategy) (count: int) : Source<'t, IActorRef<'t>>  =
        Source.ActorRef(count, overflowStrategy).MapMaterializedValue(Func<_,_>(typed))

    /// Creates a source that is materialized as an source queue.
    /// You can push elements to the queue and they will be emitted to the stream if there is demand from downstream,
    /// otherwise they will be buffered until request for demand is received.
    let inline queue (overflowStrategy: OverflowStrategy) (count: int) : Source<'t, ISourceQueueWithComplete<'t>> =
        Source.Queue(count, overflowStrategy)

    /// Recover allows to send last element on failure and gracefully complete the stream
    /// Since the underlying failure signal onError arrives out-of-band, it might jump over existing elements.
    /// This stage can recover the failure signal, but not the skipped elements, which will be dropped.
    let recover (fn: exn -> 'out option) (source) : Source<'out, 'mat> =
        SourceOperations.Recover(source, Func<exn, _>(fn >> toCsOption))

    /// RecoverWith allows to switch to alternative Source on flow failure. It will stay in effect after
    /// a failure has been recovered so that each time there is a failure it is fed into the <paramref name="partialFunc"/> and a new
    /// Source may be materialized.
    let inline recoverWithRetries (attempts: int) (fn: exn -> #IGraph<SourceShape<'out>, 'mat>) (source) : Source<'out, 'mat> =
        SourceOperations.RecoverWithRetries(source, Func<exn, Akka.Streams.IGraph<SourceShape<_>, _>>(fun ex -> upcast fn ex), attempts)

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step.
    let inline map (fn: 't -> 'u) (source) : Source<'u, 'mat> =
        SourceOperations.Select(source, Func<_, _>(fn))

    /// Transform each input element into a sequence of output elements that is
    /// then flattened into the output stream.
    ///
    /// The returned sequence MUST NOT contain NULL values,
    /// as they are illegal as stream elements - according to the Reactive Streams specification.
    let inline collect (fn: 't -> #seq<'u>) (source) : Source<'u, 'mat> =
        SourceOperations.SelectMany(source, Func<_, _>(fun x -> upcast fn x))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step. The function returns an Async and the
    /// value of that computation will be emitted downstream. The number of tasks
    /// that shall run in parallel is given as the first argument.
    /// These tasks may complete in any order, but the elements that
    /// are emitted downstream are in the same order as received from upstream.
    let inline asyncMap (parallelism: int) (fn: 't -> Async<'u>) (source) : Source<'u, 'mat> =
        SourceOperations.SelectAsync(source, parallelism, Func<_, _>(fun x -> fn(x) |> Async.StartAsTask))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step. The function returns an Async and the
    /// value of that computation will be emitted downstreams. As many asyncs as requested elements by
    /// downstream may run in parallel and each processed element will be emitted dowstream
    /// as soon as it is ready, i.e. it is possible that the elements are not emitted downstream
    /// in the same order as received from upstream.
    let inline asyncMapUnordered (parallelism: int) (fn: 't -> Async<'u>) (source) : Source<'u, 'mat> =
        SourceOperations.SelectAsyncUnordered(source, parallelism, Func<_, _>(fun x -> fn(x) |> Async.StartAsTask))

    /// Only pass on those elements that satisfy the given predicate function.
    let inline filter (pred: 't -> bool) (source) : Source<'t, 'mat> =
        SourceOperations.Where(source, Predicate<_>(pred))

    /// Terminate processing (and cancel the upstream publisher) after predicate function
    /// returns false for the first time. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    let inline takeWhile (pred: 't -> bool) (source) : Source<'t, 'mat> =
        SourceOperations.TakeWhile(source, Predicate<_>(pred))

    /// Discard elements at the beginning of the stream while predicate function is true.
    /// All elements will be taken after function returns false first time.
    let inline skipWhile (pred: 't -> bool) (source) : Source<'t, 'mat> =
        SourceOperations.SkipWhile(source, Predicate<_>(pred))

    /// Transform this stream by applying the given function to each of the elements
    /// on which the function is defined (read: returns Some) as they pass through this processing step.
    /// Non-matching elements are filtered out.
    let inline choose (fn: 't -> 'u option) (source) : Source<'u, 'mat> =
        SourceOperations.Collect(source, Func<_, _>(fn)).Select(Func<_,_>(fun (Some x) -> x))

    /// Chunk up this stream into groups of the given size, with the last group
    /// possibly smaller than requested due to end-of-stream.
    /// N must be positive, otherwise ArgumentException is thrown.
    let inline windowed (n: int) (source) : Source<'t seq, 'mat> =
        SourceOperations.Grouped(source, n)

    /// Ensure stream boundedness by limiting the number of elements from upstream.
    /// If the number of incoming elements exceeds max parameter, it will signal
    /// upstream failure StreamLimitReachedException downstream.
    ///
    /// Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    ///
    /// The stream will be completed without producing any elements if max parameter
    /// is zero or negative.
    let inline limit (max: int64) (source) : Source<'t, 'mat> =
        SourceOperations.Limit(source, max)

    /// Ensure stream boundedness by evaluating the cost of incoming elements
    /// using a cost function. Exactly how many elements will be allowed to travel downstream depends on the
    /// evaluated cost of each element. If the accumulated cost exceeds max parameter, it will signal
    /// upstream failure StreamLimitReachedException downstream.
    ///
    /// Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    ///
    /// The stream will be completed without producing any elements if max parameter is zero
    /// or negative.
    let inline limitWeighted (max: int64) (costFn: 't -> int64) (source) : Source<'t, 'mat> =
        SourceOperations.LimitWeighted(source, max, Func<_, _>(costFn))

    /// Apply a sliding window over the stream and return the windows as groups of elements, with the last group
    /// possibly smaller than requested due to end-of-stream.
    let inline sliding (n: int) (source) : Source<'t seq, 'mat> =
        SourceOperations.Sliding(source, n)

    /// Apply a sliding window over the stream and return the windows as groups of elements, with the last group
    /// possibly smaller than requested due to end-of-stream.
    let inline slidingBy (n: int) (step: int) (source) : Source<'t seq , 'mat> =
        SourceOperations.Sliding(source, n, step)

    /// Similar to fold but is not a terminal operation,
    /// emits its current value which starts at zero and then
    /// applies the current and next value to the given function,
    /// emitting the next current value.
    ///
    /// If the function throws an exception and the supervision decision is
    /// restart current value starts at zero again the stream will continue.
    let inline scan (folder: 'state -> 't -> 'state) (zero: 'state) (source) : Source<'state, 'mat> =
        SourceOperations.Scan(source, zero, Func<_,_,_>(folder))

    /// Similar to scan but only emits its result when the upstream completes,
    /// after which it also completes. Applies the given function towards its current and next value,
    /// yielding the next current value.
    ///
    /// If the function throws an exception and the supervision decision is
    /// restart current value starts at state again the stream will continue.
    let inline fold (folder: 'state -> 't -> 'state) (zero: 'state) (source) : Source<'state, 'mat> =
        SourceOperations.Aggregate(source, zero, Func<_,_,_>(folder))

    /// Similar to fold but uses first element as zero element.
    /// Applies the given function towards its current and next value,
    /// yielding the next current value.
    let inline reduce (acc: 't -> 't -> 't) (source) : Source<'t, 'mat> =
        SourceOperations.Sum(source, Func<_,_,_>(acc))

    /// Intersperses stream with provided element, similar to how string.Join
    /// injects a separator between a collection's elements.
    ///
    /// Additionally can inject start and end marker elements to stream.
    let inline intersperseBounded (start: 't) (inject: 't) (finish: 't) (source) : Source<'t, 'mat> =
        SourceOperations.Intersperse(source, start, inject, finish)

    /// Intersperses stream with provided element, similar to how string.Join
    /// injects a separator between a collection's elements.
    let inline intersperse (inject: 't) (source) : Source<'t, 'mat> =
        SourceOperations.Intersperse(source, inject)

    /// Chunk up this stream into groups of elements received within a time window,
    /// or limited by the given number of elements, whatever happens first.
    /// Empty groups will not be emitted if no elements are received from upstream.
    /// The last group before end-of-stream will contain the buffered elements
    /// since the previously emitted group.
    let inline groupedWithin (n: int) (timeout: TimeSpan) (source) : Source<'t seq, 'mat> =
        SourceOperations.GroupedWithin(source, n, timeout)

    /// Shifts elements emission in time by a specified amount. It allows to store elements
    /// in internal buffer while waiting for next element to be emitted.
    ///
    /// Delay precision is 10ms to avoid unnecessary timer scheduling cycles
    let inline delay (by: TimeSpan) (source) : Source<'t, 'mat>=
        SourceOperations.Delay(source, by)

    /// Shifts elements emission in time by a specified amount. It allows to store elements
    /// in internal buffer while waiting for next element to be emitted.Depending on the defined
    /// strategy it might drop elements or backpressure the upstream if
    /// there is no space available in the buffer.
    ///
    /// Delay precision is 10ms to avoid unnecessary timer scheduling cycles
    let inline delayWithStrategy (strategy: DelayOverflowStrategy) (by: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.Delay(source, by, Nullable strategy)

    /// Discard the given number of elements at the beginning of the stream.
    /// No elements will be dropped if parameter is zero or negative.
    let inline skip (n: int64) (source) : Source<'t, 'mat> =
        SourceOperations.Skip(source, n)

    /// Discard the elements received within the given duration at beginning of the stream.
    let inline skipWithin (timeout: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.SkipWithin(source, timeout)

    /// Terminate processing (and cancel the upstream publisher) after the given
    /// number of elements. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    ///
    /// The stream will be completed without producing any elements if parameter is zero
    /// or negative.
    let inline take (n: int64) (source) : Source<'t, 'mat> =
        SourceOperations.Take(source, n)

    /// Terminate processing (and cancel the upstream publisher) after the given
    /// duration. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    let inline takeWithin (timeout: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.TakeWithin(source, timeout)

    /// Allows a faster upstream to progress independently of a slower subscriber by conflating elements into a summary
    /// until the subscriber is ready to accept them. For example a conflate step might average incoming numbers if the
    /// upstream publisher is faster.
    ///
    /// This version of conflate allows to derive a seed from the first element and change the aggregated type to
    /// be different than the input type. See conflate for a simpler version that does not change types.
    ///
    /// This element only rolls up elements if the upstream is faster, but if the downstream is faster it will not
    /// duplicate elements.
    let inline conflateSeeded (folder: 't -> 'u -> 't) (seed: 'u -> 't) (source) : Source<'t, 'mat> =
        SourceOperations.ConflateWithSeed(source, Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by conflating elements into a summary
    /// until the subscriber is ready to accept them. For example a conflate step might average incoming numbers if the
    /// upstream publisher is faster.
    ///
    /// This version of conflate does not change the output type of the stream. See conflateSeeded
    /// for a more flexible version that can take a seed function and transform elements while rolling up.
    ///
    /// This element only rolls up elements if the upstream is faster, but if the downstream is faster it will not
    /// duplicate elements.
    let inline conflate (folder: 't -> 't -> 't) (source) : Source<'t, 'mat> =
        SourceOperations.Conflate(source, Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by aggregating elements into batches
    /// until the subscriber is ready to accept them.For example a batch step might store received elements in
    /// an array up to the allowed max limit if the upstream publisher is faster.
    let inline batch (max: int64) (seed: 'u -> 't) (folder: 't -> 'u -> 't) (source) : Source<'t, 'mat> =
        SourceOperations.Batch(source, max, Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by aggregating elements into batches
    /// until the subscriber is ready to accept them.
    let inline batchWeighted (max: int64) (costFn: 'u -> int64) (seed: 'u -> 't) (folder: 't -> 'u -> 't) (source) : Source<'t, 'mat> =
        SourceOperations.BatchWeighted(source, max, Func<_,_>(costFn), Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster downstream to progress independently of a slower publisher by extrapolating elements from an older
    /// element until new element comes from the upstream. For example an expand step might repeat the last element for
    /// the subscriber until it receives an update from upstream.
    ///
    /// This element will never "drop" upstream elements as all elements go through at least one extrapolation step.
    /// This means that if the upstream is actually faster than the upstream it will be backpressured by the downstream
    /// subscriber.
    ///
    /// Expand does not support restart and resume directives. Exceptions from the extrapolate function will complete the stream with failure.
    let inline expand (extrapolate: 'u -> #seq<'t>) (source) : Source<'t, 'mat>  =
        SourceOperations.Expand(source, Func<_,_>(fun x -> upcast extrapolate x))

    /// Adds a fixed size buffer in the flow that allows to store elements from a faster upstream until it becomes full.
    /// Depending on the defined strategy it might drop elements or backpressure the upstream if
    /// there is no space available
    let inline buffer (strategy: OverflowStrategy) (n: int) (source) : Source<'t, 'mat> =
        SourceOperations.Buffer(source, n, strategy)

    /// Generic transformation of a stream with a custom processing stage.
    /// This operator makes it possible to extend the flow API when there is no specialized
    /// operator that performs the transformation.
    let inline transform (stageFac: unit -> #IStage<'u, 't>) (source) : Source<'t, 'mat>  =
        SourceOperations.Transform(source, Func<_>(fun () -> upcast stageFac()))

    /// Takes up to n elements from the stream and returns a pair containing a strict sequence of the taken element
    /// and a stream representing the remaining elements. If <paramref name="n"/> is zero or negative, then this will return a pair
    /// of an empty collection and a stream containing the whole upstream unchanged.
    //let inline prefixAndTail n flow = SourceOperations.PrefixAndTail(flow, n)

    /// This operation demultiplexes the incoming stream into separate output
    /// streams, one for each element key. The key is computed for each element
    /// using the given function. When a new key is encountered for the first time
    /// it is emitted to the downstream subscriber together with a fresh
    /// flow that will eventually produce all the elements of the substream
    /// for that key. Not consuming the elements from the created streams will
    /// stop this processor from processing more elements, therefore you must take
    /// care to unblock (or cancel) all of the produced streams even if you want
    /// to consume only one of them.
    let inline groupBy (max: int) (groupFn: 'u -> 't) (source) : SubFlow<'u, 'mat, IRunnableGraph<'mat>> =
        SourceOperations.GroupBy(source, max, Func<_,_>(groupFn))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams, always beginning a new one with
    /// the current element if the given predicate returns true for it.
    let inline splitWhen (pred: 't -> bool) (source) : SubFlow<'t, 'mat, IRunnableGraph<'mat>> =
        SourceOperations.SplitWhen(source, Func<_, _>(pred))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams, always beginning a new one with
    /// the current element if the given predicate returns true for it.
    let inline splitWhenCancellable (cancelStrategy: SubstreamCancelStrategy) (pred: 't -> bool) (source) : SubFlow<'t, 'mat, IRunnableGraph<'mat>> =
        SourceOperations.SplitWhen(source, cancelStrategy, Func<_,_>(pred))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams. It *ends* the current substream when the
    /// predicate is true.
    let inline splitAfter (pred: 't -> bool) (source) : SubFlow<'t, 'mat, IRunnableGraph<'mat>> =
        SourceOperations.SplitAfter(source, Func<_,_>(pred))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams. It *ends* the current substream when the
    /// predicate is true.
    let inline splitAfterCancellable (strategy: SubstreamCancelStrategy) (pred: 't -> bool) (source) : SubFlow<'t, 'mat, IRunnableGraph<'mat>> =
        SourceOperations.SplitAfter(source, strategy, Func<_,_>(pred))

    /// Transform each input element into a source of output elements that is
    /// then flattened into the output stream by concatenation,
    /// fully consuming one Source after the other.
    let inline collectMap (flatten: 'u -> #IGraph<SourceShape<'t>, 'mat>) (source) : Source<'t, 'mat> =
        SourceOperations.ConcatMany(source, Func<_,_>(fun x -> upcast flatten x))

    /// Transform each input element into a source of output elements that is
    /// then flattened into the output stream by merging, where at most breadth
    /// substreams are being consumed at any given time.
    let inline collectMerge (breadth: int) (flatten: 'u -> #IGraph<SourceShape<'t>, 'mat>) (source) : Source<'t, 'mat> =
        SourceOperations.MergeMany(source, breadth, Func<_,_>(fun x -> upcast flatten x))

    /// If the first element has not passed through this stage before the provided timeout, the stream is failed
    /// with a TimeoutException
    let inline initWithin (timeout: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.InitialTimeout(source, timeout)

    /// If the completion of the stream does not happen until the provided timeout, the stream is failed
    /// with a TimeoutException.
    let inline completeWithin (timeout: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.CompletionTimeout(source, timeout)

    /// If the time between two processed elements exceed the provided timeout, the stream is failed
    /// with a TimeoutException.
    let inline idleTimeout (timeout: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.IdleTimeout(source, timeout)

    /// Injects additional elements if the upstream does not emit for a configured amount of time. In other words, this
    /// stage attempts to maintains a base rate of emitted elements towards the downstream.
    let inline keepAlive (timeout: TimeSpan) (injectFn: unit -> 't) (source) : Source<'t, 'mat> =
        SourceOperations.KeepAlive(source, timeout, Func<_>(injectFn))

    /// Sends elements downstream with speed limited to n/per timeout. In other words, this stage set the maximum rate
    /// for emitting messages. This combinator works for streams where all elements have the same cost or length.
    ///
    /// Throttle implements the token bucket model. There is a bucket with a given token capacity (burst size or maximumBurst).
    /// Tokens drops into the bucket at a given rate and can be "spared" for later use up to bucket capacity
    /// to allow some burstyness. Whenever stream wants to send an element, it takes as many
    /// tokens from the bucket as number of elements. If there isn't any, throttle waits until the
    /// bucket accumulates enough tokens.
    let inline throttle mode maxBurst n per (source) : Source<'t, 'mat> =
        SourceOperations.Throttle(source, n, per, maxBurst, mode)

    /// Sends elements downstream with speed limited to n/per timeout. In other words, this stage set the maximum rate
    /// for emitting messages. This combinator works for streams where all elements have the same cost or length.
    ///
    /// Throttle implements the token bucket model. There is a bucket with a given token capacity (burst size or maximumBurst).
    /// Tokens drops into the bucket at a given rate and can be "spared" for later use up to bucket capacity
    /// to allow some burstyness. Whenever stream wants to send an element, it takes as many
    /// tokens from the bucket as number of elements. If there isn't any, throttle waits until the
    /// bucket accumulates enough tokens.
    let inline throttleWeighted (mode: ThrottleMode) (maxBurst: int) (n: int) (per: TimeSpan) (costFn: 't -> int) (source) : Source<'t, 'mat> =
        SourceOperations.Throttle(source, n, per, maxBurst, Func<_,_>(costFn), mode)

    /// Attaches the given sink graph to this flow, meaning that elements that passes
    /// through will also be sent to the sink.
    let inline alsoToMat (sink: #IGraph<SinkShape<'t>, 'mat2>) (matFn: 'mat -> 'mat2 -> 'mat3) (source) : Source<'t, 'mat3> =
        SourceOperations.AlsoToMaterialized(source, sink, Func<_,_,_>(matFn))

    /// Attaches the given sink to this flow, meaning that elements that passes
    /// through will also be sent to the sink.
    let inline alsoTo (sink: #IGraph<SinkShape<'t>, 'mat>) (source) : Source<'t, 'mat> =
        SourceOperations.AlsoTo(source, sink)

    /// Materializes to Async that completes on getting termination message.
    /// The task completes with success when received complete message from upstream or cancel
    /// from downstream. It fails with the same error when received error message from
    /// downstream.
    let inline watchTermination (matFn: 'mat -> Async<unit> -> 'mat2) (source) : Source<'t, 'mat2> =
        SourceOperations.WatchTermination(source, Func<_,_,_>(fun m t -> matFn m (t |> Async.AwaitTask)))

    /// Detaches upstream demand from downstream demand without detaching the
    /// stream rates; in other words acts like a buffer of size 1.
    let inline detach (source) : Source<'t, 'mat> =
        SourceOperations.Detach(source)

    /// Delays the initial element by the specified duration.
    let inline initDelay (delay: TimeSpan) (source) : Source<'t, 'mat> =
        SourceOperations.InitialDelay(source, delay)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline log (stageName: string) (source) : Source<'t, 'mat> =
        SourceOperations.Log(source, stageName)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logf (stageName: string) format (source) : Source<'t, 'mat> =
        SourceOperations.Log(source, stageName, Func<_,_>(format >> box))

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logWith (stageName: string) (logger: Akka.Event.ILoggingAdapter) (source) : Source<'t, 'mat> =
        SourceOperations.Log(source, stageName, log = logger)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logWithf (stageName: string) format (logger: Akka.Event.ILoggingAdapter) (source) : Source<'t, 'mat> =
        SourceOperations.Log(source, stageName, Func<_,_>(format >> box), logger)

    /// Combine the elements of current flow and the given source into a stream of tuples.
    let inline zip (other: #IGraph<SourceShape<'u>, 'mat>) (source) : Source<'t * 'u, 'mat> =
        SourceOperations.ZipWith(source, other, Func<_,_,_>(fun x y -> (x,y)))

    /// Put together the elements of current flow and the given source
    /// into a stream of combined elements using a combiner function.
    let inline zipWith (other: #IGraph<SourceShape<'u>, 'mat>) (combineFn: 't -> 'u -> 'x) (source) : Source<'x, 'mat> =
        SourceOperations.ZipWith(source, other, Func<_,_,_>(combineFn))

    /// Interleave is a deterministic merge of the given source with elements of this flow.
    /// It first emits number of elements from this flow to downstream, then - same amount for other
    /// source, then repeat process.
    let inline interleave (count: int) (other: #IGraph<SourceShape<_>, 'mat>) (source) : Source<_, 'mat> =
        SourceOperations.Interleave(source, other, count)

    /// Interleave is a deterministic merge of the given source with elements of this flow.
    /// It first emits count number of elements from this flow to downstream, then - same amount for source,
    /// then repeat process.
    let inline interleaveMat (count: int) (other: #IGraph<SourceShape<_>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (source) : Source<_, 'mat3> =
        SourceOperations.InterleaveMaterialized(source, other, count, Func<_,_,_>(combineFn))

    /// Merge the given source to this flow, taking elements as they arrive from input streams,
    /// picking randomly when several elements ready.
    let inline merge (other: #IGraph<SourceShape<_>, 'mat>) (source) : Source<_, 'mat> =
        SourceOperations.Merge(source, other)

    /// Merge the given source to this flow, taking elements as they arrive from input streams,
    /// picking randomly when several elements ready.
    let inline mergeMat (other: #IGraph<SourceShape<_>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (source) : Source<_, 'mat3> =
        SourceOperations.MergeMaterialized(source, other, Func<_,_,_>(combineFn))

    /// Merge the given source to this flow, taking elements as they arrive from input streams,
    /// picking always the smallest of the available elements(waiting for one element from each side
    /// to be available). This means that possible contiguity of the input streams is not exploited to avoid
    /// waiting for elements, this merge will block when one of the inputs does not have more elements(and
    /// does not complete).
    let inline mergeSort (other: #IGraph<SourceShape<'t>, 'mat>) (sortFn: 't -> 't -> int) (source) : Source<'t, 'mat> =
        SourceOperations.MergeSorted(source, other, Func<_,_,_>(sortFn))

    /// Concatenate the given source to this flow, meaning that once this
    /// Flow’s input is exhausted and all result elements have been generated,
    /// the Source’s elements will be produced.
    let inline concat (other: #IGraph<SourceShape<'t>, 'mat>) (source) : Source<'t, 'mat> =
        SourceOperations.Concat(source, other)

    /// Prepend the given source to this flow, meaning that before elements
    /// are generated from this flow, the Source's elements will be produced until it
    /// is exhausted, at which point Flow elements will start being produced.
    let inline prepend (other: #IGraph<SourceShape<'t>, 'mat>) (source) : Source<'t, 'mat> =
        SourceOperations.Prepend(source, other)

    /// Put an asynchronous boundary around this Source.
    let inline async (source: Source<'t, 'mat>) : Source<'t, 'mat> = source.Async()

    /// Add a name attribute to this Source.
    let inline named (name: string) (source: Source<'t, 'mat>) : Source<'t, 'mat> = source.Named(name)

    /// Nests the current Source and returns a Source with the given Attributes
    let inline attrs (a: Attributes) (source: Source<'t, 'mat>) : Source<'t, 'mat> = source.WithAttributes(a)

    /// Transform this source by appending the given processing steps.
    /// The materialized value of the combined source will be the materialized
    /// value of the current flow (ignoring the other flow’s value).
    let inline via (flow: #IGraph<FlowShape<'t,'u>, 'mat2>) (source: Source<'t, 'mat>) : Source<'u, 'mat> =
        source.Via(flow)

    /// Transform this source by appending the given processing steps.
    /// The combine function is used to compose the materialized values of this flow and that
    /// flow into the materialized value of the resulting Flow.
    let inline viaMat (flow: #IGraph<FlowShape<'t,'u>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (source: Source<'t, 'mat>) : Source<'u, 'mat3> =
        source.ViaMaterialized(flow, Func<_,_,_>(combineFn))

    /// Connect this source to a sink concatenating the processing steps of both.
    let inline toMat (sink: #IGraph<SinkShape<'t>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (source: Source<'t, 'mat>) : IRunnableGraph<'mat3> =
        source.ToMaterialized(sink, Func<_,_,_>(combineFn))

    /// Connect this source to a sink and run it. The returned value is the materialized value of the sink
    let inline runWith (mat: #IMaterializer) (sink: #IGraph<SinkShape<'t>, 'mat2>) (source: Source<'t, 'mat>) : 'mat2 =
        source.RunWith(sink, mat)

    /// Shortcut for running this source with a fold function.
    /// The given function is invoked for every received element, giving it its previous
    /// output (or the given zero value) and the element as input.
    /// The returned Async computation will be completed with value of the final
    /// function evaluation when the input stream ends, or completed with Failure
    /// if there is a failure signaled in the stream.
    let inline runFold (mat: #IMaterializer) (folder: 'state -> 't -> 'state) (zero: 'state) (source: Source<'t, 'mat>) : Async<'state> =
        source.RunAggregate(zero, Func<_,_,_>(folder), mat) |> Async.AwaitTask

    /// Shortcut for running this surce with a reduce function.
    /// The given function is invoked for every received element, giving it its previous
    /// output (from the second element) and the element as input.
    /// The returned Async computation will be completed with value of the final
    /// function evaluation when the input stream ends, or completed with Failure
    /// if there is a failure signaled in the stream.
    let inline runReduce (mat: #IMaterializer) (folder: 't -> 't -> 't) (source: Source<'t, 'mat>) : Async<'t> =
        source.RunSum(Func<_,_,_>(folder), mat) |> Async.AwaitTask

    /// Shortcut for running this source with a foreach procedure. The given procedure is invoked
    /// for each received element.
    /// The returned Async computation will be completed with Success when reaching the
    /// normal end of the stream, or completed with Failure if there is a failure signaled in
    /// the stream.
    let inline runForEach (mat: #IMaterializer) (fn: 't -> unit) (source: Source<'t, 'mat>) : Async<unit> =
        source.RunForeach(Action<_>(fn), mat) |> Async.AwaitTask