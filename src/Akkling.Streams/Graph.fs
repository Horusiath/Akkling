//-----------------------------------------------------------------------
// <copyright file="Source.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open Akka.Streams
open Akka.Streams.Dsl

[<AutoOpen>]
module Operators =

    /// Links provided partial graph to another flow on the right.
    let inline ( >>= ) (op: GraphDsl.ForwardOps<'out, 'mat>) (flow: Flow<_, 'out2, unit>) = 
        op.Via(flow.MapMaterializedValue(Func<_,_>(fun () -> Akka.NotUsed.Instance)))
        
    /// Links provided operator to input sink on the right.
    let inline ( >>=* ) (op: GraphDsl.ForwardOps<'out, 'mat>) (sink: Sink<_, 'mat>) =
        op.To(sink)
        
    /// Links provided partial graph to another flow on the left.
    let inline ( =<< ) (flow: Flow<_, 'out2, unit>) (op: GraphDsl.ForwardOps<'out, 'mat>) =
        op.Via(flow.MapMaterializedValue(Func<_,_>(fun () -> Akka.NotUsed.Instance)))

    /// Links provided partial graph to another input sink on the left.
    let inline ( *=<< ) (sink: Sink<_, 'mat>) (op: GraphDsl.ForwardOps<'out, 'mat>) =
        op.To(sink)
        
    /// Links output of the first flow as input of a second flow, producing new flow in result.
    let inline ( =>> ) (flow1: Flow<_,_,_>) (flow2: Flow<_,_,_>) =
        flow1.Via(flow2)
        
    /// Sets a second flow on top of the first one.
    let inline ( <=> ) (bidi1: BidiFlow<_,_,_,_,_>) (bidi2: BidiFlow<_,_,_,_,_>) =
        bidi1.Atop(bidi2)
        
    /// Completes bidi flow by joining it to a final flow.
    let inline ( <=>* ) (bidi: BidiFlow<_,_,_,_,_>) (flow: Flow<_,_,_>) =
        bidi.Join(flow)
        
    /// Completes bidi flow by joining it to a final flow.
    let inline ( *<=> ) (flow: Flow<_,_,_>) (bidi: BidiFlow<_,_,_,_,_>) =
        bidi.Join(flow)

[<RequireQualifiedAccess>]
module Graph =

    /// Creates a complete runnable graph given graph construction function.
    let runnable (builder: GraphDsl.Builder<'mat> -> unit) : RunnableGraph<'mat> =
        RunnableGraph.FromGraph(GraphDsl.CreateMaterialized(Func<_,_>(fun b ->
            builder b
            ClosedShape.Instance)))

    /// Creates a new partial graph using provided builder function
    let create (builder: GraphDsl.Builder<'mat> -> FlowShape<'t,'u>): IGraph<FlowShape<'t,'u>, 'mat> =
        GraphDsl.CreateMaterialized(Func<_,_>(builder))

    /// Executes provided graph using provided materializer.
    let run (mat: #IMaterializer) (graph: #IRunnableGraph<'mat>) =
        graph.Run mat