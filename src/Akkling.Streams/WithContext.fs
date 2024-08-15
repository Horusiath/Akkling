//-----------------------------------------------------------------------
// <copyright file="Source.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open System.Threading.Tasks
open Akka.Streams
open Akka.Streams.Dsl

module FlowWithContext =

    /// Stops automatic context propagation from here and converts this to a regular
    /// stream of a pair of (data, context).
    let inline asFlow (flow: FlowWithContext<_, _, _, _, _>) = flow.AsFlow()

    /// Transform this flow by the regular flow. The given flow must support manual context propagation by
    /// taking and producing tuples of (data, context).
    ///
    /// This can be used as an escape hatch for operations that are not (yet) provided with automatic
    /// context propagation here.
    let inline via (stage: #IGraph<FlowShape<_, _>>) (flow: FlowWithContext<_, _, _, _, _>) = flow.Via(stage)

    /// Transform this flow by the regular flow. The given flow must support manual context propagation by
    /// taking and producing tuples of (data, context).
    ///
    /// This can be used as an escape hatch for operations that are not (yet) provided with automatic
    /// context propagation here.
    ///
    /// The `fn` function is used to compose the materialized values of this flow and that
    /// flow into the materialized value of the resulting Flow.
    let inline viaMat (fn: 't -> 'u -> 'w) (stage: #IGraph<FlowShape<_, _>>) (flow: FlowWithContext<_, _, _, _, _>) =
        flow.ViaMaterialized(stage, Func<_, _, _>(fn))

    /// Context-preserving variant of `Flow.map`.
    let inline map (fn: 'a -> 'b) (flow: FlowWithContext<_, _, _, _, _>) = flow.Select(Func<_, _>(fn))

    /// Context-preserving variant of `Flow.collect`.
    let inline collect (fn: 't -> seq<'u>) (flow: FlowWithContext<_, _, _, _, _>) = flow.SelectConcat(Func<_, _>(fn))

    /// Context-preserving variant of `Flow.asyncMap`.
    let inline asyncMap (parallelism: int) (fn: 'a -> Async<'b>) (flow: FlowWithContext<_, _, _, _, _>) =
        flow.SelectAsync(parallelism, Func<_, _>(fn >> Async.StartAsTask<'b>))

    /// Context-preserving variant of `Flow.taskMap`.
    let inline taskMap (parallelism: int) (fn: 'a -> Task<'b>) (flow: FlowWithContext<_, _, _, _, _>) =
        flow.SelectAsync(parallelism, Func<_, _>(fn))

    /// Context-preserving variant of `Flow.choose`.
    let inline choose (fn: 't -> 'u option) (flow: FlowWithContext<_, _, _, _, _>) =
        flow.Collect(Func<_, _>(fn)).Select(Func<_, _>(Option.get))

    /// Context-preserving variant of `Flow.filter`.
    let inline filter (fn: 't -> bool) (flow: FlowWithContext<_, _, _, _, _>) = flow.Where(Func<_, _>(fn))

    /// Context-preserving variant of `Flow.windowed`.
    let inline windowed (n: int) (flow: FlowWithContext<_, _, _, _, _>) = flow.Grouped(n)

    /// Context-preserving variant of `Flow.sliding`.
    let inline sliding (n: int) (flow: FlowWithContext<_, _, _, _, _>) = flow.Sliding(n)

    /// Apply the given function to each context element (leaving the data elements unchanged).
    let inline mapContext (fn: 'ctx -> 'ctx2) (flow: FlowWithContext<_, _, _, _, _>) =
        flow.SelectContext(Func<'ctx, 'ctx2>(fn))


module SourceWithContext =

    /// Stops automatic context propagation from here and converts this to a regular
    /// stream of a pair of (data, context).
    let inline asSource (source: SourceWithContext<_, _, _>) = source.AsSource()

    /// Transform this source by the regular source. The given flow must support manual context propagation by
    /// taking and producing tuples of (data, context).
    ///
    /// This can be used as an escape hatch for operations that are not (yet) provided with automatic
    /// context propagation here.
    let inline via (stage: #IGraph<FlowShape<_, _>>) (source: SourceWithContext<_, _, _>) = source.Via(stage)

    /// Transform this flow by the regular flow. The given flow must support manual context propagation by
    /// taking and producing tuples of (data, context).
    ///
    /// This can be used as an escape hatch for operations that are not (yet) provided with automatic
    /// context propagation here.
    ///
    /// The `fn` function is used to compose the materialized values of this flow and that
    /// flow into the materialized value of the resulting Flow.
    let inline viaMat (fn: 't -> 'u -> 'w) (stage: #IGraph<FlowShape<_, _>>) (source: SourceWithContext<_, _, _>) =
        source.ViaMaterialized(stage, Func<_, _, _>(fn))

    /// Context-preserving variant of `Source.map`.
    let inline map (fn: 'a -> 'b) (source: SourceWithContext<_, _, _>) = source.Select(Func<_, _>(fn))

    /// Context-preserving variant of `Source.collect`.
    let inline collect (fn: 't -> seq<'u>) (source: SourceWithContext<_, _, _>) = source.SelectConcat(Func<_, _>(fn))

    /// Context-preserving variant of `Source.asyncMap`.
    let inline asyncMap (parallelism: int) (fn: 'a -> Async<'b>) (source: SourceWithContext<_, _, _>) =
        source.SelectAsync(parallelism, Func<_, _>(fn >> Async.StartAsTask<'b>))

    /// Context-preserving variant of `Source.taskMap`.
    let inline taskMap (parallelism: int) (fn: 'a -> Task<'b>) (source: SourceWithContext<_, _, _>) =
        source.SelectAsync(parallelism, Func<_, _>(fn))

    /// Context-preserving variant of `Source.choose`.
    let inline choose (fn: 't -> 'u option) (source: SourceWithContext<_, _, _>) =
        source.Collect(Func<_, _>(fn)).Select(Func<_, _>(Option.get))

    /// Context-preserving variant of `Source.filter`.
    let inline filter (fn: 't -> bool) (source: SourceWithContext<_, _, _>) = source.Where(Func<_, _>(fn))

    /// Context-preserving variant of `Source.windowed`.
    let inline windowed (n: int) (source: SourceWithContext<_, _, _>) = source.Grouped(n)

    /// Context-preserving variant of `Source.sliding`.
    let inline sliding (n: int) (source: SourceWithContext<_, _, _>) = source.Sliding(n)

    /// Apply the given function to each context element (leaving the data elements unchanged).
    let inline mapContext (fn: 'ctx -> 'ctx2) (source: SourceWithContext<_, _, _>) =
        source.SelectContext(Func<'ctx, 'ctx2>(fn))
