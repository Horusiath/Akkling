//-----------------------------------------------------------------------
// <copyright file="SubFlow.fs" company="Akka.NET Project">
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

[<RequireQualifiedAccess>]
module SubFlow =
    open Stages

    /// Recover allows to send last element on failure and gracefully complete the stream
    /// Since the underlying failure signal onError arrives out-of-band, it might jump over existing elements.
    /// This stage can recover the failure signal, but not the skipped elements, which will be dropped.
    let recover (fn: exn -> 'out option) (subFlow: SubFlow<'out, 'mat, 'closed>) : SubFlow<'out option, 'mat, 'closed> =
        SubFlowOperations.Recover(subFlow, Func<exn, Util.Option<'out>>(fn >> toCsOption)).Select(Func<Util.Option<'out>, 'out option>(ofCsOption))
        
    /// RecoverWith allows to switch to alternative Source on subFlow failure. It will stay in effect after
    /// a failure has been recovered so that each time there is a failure it is fed into the <paramref name="partialFunc"/> and a new
    /// Source may be materialized.
    let inline recoverWithRetries (attempts: int) (fn: exn -> #IGraph<SourceShape<'out>, 'mat>) (subFlow) : SubFlow<'out, 'mat, 'closed> =
        SubFlowOperations.RecoverWithRetries(subFlow, Func<exn, Akka.Streams.IGraph<SourceShape<_>, _>>(fun ex -> upcast fn ex), attempts)
        
    /// While similar to recover, this stage can be used to transform an error signal to a different one without logging
    /// it as an error in the process. So in that sense it is NOT exactly equivalent to Recover(e => throw e2) since Recover
    /// would log the e2 error. 
    let inline mapError (fn: exn -> #exn) (subFlow) : SubFlow<'inp,'out,'closed> =
        SubFlowOperations.SelectError(subFlow, Func<exn, exn>(fun e -> upcast fn e))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step.
    let inline map (fn: 'u -> 'w) (subFlow: SubFlow<'u, 'mat, 'closed>) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.Select(subFlow, Func<_, _>(fn))

    /// Transform each input element into a sequence of output elements that is
    /// then flattened into the output stream.
    ///
    /// The returned sequence MUST NOT contain NULL values,
    /// as they are illegal as stream elements - according to the Reactive Streams specification.
    let inline collect (fn: 'u-> #seq<'w>) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.SelectMany(subFlow, Func<_, _>(fun x -> upcast fn x))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step. The function returns an Async and the
    /// value of that computation will be emitted downstream. The number of tasks
    /// that shall run in parallel is given as the first argument.
    /// These tasks may complete in any order, but the elements that
    /// are emitted downstream are in the same order as received from upstream.
    let inline asyncMap (parallelism: int) (fn: 'u -> Async<'w>) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.SelectAsync(subFlow, parallelism, Func<_, _>(fun x -> fn(x) |> Async.StartAsTask))

    /// Transform this stream by applying the given function to each of the elements
    /// as they pass through this processing step. The function returns an Async and the
    /// value of that computation will be emitted downstreams. As many asyncs as requested elements by
    /// downstream may run in parallel and each processed element will be emitted dowstream
    /// as soon as it is ready, i.e. it is possible that the elements are not emitted downstream
    /// in the same order as received from upstream.
    let inline asyncMapUnordered (parallelism: int) (fn: 'u -> Async<'w>) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.SelectAsyncUnordered(subFlow, parallelism, Func<_, _>(fun x -> fn(x) |> Async.StartAsTask))

    /// Only pass on those elements that satisfy the given predicate function.
    let inline filter (pred: 'u -> bool) (subFlow) : SubFlow<'u, 'mat, 'closed> = SubFlowOperations.Where(subFlow, Predicate<_>(pred))

    /// Terminate processing (and cancel the upstream publisher) after predicate function
    /// returns false for the first time. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    let inline takeWhile (pred: 'u -> bool) (subFlow) : SubFlow<'u, 'mat, 'closed> = SubFlowOperations.TakeWhile(subFlow, Predicate<_>(pred))

    /// Discard elements at the beginning of the stream while predicate function is true.
    /// All elements will be taken after function returns false first time.
    let inline skipWhile (pred: 'u -> bool) (subFlow) : SubFlow<'u, 'mat, 'closed> = SubFlowOperations.SkipWhile(subFlow, Predicate<_>(pred))

    /// Transform this stream by applying the given function to each of the elements
    /// on which the function is defined (read: returns Some) as they pass through this processing step.
    /// Non-matching elements are filtered out.
    let inline choose (fn: 'u -> 'w option) (subFlow) : SubFlow<'w, 'mat, 'closed> = 
        SubFlowOperations.Collect(subFlow, Func<_, _>(fn)).Where(Predicate<_>(Option.isSome)).Select(Func<_,_>(Option.get))

    /// Chunk up this stream into groups of the given size, with the last group
    /// possibly smaller than requested due to end-of-stream.
    /// N must be positive, otherwise ArgumentException is thrown.
    let inline windowed (n: int) (subFlow) : SubFlow<'u seq, 'mat, 'closed> = SubFlowOperations.Grouped(subFlow, n)

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
    let inline limit (max: int64) (subFlow) : SubFlow<'u, 'mat, 'closed> = SubFlowOperations.Limit(subFlow, max)

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
    let inline limitWeighted (max: int64) (costFn: 'u -> int64) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.LimitWeighted(subFlow, max, Func<_, _>(costFn))

    /// Apply a sliding window over the stream and return the windows as groups of elements, with the last group
    /// possibly smaller than requested due to end-of-stream.
    let inline sliding (n: int) (subFlow) : SubFlow<'u seq, 'mat, 'closed> = SubFlowOperations.Sliding(subFlow, n)

    /// Apply a sliding window over the stream and return the windows as groups of elements, with the last group
    /// possibly smaller than requested due to end-of-stream.
    let inline slidingBy (n: int) (step: int) (subFlow) : SubFlow<'u seq, 'mat, 'closed> = SubFlowOperations.Sliding(subFlow, n, step)

    /// Similar to fold but is not a terminal operation,
    /// emits its current value which starts at zero and then
    /// applies the current and next value to the given function,
    /// emitting the next current value.
    ///
    /// If the function throws an exception and the supervision decision is
    /// restart current value starts at zero again the stream will continue.
    let inline scan (folder: 'w -> 'u -> 'w) (state: 'w) (subFlow) : SubFlow<'w, 'mat, 'closed> = 
        SubFlowOperations.Scan(subFlow, state, Func<_,_,_>(folder))

    /// Similar to scan but with a asynchronous function, emits its current value which 
    /// starts at zero and then applies the current and next value to the given function
    /// emitting an Async that resolves to the next current value.
    let inline asyncScan (folder: 'state -> 'inp -> Async<'state>) (zero: 'state) (subFlow) : SubFlow<'state,'mat,'closed> =
        SubFlowOperations.ScanAsync(subFlow, zero, Func<_,_,_>(fun s e -> folder s e |> Async.StartAsTask))

    /// Similar to scan but only emits its result when the upstream completes,
    /// after which it also completes. Applies the given function towards its current and next value,
    /// yielding the next current value.
    ///
    /// If the function throws an exception and the supervision decision is
    /// restart current value starts at state again the stream will continue.
    let inline fold (folder: 'w -> 'u -> 'w) (state: 'w) (subFlow) : SubFlow<'w, 'mat, 'closed> = 
        SubFlowOperations.Aggregate(subFlow, state, Func<_,_,_>(folder))
        
    /// Similar to fold but with an asynchronous function.
    /// Applies the given function towards its current and next value,
    /// yielding the next current value.
    /// 
    /// If the function fold returns a failure and the supervision decision is
    /// Directive.Restart current value starts at state again
    /// the stream will continue.
    let inline asyncFold (folder: 'w -> 'u -> Async<'w>) (state: 'w) (subFlow) : SubFlow<'w, 'mat, 'closed> = 
        SubFlowOperations.AggregateAsync(subFlow, state, Func<_,_,_>(fun acc x -> folder acc x |> Async.StartAsTask))

    /// Similar to fold but uses first element as zero element.
    /// Applies the given function towards its current and next value,
    /// yielding the next current value.
    let inline reduce (acc: 'u -> 'u -> 'u) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Sum(subFlow, Func<_,_,_>(acc))

    /// Intersperses stream with provided element, similar to how string.Join
    /// injects a separator between a collection's elements.
    ///
    /// Additionally can inject start and end marker elements to stream.
    let inline intersperseBounded (start: 'u) (inject: 'u) (finish: 'u) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Intersperse(subFlow, start, inject, finish)

    /// Intersperses stream with provided element, similar to how string.Join
    /// injects a separator between a collection's elements.
    let inline intersperse (inject: 'u) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Intersperse(subFlow, inject)

    /// Chunk up this stream into groups of elements received within a time window,
    /// or limited by the given number of elements, whatever happens first.
    /// Empty groups will not be emitted if no elements are received from upstream.
    /// The last group before end-of-stream will contain the buffered elements
    /// since the previously emitted group.
    let inline groupedWithin (n: int) (timeout: TimeSpan) (subFlow) : SubFlow<'u seq, 'mat, 'closed> = 
        SubFlowOperations.GroupedWithin(subFlow, n, timeout)

    /// Shifts elements emission in time by a specified amount. It allows to store elements
    /// in internal buffer while waiting for next element to be emitted.
    ///
    /// Delay precision is 10ms to avoid unnecessary timer scheduling cycles
    let inline delay (by: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Delay(subFlow, by)

    /// Shifts elements emission in time by a specified amount. It allows to store elements
    /// in internal buffer while waiting for next element to be emitted.Depending on the defined
    /// strategy it might drop elements or backpressure the upstream if
    /// there is no space available in the buffer.
    ///
    /// Delay precision is 10ms to avoid unnecessary timer scheduling cycles
    let inline delayWithStrategy (strategy: DelayOverflowStrategy) (by: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Delay(subFlow, by, Nullable strategy)

    /// Discard the given number of elements at the beginning of the stream.
    /// No elements will be dropped if parameter is zero or negative.
    let inline skip (n: int64) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Skip(subFlow, n)

    /// Discard the elements received within the given duration at beginning of the stream.
    let inline skipWithin (timeout: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.SkipWithin(subFlow, timeout)

    /// Terminate processing (and cancel the upstream publisher) after the given
    /// number of elements. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    ///
    /// The stream will be completed without producing any elements if parameter is zero
    /// or negative.
    let inline take (n: int64) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Take(subFlow, n)

    /// Terminate processing (and cancel the upstream publisher) after the given
    /// duration. Due to input buffering some elements may have been
    /// requested from upstream publishers that will then not be processed downstream
    /// of this step.
    let inline takeWithin (timeout: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.TakeWithin(subFlow, timeout)

    /// Allows a faster upstream to progress independently of a slower subscriber by conflating elements into a summary
    /// until the subscriber is ready to accept them. For example a conflate step might average incoming numbers if the
    /// upstream publisher is faster.
    ///
    /// This version of conflate allows to derive a seed from the first element and change the aggregated type to
    /// be different than the input type. See conflate for a simpler version that does not change types.
    ///
    /// This element only rolls up elements if the upstream is faster, but if the downstream is faster it will not
    /// duplicate elements.
    let inline conflateSeeded (folder: 'w -> 'u -> 'w) (seed: 'u -> 'w) (subFlow) : SubFlow<'w, 'mat, 'closed> = 
        SubFlowOperations.ConflateWithSeed(subFlow, Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by conflating elements into a summary
    /// until the subscriber is ready to accept them. For example a conflate step might average incoming numbers if the
    /// upstream publisher is faster.
    ///
    /// This version of conflate does not change the output type of the stream. See conflateSeeded
    /// for a more flexible version that can take a seed function and transform elements while rolling up.
    ///
    /// This element only rolls up elements if the upstream is faster, but if the downstream is faster it will not
    /// duplicate elements.
    let inline conflate (folder: 'u -> 'u -> 'u) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.Conflate(subFlow, Func<_,_,_>(folder))

    /// Allows a faster upstream to progress independently of a slower subscriber by aggregating elements into batches
    /// until the subscriber is ready to accept them.For example a batch step might store received elements in
    /// an array up to the allowed max limit if the upstream publisher is faster.
    let inline batch (max: int64) (seed: 'u -> 'w) (folder: 'w -> 'u -> 'w) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.Batch(subFlow, max, Func<_,_>(seed), Func<_,_,_>(folder))

    /// Takes up to n elements from the stream and returns a pair containing a strict sequence of the taken element
    /// and a stream representing the remaining elements. If `n` is zero or negative, then this will return a pair
    /// of an empty collection and a stream containing the whole upstream unchanged.
    let inline prefixAndTail (n: int) (subFlow) : SubFlow<'out list * Source<'out, unit>, 'mat, 'closed> = 
        SubFlowOperations.PrefixAndTail(subFlow, n).Select(Func<_,_>(fun (imm, source) -> 
            let s = source.MapMaterializedValue(Func<_,_>(ignore))
            (List.ofSeq imm), s))

    /// Allows a faster upstream to progress independently of a slower subscriber by aggregating elements into batches
    /// until the subscriber is ready to accept them.
    let inline batchWeighted (max: int64) (costFn: 'u -> int64) (seed: 'u -> 'w) (folder: 'w -> 'u -> 'w) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.BatchWeighted(subFlow, max, Func<_,_>(costFn), Func<_,_>(seed), Func<_,_,_>(folder))

    /// Allows a faster downstream to progress independently of a slower publisher by extrapolating elements from an older
    /// element until new element comes from the upstream. For example an expand step might repeat the last element for
    /// the subscriber until it receives an update from upstream.
    ///
    /// This element will never "drop" upstream elements as all elements go through at least one extrapolation step.
    /// This means that if the upstream is actually faster than the upstream it will be backpressured by the downstream
    /// subscriber.
    ///
    /// Expand does not support restart and resume directives. Exceptions from the extrapolate function will complete the stream with failure.
    let inline expand (extrapolate: 'u -> #seq<'w> ) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.Expand(subFlow, Func<_,_>(fun x -> upcast extrapolate x))

    /// Adds a fixed size buffer in the subFlow that allows to store elements from a faster upstream until it becomes full.
    /// Depending on the defined strategy it might drop elements or backpressure the upstream if
    /// there is no space available
    let inline buffer (strategy: OverflowStrategy) (n: int) (subFlow) : SubFlow<'t, 'u, 'mat> = SubFlowOperations.Buffer(subFlow, n, strategy)
            
    /// Transform each input element into a source of output elements that is
    /// then flattened into the output stream by concatenation,
    /// fully consuming one Source after the other.
    let inline collectMap (flatten: 'u -> #IGraph<SourceShape<'w>, 'mat>) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.ConcatMany(subFlow, Func<_,_>(fun x -> upcast flatten x))

    /// Transform each input element into a source of output elements that is
    /// then flattened into the output stream by merging, where at most breadth
    /// substreams are being consumed at any given time.
    let inline collectMerge (breadth: int) (flatten: 'u -> #IGraph<SourceShape<'w>, 'mat>) (subFlow) : SubFlow<'w, 'mat, 'closed> =
        SubFlowOperations.MergeMany(subFlow, breadth, Func<_,_>(fun x -> upcast flatten x))

    /// Combine the elements of current flow into a stream of tuples consisting
    /// of all elements paired with their index. Indices start at 0.
    let inline zipi (subFlow) : SubFlow<'out * int64, 'mat, 'closed> =
        SubFlowOperations.ZipWithIndex(subFlow)

    /// If the first element has not passed through this stage before the provided timeout, the stream is failed
    /// with a TimeoutException
    let inline initWithin (timeout: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.InitialTimeout(subFlow, timeout)

    /// If the completion of the stream does not happen until the provided timeout, the stream is failed
    /// with a TimeoutException.
    let inline completeWithin (timeout: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.CompletionTimeout(subFlow, timeout)

    /// If the time between two processed elements exceed the provided timeout, the stream is failed
    /// with a TimeoutException.
    let inline idleTimeout (timeout: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.IdleTimeout(subFlow, timeout)
        
    /// If the time between the emission of an element and the following downstream demand exceeds the provided timeout,
    /// the stream is failed with a TimeoutException. The timeout is checked periodically,
    /// so the resolution of the check is one period (equals to timeout value).
    let inline backpressureTimeout (timeout: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.BackpressureTimeout(subFlow, timeout)

    /// Injects additional elements if the upstream does not emit for a configured amount of time. In other words, this
    /// stage attempts to maintains a base rate of emitted elements towards the downstream.
    let inline keepAlive (timeout: TimeSpan) (injectFn: unit -> 'u) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.KeepAlive(subFlow, timeout, Func<_>(injectFn))

    /// Sends elements downstream with speed limited to n/per timeout. In other words, this stage set the maximum rate
    /// for emitting messages. This combinator works for streams where all elements have the same cost or length.
    ///
    /// Throttle implements the token bucket model. There is a bucket with a given token capacity (burst size or maximumBurst).
    /// Tokens drops into the bucket at a given rate and can be "spared" for later use up to bucket capacity
    /// to allow some burstyness. Whenever stream wants to send an element, it takes as many
    /// tokens from the bucket as number of elements. If there isn't any, throttle waits until the
    /// bucket accumulates enough tokens.
    let inline throttle (mode: ThrottleMode) (maxBurst: int) (n: int) (per: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Throttle(subFlow, n, per, maxBurst, mode)

    /// Sends elements downstream with speed limited to n/per timeout. In other words, this stage set the maximum rate
    /// for emitting messages. This combinator works for streams where all elements have the same cost or length.
    ///
    /// Throttle implements the token bucket model. There is a bucket with a given token capacity (burst size or maximumBurst).
    /// Tokens drops into the bucket at a given rate and can be "spared" for later use up to bucket capacity
    /// to allow some burstyness. Whenever stream wants to send an element, it takes as many
    /// tokens from the bucket as number of elements. If there isn't any, throttle waits until the
    /// bucket accumulates enough tokens.
    let inline throttleWeighted (mode: ThrottleMode) (maxBurst: int) (n: int) (per: TimeSpan) (costFn: 'u -> int) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Throttle(subFlow, n, per, maxBurst, Func<_,_>(costFn), mode)

    /// Attaches the given sink graph to this subFlow, meaning that elements that passes
    /// through will also be sent to the sink.
    let inline alsoToMat (sink: #IGraph<SinkShape<'u>, 'mat2>) (matFn: 'mat -> 'mat2 -> 'mat3) (subFlow) : SubFlow<'u, 'mat3, 'closed> =
        SubFlowOperations.AlsoToMaterialized(subFlow, sink, Func<_,_,_>(matFn))

    /// Attaches the given sink to this subFlow, meaning that elements that passes
    /// through will also be sent to the sink.
    let inline alsoTo (sink: #IGraph<SinkShape<'u>, 'mat>) (subFlow) : SubFlow<'u, 'mat, 'closed> = 
        SubFlowOperations.AlsoTo(subFlow, sink)

    /// Materializes to Async that completes on getting termination message.
    /// The task completes with success when received complete message from upstream or cancel
    /// from downstream. It fails with the same error when received error message from
    /// downstream.
    let inline watchTermination (matFn: 'mat -> Async<unit> -> 'mat2) (subFlow) : SubFlow<_, _, 'mat2> =
        SubFlowOperations.WatchTermination(subFlow, Func<_,_,_>(fun m t -> matFn m (t |> Async.AwaitTask)))

    /// Detaches upstream demand from downstream demand without detaching the
    /// stream rates; in other words acts like a buffer of size 1.
    let inline detach (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Detach(subFlow)

    /// Delays the initial element by the specified duration.
    let inline initDelay (delay: TimeSpan) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.InitialDelay(subFlow, delay)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline log (stageName: string) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Log(subFlow, stageName)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logf (stageName: string) format (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Log(subFlow, stageName, Func<_,_>(format))

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logWith (stageName: string) (logger: Akka.Event.ILoggingAdapter) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Log(subFlow, stageName, log = logger)

    /// Logs elements flowing through the stream as well as completion and erroring.
    let inline logWithf (stageName: string) (format: 'u -> string) (logger: Akka.Event.ILoggingAdapter) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Log(subFlow, stageName, Func<_,_>(format >> box), logger)

    /// Combine the elements of current subFlow and the given source into a stream of tuples.
    let inline zip (source: #IGraph<SourceShape<'w>, 'mat>) (subFlow) : SubFlow<'u * 'w, 'mat, 'closed> =
        SubFlowOperations.ZipWith(subFlow, source, Func<_,_,_>(fun x y -> (x,y)))

    /// Put together the elements of current subFlow and the given source
    /// into a stream of combined elements using a combiner function.
    let inline zipWith (source: #IGraph<SourceShape<'w>, 'mat>) (combineFn: 'u -> 'w -> 'x) (subFlow) : SubFlow<'x, 'mat, 'closed> =
        SubFlowOperations.ZipWith(subFlow, source, Func<_,_,_>(combineFn))

    /// Interleave is a deterministic merge of the given source with elements of this subFlow.
    /// It first emits number of elements from this subFlow to downstream, then - same amount for other
    /// source, then repeat process.
    let inline interleave (count: int) (source: #IGraph<SourceShape<_>, 'mat>) (subFlow) : SubFlow<'t, _, 'mat> =
        SubFlowOperations.Interleave(subFlow, source, count)

    /// Interleave is a deterministic merge of the given source with elements of this subFlow.
    /// It first emits count number of elements from this subFlow to downstream, then - same amount for source,
    /// then repeat process.
    let inline interleaveMat (count: int) (source: #IGraph<SourceShape<_>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (subFlow) : SubFlow<_, 'mat3, 'closed> =
        SubFlowOperations.InterleaveMaterialized(subFlow, source, count, Func<_,_,_>(combineFn))

    /// Merge the given source to this subFlow, taking elements as they arrive from input streams,
    /// picking randomly when several elements ready.
    let inline merge (source: #IGraph<SourceShape<'u>, 'mat>) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Merge(subFlow, source)

    /// Merge the given source to this subFlow, taking elements as they arrive from input streams,
    /// picking randomly when several elements ready.
    let inline mergeMat (source: #IGraph<SourceShape<'u>, 'mat2>) (combineFn: 'mat -> 'mat2 -> 'mat3) (subFlow) : SubFlow<'u, 'mat3, 'closed> =
        SubFlowOperations.MergeMaterialized(subFlow, source, Func<_,_,_>(combineFn))

    /// Merge the given source to this subFlow, taking elements as they arrive from input streams,
    /// picking always the smallest of the available elements(waiting for one element from each side
    /// to be available). This means that possible contiguity of the input streams is not exploited to avoid
    /// waiting for elements, this merge will block when one of the inputs does not have more elements(and
    /// does not complete).
    let inline mergeSort (source: #IGraph<SourceShape<'u>, 'mat>) sortFn (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.MergeSorted(subFlow, source, Func<_,_,_>(sortFn))

    /// Concatenate the given source to this subFlow, meaning that once this
    /// SubFlow’s input is exhausted and all result elements have been generated,
    /// the Source’s elements will be produced.
    let inline concat (source: #IGraph<SourceShape<'u>, 'mat>) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Concat(subFlow, source)

    /// Prepend the given source to this subFlow, meaning that before elements
    /// are generated from this subFlow, the Source's elements will be produced until it
    /// is exhausted, at which point SubFlow elements will start being produced.
    let inline prepend (source: #IGraph<SourceShape<_>, 'mat>) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.Prepend(subFlow, source)
       
    /// Provides a secondary source that will be consumed if this stream completes without any
    /// elements passing by. As soon as the first element comes through this stream, the alternative
    /// will be cancelled.
    ///
    /// Note that this Flow will be materialized together with the Source and just kept
    /// from producing elements by asserting back-pressure until its time comes or it gets
    /// cancelled. 
    let inline orElse (source: #IGraph<SourceShape<'u>, 'mat>) (subFlow) : SubFlow<'u, 'mat, 'closed> =
        SubFlowOperations.OrElse(subFlow, source)

    /// Passes current subflow via another flow, constructing new type of subflow in the result.
    let inline via (flow: #IGraph<FlowShape<'t, 'u>, 'mat2>) (subFlow: SubFlow<'t, 'mat, 'closed>) : SubFlow<'u, 'mat, 'closed> =
         downcast subFlow.Via(flow)
         
    /// Passes current subflow via another flow, resolving a returned materialized value using 
    /// combine function, constructing new type of subflow in the result.
    let inline viaMat (combine: 'mat -> 'mat2 -> 'mat3) (flow: #IGraph<FlowShape<'t, 'u>, 'mat2>) (subFlow: SubFlow<'t, 'mat, 'closed>) : SubFlow<'u, 'mat3, 'closed> =
        downcast subFlow.ViaMaterialized(flow, Func<_,_,_>(combine))

    /// Flatten the sub-flows back into the super-flow by performing a merge
    /// without parallelism limit (i.e. having an unbounded number of sub-flows
    /// active concurrently).
    let inline mergeSubstreams (subFlow: SubFlow<'u, 'mat, 'closed>) : Source<'u, 'mat> =
        downcast subFlow.MergeSubstreams()

    /// Flatten the sub-flows back into the super-flow by performing a merge
    /// with the given parallelism limit. This means that only up to 'n' number of
    /// substreams will be executed at any given time. Substreams that are not
    /// yet executed are also not materialized, meaning that back-pressure will
    /// be exerted at the operator that creates the substreams when the parallelism
    /// limit is reached.
    let inline mergeSubstreamsParallel (n: int) (subFlow: SubFlow<'u, 'mat, 'closed>) : Source<'u, 'mat> =
        downcast subFlow.MergeSubstreamsWithParallelism(n)
        
    /// Filters our consecutive duplicated elements from the stream (uniqueness is recognized 
    /// by provided function).
    let inline deduplicate (eq: 'out -> 'out -> bool) (subFlow: SubFlow<'out,'mat,'closed>) : SubFlow<'out,'mat,'closed> =
        downcast subFlow.Via(Deduplicate(eq))