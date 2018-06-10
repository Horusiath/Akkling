//-----------------------------------------------------------------------
// <copyright file="Flow.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open Akkling
open Akka.Streams
open Akka.Streams.Dsl

type SwitchMode = SwitchMode

type DelayStrategy<'a> =
    | FixedDelay of TimeSpan
    | LinearIncreasingDelay of init:TimeSpan * step:TimeSpan * max:TimeSpan * increaseCheck:('a -> bool)
    | CustomDelay of ('a -> TimeSpan)

[<RequireQualifiedAccess>]
module Flow =
    open Reactive.Streams
    open Stages
    open Akka.Streams.Dsl

    /// Creates a new empty flow.
    let empty<'t, 'mat> : Flow<'t, 't, 'mat> = Flow.Create<'t, 'mat>()

    /// Recover allows to send last element on failure and gracefully complete the stream
    /// Since the underlying failure signal onError arrives out-of-band, it might jump over existing elements.
    /// This stage can recover the failure signal, but not the skipped elements, which will be dropped.
    let recover (fn: exn -> 'out option) (flow: Flow<'inp, 'out, 'mat>) : Flow<'inp, 'out, 'mat> =
        FlowOperations.Recover(flow, Func<_, _>(fn >> toCsOption))
        
    /// RecoverWith allows to switch to alternative Source on flow failure. It will stay in effect after
    /// a failure has been recovered so that each time there is a failure it is fed into the <paramref name="partialFunc"/> and a new
    /// Source may be materialized.
    let inline recoverWithRetries (attempts: int) (fn: exn -> #IGraph<SourceShape<'out>, 'mat>) (flow) : Flow<'inp, 'out, 'mat> =
        FlowOperations.RecoverWithRetries(flow, Func<exn, Akka.Streams.IGraph<SourceShape<_>, _>>(fun ex -> upcast fn ex), attempts)
        
    /// While similar to recover, this stage can be used to transform an error signal to a different one without logging
    /// it as an error in the process. So in that sense it is NOT exactly equivalent to Recover(e => throw e2) since Recover
    /// would log the e2 error. 
    let inline mapError (fn: exn -> #exn) (flow) : Flow<'inp,'out,'mat> =
        FlowOperations.SelectError(flow, Func<exn, exn>(fun e -> upcast fn e))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step.
    let inline map (fn: 'u -> 'w) (flow: Flow<'t, 'u, 'mat>) : Flow<'t, 'w, 'mat> =
        FlowOperations.Select(flow, Func<_, _>(fn))

    /// Transform each input element into a sequence of output elements that is
    /// then flattened into the output stream.
    ///
    /// The returned sequence MUST NOT contain NULL values,
    /// as they are illegal as stream elements - according to the Reactive Streams specification.
    let inline collect (fn: 'u-> #seq<'w>) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.SelectMany(flow, Func<_, _>(fun x -> upcast fn x))
        
    /// Givien initial state, transforms each input element into new output state and a 
    /// sequence-like structure of output elements, that is then flattened into the output stream.
    let statefulCollect (fn: 'state -> 't -> 'state * #seq<'u>) (zero: 'state) (flow): Flow<'t, 'u, 'mat> =
        FlowOperations.StatefulSelectMany(flow, Func<_>(fun () ->
            let mutable state = zero
            Func<_,_>(fun x ->
                let newState, result = fn state x
                state <- newState
                upcast result)))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step. The function returns an Async and the
    /// value of that computation will be emitted downstream. The number of tasks
    /// that shall run in parallel is given as the first argument.
    /// These tasks may complete in any order, but the elements that
    /// are emitted downstream are in the same order as received from upstream.
    let inline asyncMap (parallelism: int) (fn: 'u -> Async<'w>) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.SelectAsync(flow, parallelism, Func<_, _>(fn >> Async.StartAsTask))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step. The function returns an Async and the
    /// value of that computation will be emitted downstreams. As many asyncs as requested elements by
    /// downstream may run in parallel and each processed element will be emitted dowstream
    /// as soon as it is ready, i.e. it is possible that the elements are not emitted downstream
    /// in the same order as received from upstream.
    let inline asyncMapUnordered (parallelism: int) (fn: 'u -> Async<'w>) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.SelectAsyncUnordered(flow, parallelism, Func<_, _>(fn >> Async.StartAsTask))

    /// Only pass on those elements that satisfy the given predicate function.
    let inline filter (pred: 'u -> bool) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Where(flow, Predicate<_>(pred))

    /// Terminate processing (and cancel the upstream publisher) after predicate function
    /// returns false for the first time. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    let inline takeWhile (pred: 'u -> bool) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.TakeWhile(flow, Predicate<_>(pred))

    /// Discard elements at the beginning of the stream while predicate function is true.
    /// All elements will be taken after function returns false first time.
    let inline skipWhile (pred: 'u -> bool) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.SkipWhile(flow, Predicate<_>(pred))

    /// Transform this stream by applying the given function to each of the elements
    /// on which the function is defined (read: returns Some) as they pass through this processing step.
    /// Non-matching elements are filtered out.
    let inline choose (fn: 'u -> 'w option) (flow) : Flow<'t, 'w, 'mat> = 
        FlowOperations.Collect(flow, Func<_, _>(fn)).Where(Predicate<_>(Option.isSome)).Select(Func<_,_>(Option.get))

    /// Chunk up this stream into groups of the given size, with the last group
    /// possibly smaller than requested due to end-of-stream.
    /// N must be positive, otherwise ArgumentException is thrown.
    let inline windowed (n: int) (flow) : Flow<'t, 'u seq, 'mat> = FlowOperations.Grouped(flow, n)

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
    let inline limit (max: int64) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Limit(flow, max)

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
    let inline limitWeighted (max: int64) (costFn: 'u -> int64) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.LimitWeighted(flow, max, Func<_, _>(costFn))

    /// Apply a sliding window over the stream and return the windows as groups of elements, with the last group
    /// possibly smaller than requested due to end-of-stream.
    let inline sliding (n: int) (flow) : Flow<'t, 'u seq, 'mat> = FlowOperations.Sliding(flow, n)

    /// Apply a sliding window over the stream and return the windows as groups of elements, with the last group
    /// possibly smaller than requested due to end-of-stream.
    let inline slidingBy (n: int) (step: int) (flow) : Flow<'t, 'u seq, 'mat> = FlowOperations.Sliding(flow, n, step)

    /// Similar to fold but is not a terminal operation,
    /// emits its current value which starts at zero and then
    /// applies the current and next value to the given function,
    /// emitting the next current value.
    ///
    /// If the function throws an exception and the supervision decision is
    /// restart current value starts at zero again the stream will continue.
    let inline scan (folder: 'w -> 'u -> 'w) (state: 'w) (flow) : Flow<'t, 'w, 'mat> = FlowOperations.Scan(flow, state, Func<_,_,_>(folder))

    /// Similar to scan but with a asynchronous function, emits its current value which 
    /// starts at zero and then applies the current and next value to the given function
    /// emitting an Async that resolves to the next current value.
    let inline asyncScan (folder: 'state -> 'out -> Async<'state>) (zero: 'state) (flow: Flow<'inp,'out,'mat>) : Flow<'inp,'state,'mat> =
        FlowOperations.ScanAsync(flow, zero, Func<_,_,_>(fun s e -> folder s e |> Async.StartAsTask))

    /// Similar to scan but only emits its result when the upstream completes,
    /// after which it also completes. Applies the given function towards its current and next value,
    /// yielding the next current value.
    ///
    /// If the function throws an exception and the supervision decision is
    /// restart current value starts at state again the stream will continue.
    let inline fold (folder: 'w -> 'u -> 'w) (state: 'w) (flow) : Flow<'t, 'w, 'mat> = FlowOperations.Aggregate(flow, state, Func<_,_,_>(folder))
    
    /// Similar to fold but with an asynchronous function.
    /// Applies the given function towards its current and next value,
    /// yielding the next current value.
    /// 
    /// If the function 'folder' returns a failure and the supervision decision is
    /// Directive.Restart current value starts at 'state' again
    /// the stream will continue.
    let inline asyncFold (folder: 'w -> 'u -> Async<'w>) (state: 'w) (flow) : Flow<'t, 'w, 'mat> = 
        FlowOperations.AggregateAsync(flow, state, Func<_,_,_>(fun acc x -> folder acc x |> Async.StartAsTask))

    /// Similar to fold but uses first element as zero element.
    /// Applies the given function towards its current and next value,
    /// yielding the next current value.
    let inline reduce (acc: 'u -> 'u -> 'u) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Sum(flow, Func<_,_,_>(acc))

    /// Intersperses stream with provided element, similar to how string.Join
    /// injects a separator between a collection's elements.
    ///
    /// Additionally can inject start and end marker elements to stream.
    let inline intersperseBounded (start: 'u) (inject: 'u) (finish: 'u) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Intersperse(flow, start, inject, finish)

    /// Intersperses stream with provided element, similar to how string.Join
    /// injects a separator between a collection's elements.
    let inline intersperse (inject: 'u) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Intersperse(flow, inject)

    /// Chunk up this stream into groups of elements received within a time window,
    /// or limited by the given number of elements, whatever happens first.
    /// Empty groups will not be emitted if no elements are received from upstream.
    /// The last group before end-of-stream will contain the buffered elements
    /// since the previously emitted group.
    let inline groupedWithin (n: int) (timeout: TimeSpan) (flow) : Flow<'t, 'u seq, 'mat> = FlowOperations.GroupedWithin(flow, n, timeout)

    /// Shifts elements emission in time by a specified amount. It allows to store elements
    /// in internal buffer while waiting for next element to be emitted.
    ///
    /// Delay precision is 10ms to avoid unnecessary timer scheduling cycles
    let inline delay (by: TimeSpan) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Delay(flow, by)

    /// Shifts elements emission in time by a specified amount. It allows to store elements
    /// in internal buffer while waiting for next element to be emitted.Depending on the defined
    /// strategy it might drop elements or backpressure the upstream if
    /// there is no space available in the buffer.
    ///
    /// Delay precision is 10ms to avoid unnecessary timer scheduling cycles
    let inline delayWithStrategy (strategy: DelayOverflowStrategy) (by: TimeSpan) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Delay(flow, by, Nullable strategy)

    /// Discard the given number of elements at the beginning of the stream.
    /// No elements will be dropped if parameter is zero or negative.
    let inline skip (n: int64) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Skip(flow, n)

    /// Discard the elements received within the given duration at beginning of the stream.
    let inline skipWithin (timeout: TimeSpan) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.SkipWithin(flow, timeout)

    /// Terminate processing (and cancel the upstream publisher) after the given
    /// number of elements. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    ///
    /// The stream will be completed without producing any elements if parameter is zero
    /// or negative.
    let inline take (n: int64) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Take(flow, n)

    /// Terminate processing (and cancel the upstream publisher) after the given
    /// duration. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    let inline takeWithin (timeout: TimeSpan) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.TakeWithin(flow, timeout)

    /// Allows a faster upstream to progress independently of a slower subscriber by conflating elements into a summary
    /// until the subscriber is ready to accept them. For example a conflate step might average incoming numbers if the
    /// upstream publisher is faster.
    ///
    /// This version of conflate allows to derive a seed from the first element and change the aggregated type to
    /// be different than the input type. See conflate for a simpler version that does not change types.
    ///
    /// This element only rolls up elements if the upstream is faster, but if the downstream is faster it will not
    /// duplicate elements.
    let inline conflateSeeded (folder: 'w -> 'u -> 'w) (seed: 'u -> 'w) (flow) : Flow<'t, 'w, 'mat> = FlowOperations.ConflateWithSeed(flow, Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by conflating elements into a summary
    /// until the subscriber is ready to accept them. For example a conflate step might average incoming numbers if the
    /// upstream publisher is faster.
    ///
    /// This version of conflate does not change the output type of the stream. See conflateSeeded
    /// for a more flexible version that can take a seed function and transform elements while rolling up.
    ///
    /// This element only rolls up elements if the upstream is faster, but if the downstream is faster it will not
    /// duplicate elements.
    let inline conflate (folder: 'u -> 'u -> 'u) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Conflate(flow, Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by aggregating elements into batches
    /// until the subscriber is ready to accept them.For example a batch step might store received elements in
    /// an array up to the allowed max limit if the upstream publisher is faster.
    let inline batch (max: int64) (seed: 'u -> 'w) (folder: 'w -> 'u -> 'w) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.Batch(flow, max, Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by aggregating elements into batches
    /// until the subscriber is ready to accept them.
    let inline batchWeighted (max: int64) (costFn: 'u -> int64) (seed: 'u -> 'w) (folder: 'w -> 'u -> 'w) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.BatchWeighted(flow, max, Func<_,_>(costFn), Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster downstream to progress independently of a slower publisher by extrapolating elements from an older
    /// element until new element comes from the upstream. For example an expand step might repeat the last element for
    /// the subscriber until it receives an update from upstream.
    ///
    /// This element will never "drop" upstream elements as all elements go through at least one extrapolation step.
    /// This means that if the upstream is actually faster than the upstream it will be backpressured by the downstream
    /// subscriber.
    ///
    /// Expand does not support restart and resume directives. Exceptions from the extrapolate function will complete the stream with failure.
    let inline expand (extrapolate: 'u -> #seq<'w> ) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.Expand(flow, Func<_,_>(fun x -> upcast extrapolate x))

    /// Adds a fixed size buffer in the flow that allows to store elements from a faster upstream until it becomes full.
    /// Depending on the defined strategy it might drop elements or backpressure the upstream if
    /// there is no space available
    let inline buffer (strategy: OverflowStrategy) (n: int) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.Buffer(flow, n, strategy)
            
    /// Takes up to n elements from the stream and returns a pair containing a strict sequence of the taken element
    /// and a stream representing the remaining elements. If `n` is zero or negative, then this will return a pair
    /// of an empty collection and a stream containing the whole upstream unchanged.
    let inline prefixAndTail (n: int) (flow) : Flow<'inp,'out list * Source<'out, unit>, 'mat> = 
        FlowOperations.PrefixAndTail(flow, n).Select(Func<_,_>(fun (imm, source) -> 
            let s = source.MapMaterializedValue(Func<_,_>(ignore))
            (List.ofSeq imm), s))

    /// This operation demultiplexes the incoming stream into separate output
    /// streams, one for each element key. The key is computed for each element
    /// using the given function. When a new key is encountered for the first time
    /// it is emitted to the downstream subscriber together with a fresh
    /// flow that will eventually produce all the elements of the substream
    /// for that key. Not consuming the elements from the created streams will
    /// stop this processor from processing more elements, therefore you must take
    /// care to unblock (or cancel) all of the produced streams even if you want
    /// to consume only one of them.
    let inline groupBy (max: int) (groupFn: 'u -> 'key) (flow) : SubFlow<'u, 'mat, Sink<'t, 'mat>> =
        FlowOperations.GroupBy(flow, max, Func<_,_>(groupFn))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams, always beginning a new one with
    /// the current element if the given predicate returns true for it.
    let inline splitWhen (pred: 'u -> bool) (flow) : SubFlow<'u, 'mat, Sink<'t, 'mat>> =
        FlowOperations.SplitWhen(flow, Func<_, _>(pred))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams, always beginning a new one with
    /// the current element if the given predicate returns true for it.
    let inline splitWhenCancellable (cancelStrategy: SubstreamCancelStrategy) (pred: 'u -> bool) (flow) : SubFlow<'u, 'mat, Sink<'t, 'mat>> =
        FlowOperations.SplitWhen(flow, cancelStrategy, Func<_,_>(pred))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams. It *ends* the current substream when the
    /// predicate is true.
    let inline splitAfter (pred: 'u -> bool) (flow) : SubFlow<'u, 'mat, Sink<'t, 'mat>> =
        FlowOperations.SplitAfter(flow, Func<_,_>(pred))

    /// This operation applies the given predicate to all incoming elements and
    /// emits them to a stream of output streams. It *ends* the current substream when the
    /// predicate is true.
    let inline splitAfterCancellable (strategy: SubstreamCancelStrategy) (pred: 'u -> bool) (flow) : SubFlow<'u, 'mat, Sink<'t, 'mat>> =
        FlowOperations.SplitAfter(flow, strategy, Func<_,_>(pred))

    /// Transform each input element into a source of output elements that is
    /// then flattened into the output stream by concatenation,
    /// fully consuming one Source after the other.
    let inline collectMap (flatten: 'u -> #IGraph<SourceShape<'w>, 'mat>) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.ConcatMany(flow, Func<_,_>(fun x -> upcast flatten x))

    /// Transform each input element into a source of output elements that is
    /// then flattened into the output stream by merging, where at most breadth
    /// substreams are being consumed at any given time.
    let inline collectMerge (breadth: int) (flatten: 'u -> #IGraph<SourceShape<'w>, 'mat>) (flow) : Flow<'t, 'w, 'mat> =
        FlowOperations.MergeMany(flow, breadth, Func<_,_>(fun x -> upcast flatten x))

    /// Combine the elements of current flow into a stream of tuples consisting
    /// of all elements paired with their index. Indices start at 0.
    let inline zipi (flow) : Flow<'inp, 'out * int64, 'mat> =
        FlowOperations.ZipWithIndex(flow)

    /// If the first element has not passed through this stage before the provided timeout, the stream is failed
    /// with a TimeoutException
    let inline initWithin (timeout: TimeSpan) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.InitialTimeout(flow, timeout)

    /// If the completion of the stream does not happen until the provided timeout, the stream is failed
    /// with a TimeoutException.
    let inline completeWithin (timeout: TimeSpan) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.CompletionTimeout(flow, timeout)

    /// If the time between two processed elements exceed the provided timeout, the stream is failed
    /// with a TimeoutException.
    let inline idleTimeout (timeout: TimeSpan) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.IdleTimeout(flow, timeout)

    /// If the time between the emission of an element and the following downstream demand exceeds the provided timeout,
    /// the stream is failed with a TimeoutException. The timeout is checked periodically,
    /// so the resolution of the check is one period (equals to timeout value).
    let inline backpressureTimeout (timeout: TimeSpan) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.BackpressureTimeout(flow, timeout)

    /// Injects additional elements if the upstream does not emit for a configured amount of time. In other words, this
    /// stage attempts to maintains a base rate of emitted elements towards the downstream.
    let inline keepAlive (timeout: TimeSpan) (injectFn: unit -> 'u) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.KeepAlive(flow, timeout, Func<_>(injectFn))

    /// Sends elements downstream with speed limited to n/per timeout. In other words, this stage set the maximum rate
    /// for emitting messages. This combinator works for streams where all elements have the same cost or length.
    ///
    /// Throttle implements the token bucket model. There is a bucket with a given token capacity (burst size or maximumBurst).
    /// Tokens drops into the bucket at a given rate and can be "spared" for later use up to bucket capacity
    /// to allow some burstyness. Whenever stream wants to send an element, it takes as many
    /// tokens from the bucket as number of elements. If there isn't any, throttle waits until the
    /// bucket accumulates enough tokens.
    let inline throttle (mode: ThrottleMode) (maxBurst: int) (n: int) (per: TimeSpan) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Throttle(flow, n, per, maxBurst, mode)

    /// Sends elements downstream with speed limited to n/per timeout. In other words, this stage set the maximum rate
    /// for emitting messages. This combinator works for streams where all elements have the same cost or length.
    ///
    /// Throttle implements the token bucket model. There is a bucket with a given token capacity (burst size or maximumBurst).
    /// Tokens drops into the bucket at a given rate and can be "spared" for later use up to bucket capacity
    /// to allow some burstyness. Whenever stream wants to send an element, it takes as many
    /// tokens from the bucket as number of elements. If there isn't any, throttle waits until the
    /// bucket accumulates enough tokens.
    let inline throttleWeighted (mode: ThrottleMode) (maxBurst: int) (n: int) (per: TimeSpan) (costFn: 'u -> int) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Throttle(flow, n, per, maxBurst, Func<_,_>(costFn), mode)

    /// Attaches the given sink graph to this flow, meaning that elements that passes
    /// through will also be sent to the sink.
    let inline alsoToMat (sink: #IGraph<SinkShape<'u>, 'mat2>) (matFn: 'mat -> 'mat2 -> 'mat3) (flow) : Flow<'t, 'u, 'mat3> =
        FlowOperations.AlsoToMaterialized(flow, sink, Func<_,_,_>(matFn))

    /// Attaches the given sink to this flow, meaning that elements that passes
    /// through will also be sent to the sink.
    let inline alsoTo (sink: #IGraph<SinkShape<'u>, 'mat>) (flow) : Flow<'t, 'u, 'mat> = FlowOperations.AlsoTo(flow, sink)

    /// Materializes to Async that completes on getting termination message.
    /// The task completes with success when received complete message from upstream or cancel
    /// from downstream. It fails with the same error when received error message from
    /// downstream.
    let inline watchTermination (matFn: 'mat -> Async<unit> -> 'mat2) (flow) : Flow<_, _, 'mat2> =
        FlowOperations.WatchTermination(flow, Func<_,_,_>(fun m t -> matFn m (t |> Async.AwaitTask)))

    /// Materializes to IFlowMonitor that allows monitoring of the the current flow. All events are propagated
    /// by the monitor unchanged. Note that the monitor inserts a memory barrier every time it processes an
    /// event, and may therefor affect performance.
    /// The 'combine' function is used to combine the IFlowMonitor with this flow's materialized value.
    let inline monitor (combine: 'mat -> IFlowMonitor -> 'mat2) (flow) : Flow<'t, 'u, 'mat2> =
        FlowOperations.Monitor(flow, Func<_,_,_>(combine))

    /// Detaches upstream demand from downstream demand without detaching the
    /// stream rates; in other words acts like a buffer of size 1.
    let inline detach (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Detach(flow)

    /// Delays the initial element by the specified duration.
    let inline initDelay (delay: TimeSpan) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.InitialDelay(flow, delay)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline log (stageName: string) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Log(flow, stageName)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logf (stageName: string) format (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Log(flow, stageName, Func<_,_>(format))

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logWith (stageName: string) (logger: Akka.Event.ILoggingAdapter) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Log(flow, stageName, log = logger)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logWithf (stageName: string) (format: 'u -> string) (logger: Akka.Event.ILoggingAdapter) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Log(flow, stageName, Func<_,_>(format >> box), logger)

    /// Combine the elements of current flow and the given source into a stream of tuples.
    let inline zip (source: #IGraph<SourceShape<'w>, 'mat>) (flow) : Flow<'t, 'u * 'w, 'mat> =
        FlowOperations.ZipWith(flow, source, Func<_,_,_>(fun x y -> (x,y)))

    /// Put together the elements of current flow and the given source
    /// into a stream of combined elements using a combiner function.
    let inline zipWith (source: #IGraph<SourceShape<'w>, 'mat>) (combineFn: 'u -> 'w -> 'x) (flow) : Flow<'t, 'x, 'mat> =
        FlowOperations.ZipWith(flow, source, Func<_,_,_>(combineFn))

    /// Interleave is a deterministic merge of the given source with elements of this flow.
    /// It first emits number of elements from this flow to downstream, then - same amount for other
    /// source, then repeat process.
    let inline interleave (count: int) (source: #IGraph<SourceShape<_>, 'mat>) (flow) : Flow<'t, _, 'mat> =
        FlowOperations.Interleave(flow, source, count)

    /// Interleave is a deterministic merge of the given source with elements of this flow.
    /// It first emits count number of elements from this flow to downstream, then - same amount for source,
    /// then repeat process.
    let inline interleaveMat (count: int) (source: #IGraph<SourceShape<_>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (flow) : Flow<'t, _, 'mat3> =
        FlowOperations.InterleaveMaterialized(flow, source, count, Func<_,_,_>(combineFn))

    /// Merge the given source to this flow, taking elements as they arrive from input streams,
    /// picking randomly when several elements ready.
    let inline merge (source: #IGraph<SourceShape<'u>, 'mat>) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Merge(flow, source)

    /// Merge the given source to this flow, taking elements as they arrive from input streams,
    /// picking randomly when several elements ready.
    let inline mergeMat (source: #IGraph<SourceShape<'u>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (flow) : Flow<'t, 'u, 'mat3> =
        FlowOperations.MergeMaterialized(flow, source, Func<_,_,_>(combineFn))

    /// Merge the given source to this flow, taking elements as they arrive from input streams,
    /// picking always the smallest of the available elements(waiting for one element from each side
    /// to be available). This means that possible contiguity of the input streams is not exploited to avoid
    /// waiting for elements, this merge will block when one of the inputs does not have more elements(and
    /// does not complete).
    let inline mergeSort (source: #IGraph<SourceShape<'u>, 'mat>) sortFn (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.MergeSorted(flow, source, Func<_,_,_>(sortFn))

    /// Concatenate the given source to this flow, meaning that once this
    /// Flow’s input is exhausted and all result elements have been generated,
    /// the Source’s elements will be produced.
    let inline concat (source: #IGraph<SourceShape<'u>, 'mat>) (flow) : Flow<'t, 'u, 'mat> =
        FlowOperations.Concat(flow, source)

    /// Prepend the given source to this flow, meaning that before elements
    /// are generated from this flow, the Source's elements will be produced until it
    /// is exhausted, at which point Flow elements will start being produced.
    let inline prepend (source: #IGraph<SourceShape<_>, 'mat>) (flow) : Flow<'t, _, 'mat> =
        FlowOperations.Prepend(flow, source)

    /// Provides a secondary source that will be consumed if this stream completes without any
    /// elements passing by. As soon as the first element comes through this stream, the alternative
    /// will be cancelled.
    ///
    /// Note that this Flow will be materialized together with the Source and just kept
    /// from producing elements by asserting back-pressure until its time comes or it gets
    /// cancelled.
    let inline orElse (other: #IGraph<SourceShape<'t>, 'mat>) (flow) : Flow<'t, 't, 'mat> =
        FlowOperations.OrElse(flow, other)
        
    /// Provides a secondary source that will be consumed if this source completes without any
    /// elements passing by. As soon as the first element comes through this stream, the alternative
    /// will be cancelled.
    let inline orElseMat (combine: 'mat -> 'mat2 -> 'mat3) (other: #IGraph<SourceShape<'t>, 'mat2>) (flow) : Flow<'t, 't, 'mat3> =
        FlowOperations.OrElseMaterialized(flow, other, Func<_,_,_>(combine))

    /// Put an asynchronous boundary around this Flow.
    let inline async (flow: Flow<'t, 'u, 'mat>) : Flow<'t, 'u, 'mat> = flow.Async()

    /// Add a name attribute to this Source.
    let inline named (name: string) (flow: Flow<'t, 'u, 'mat>) : Flow<'t, 'u, 'mat> = flow.Named(name)

    /// Nests the current Source and returns a Source with the given Attributes
    let inline attrs (a: Attributes) (flow: Flow<'t, 'u, 'mat>) : Flow<'t, 'u, 'mat> = flow.WithAttributes(a)

    /// Transform the materialized value of this Flow, leaving all other properties as they were.
    let inline mapMatValue (fn: 'mat -> 'mat2) (flow: Flow<'inp,'out,'mat>) : Flow<'inp,'out,'mat2> = 
        flow.MapMaterializedValue(Func<_,_> fn)

    /// Transform this flow by appending the given processing steps.
    /// The materialized value of the combined source will be the materialized
    /// value of the current flow (ignoring the other flow’s value).
    let inline via (other: #IGraph<FlowShape<'u, 'w>, 'mat2>) (flow: Flow<'t, 'u, 'mat>) : Flow<'t, 'w, 'mat> =
        flow.Via(other)

    /// Transform this flow by appending the given processing steps.
    /// The combine function is used to compose the materialized values of this flow and that
    /// flow into the materialized value of the resulting Flow.
    let inline viaMat (other: #IGraph<FlowShape<'u,'w>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (flow: Flow<'t, 'u, 'mat>) : Flow<'t, 'w, 'mat3> =
        flow.ViaMaterialized(other, Func<_,_,_>(combineFn))

    /// Connect this source to a sink concatenating the processing steps of both.
    let inline toMat (sink: #IGraph<SinkShape<'u>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (flow: Flow<'t, 'u, 'mat>) : Sink<'t, 'mat3> =
        flow.ToMaterialized(sink, Func<_,_,_>(combineFn))

    /// An identity flow, mapping incoming values to themselves.
    let inline id<'t,'mat> = Flow.Identity<'t,'mat>()

    /// Creates flow from the Reactive Streams Processor.
    let inline ofProc (fac: unit -> #Reactive.Streams.IProcessor<_,_>) =
        Flow.FromProcessor(Func<_>(fun () -> upcast fac()))
        
    /// Creates flow from the Reactive Streams Processor and returns a materialized value.
    let inline ofProcMat (fac: unit -> #Reactive.Streams.IProcessor<'i,'o> * 'mat) =
        Flow.FromProcessorMaterialized(Func<_>(fun () -> let proc, mat = fac() in Tuple.Create<_,_>(upcast proc, mat)))

    /// Builds a flow from provided sink and source graphs.
    let inline ofSinkAndSource (sink: #IGraph<SinkShape<'i>,'mat>) (source: #IGraph<SourceShape<'o>,'mat>) =
        Flow.FromSinkAndSource(sink, source)
        
    /// Builds a flow from provided sink and source graphs returning a materialized value being result of combineFn.
    let inline ofSinkAndSourceMat (sink: #IGraph<SinkShape<'i>,'mat>) (combineFn: 'mat -> 'mat2 -> 'mat3) (source: #IGraph<SourceShape<'o>,'mat2>) =
        Flow.FromSinkAndSource(sink, source, Func<_,_,_>(combineFn))
        
    /// Joins two flows by cross connecting their inputs and outputs.
    let inline join (other: #IGraph<FlowShape<'i,'o>,'mat2>) (flow: Flow<'o,'i,'mat>): IRunnableGraph<'mat> = flow.Join(other)

    /// Joins two flows by cross connecting their inputs and outputs with a materialized value determined by `fn` function.
    let inline joinMat (other: #IGraph<FlowShape<'i,'o>,'mat2>) (fn: 'mat -> 'mat2 -> 'mat3) (flow: Flow<_,_,'mat>): IRunnableGraph<'mat3> = 
        flow.JoinMaterialized(other, Func<'mat,'mat2,'mat3>(fn))

    let inline joinBidi (bidi: #IGraph<BidiShape<'i1,'o1,'i2,'o2>,'mat2>) (flow: Flow<'o2,'i1,'mat> ) = flow.Join(bidi)

    let inline joinBidiMat (bidi: #IGraph<BidiShape<'i1,'o1,'i2,'o2>,'mat2>) (fn: 'mat -> 'mat2 -> 'mat3) (flow: Flow<'o2,'i1,'mat> ) =
        flow.JoinMaterialized(bidi, Func<'mat,'mat2,'mat3>(fn))

    let inline iter (fn: 'o -> unit) (flow: Flow<'i,'o,'mat>): Flow<'i,'o,'mat> = flow |> map (fun x -> fn x; x)

    let retry (retryWith: 's -> ('i * 's) option) (flow: Flow<('i * 's), (Result< 'o, exn> * 's), 'mat>)  =
        let flow: Flow<'i * 's, Akka.Util.Result<'o> * 's, 'mat> = 
            flow |> map (fun (x, s) -> 
                match x with 
                | Ok x -> (Akka.Util.Result.Success x), s
                | Error e -> (Akka.Util.Result.Failure e), s)
                
        Retry.Create(flow, fun s -> 
            match retryWith s with
            | Some x -> x
            | None -> Unchecked.defaultof<_>)
        |> Flow.FromGraph // I convert IGraph to Flow here in order to map it below. Should I?
        |> map (fun (x, s) ->
            if x.IsSuccess then Ok x.Value, s
            else Result.Error x.Exception, s) 

    let retryLimited limit (retryWith: 's -> ('i * 's) seq option) (flow: Flow<('i * 's), (Result< 'o, exn> * 's), 'mat>)  =
        let flow: Flow<'i * 's, Akka.Util.Result<'o> * 's, 'mat> = 
            flow |> map (fun (x, s) -> 
                match x with 
                | Ok x -> (Akka.Util.Result.Success x), s
                | Error e -> (Akka.Util.Result.Failure e), s)
                
        Retry.Concat(limit, flow, fun s -> 
            match retryWith s with
            | Some x -> x
            | None -> Unchecked.defaultof<_>)
        |> Flow.FromGraph // I convert IGraph to Flow here in order to map it below. Should I?
        |> map (fun (x, s) ->
            if x.IsSuccess then Ok x.Value, s
            else Result.Error x.Exception, s) 
    
    /// Joins a provided flow with given sink, returning a new sink in the result.
    let inline toSink (sink: #IGraph<SinkShape<'out>,'mat2>) (flow: Flow<'inp,'out,'mat>) : Sink<'inp,'mat> = 
        flow.To(sink)
    
    /// Joins a provided flow with given sink, returning a new sink in the result.
    let inline toProcessor (flow: Flow<'inp,'out,'mat>) : IRunnableGraph<IProcessor<'inp,'out>> = 
        flow.ToProcessor()
    
    /// Filters our consecutive duplicated elements from the stream (uniqueness is recognized 
    /// by provided function).
    let inline deduplicate (eq: 'out -> 'out -> bool) (flow: Flow<'inp,'out,'mat>) : Flow<'inp, 'out, 'mat> =
        flow.Via(Deduplicate(eq))

    /// Materializes into an Async of switch which provides method flip that stops or restarts the flow of elements passing through the stage. 
    /// As long as the valve is closed it will backpressure.
    /// Note that closing the valve could result in one element being buffered inside the stage, and if the stream completes or fails while being closed, that element may be lost.
    let inline valve (mode: SwitchMode) : Flow<'t, 't, Async<IValveSwitch>> =
        Flow.FromGraph(new Valve<_>(mode)) |> mapMatValue Async.AwaitTask

    /// Pulse stage signals demand only once every "pulse" interval and then back-pressures.
    /// Requested element is emitted downstream if there is demand.
    /// It can be used to implement simple time-window processing
    /// where data is aggregated for predefined amount of time and the computed aggregate is emitted once per this time.
    let inline pulse (initiallyOpen: bool) (interval: TimeSpan) (flow: Flow<'t, 't, 'mat>) : Flow<'t, 't, 'mat> =
        flow.Via (new Pulse<_>(interval, initiallyOpen))
    
    /// Flow stage for universal delay management, allows to manage delay through a given strategy.
    let managedDelay (strategy: DelayStrategy<'t>) (flow: Flow<'t, 't, 'mat>) : Flow<'t, 't, 'mat> =
        let strategySupplier () : IDelayStrategy<'t> =
            match strategy with
            | FixedDelay delay -> upcast FixedDelay<_>(delay)
            | LinearIncreasingDelay(init, step, max:TimeSpan, increaseCheck) ->
                upcast LinearIncreasingDelay<_>(step, Func<_,_>(increaseCheck), init, max)
            | CustomDelay fn ->
                { new IDelayStrategy<'t> with
                    member x.NextDelay(element:'t) : TimeSpan = fn element }
            
        flow.Via (new DelayFlow<_>(Func<_> strategySupplier))