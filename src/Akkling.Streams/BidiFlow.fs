//-----------------------------------------------------------------------
// <copyright file="BidiFlow.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open Akkling
open Akka.Streams
open Akka.Streams.Dsl

[<RequireQualifiedAccess>]
module BidiFlow =
    
    /// A graph with the shape of a flow logically is a flow, this method makes it so also in type.
    let inline ofGraph (graph: #IGraph<BidiShape<_,_,_,_>,_>) = BidiFlow.FromGraph(graph)

    /// Wraps two flows into a single BidiFlow.
    let inline ofFlowsMat (flow1: #IGraph<FlowShape<_,_>,'mat>) (matFn: 'mat -> 'mat2 -> 'mat3) (flow2: #IGraph<FlowShape<_,_>, 'mat2>) =
        BidiFlow.FromFlowsMat(flow1, flow2, Func<_,_,_>(matFn))
        
    /// Wraps two flows into a single BidiFlow, ignoring inherited materialized values.
    let inline ofFlows (flow1: #IGraph<FlowShape<_,_>, _>) (flow2: #IGraph<FlowShape<_,_>, _>) =
        BidiFlow.FromFlowsMat(flow1, flow2, Func<_,_,_>(fun _ _ -> ()))

    /// Create a BidiFlow where the top and bottom flows are just one simple mapping
    /// stage each, expressed by the two functions.
    let inline ofFun (inbound: 'in2 -> 'out2) (outbound: 'in1 -> 'out1) = 
        BidiFlow.FromFunction(Func<_,_>(outbound), Func<_,_>(inbound))

    /// If the time between two processed elements (in any direction) exceed the 
    /// provided timeout, the stream is failed with a TimeoutException.
    let inline idle (timeout: TimeSpan) = BidiFlow.BidirectionalIdleTimeout(timeout)

    /// Replaces existing set of attributes attached to provided BidiFlow with new one.
    let inline attrs (a: Attributes) (bidi: BidiFlow<_,_,_,_,_>) =
        bidi.WithAttributes(a)

    /// Changes the name of a target BidiFlow stage.
    let inline named (name) (bidi: BidiFlow<_,_,_,_,_>) = bidi.Named(name)

    /// Marks currend BidiFlow stage as asynchronous, setting an async boundary in that place.
    let inline async (bidi: BidiFlow<_,_,_,_,_>) = bidi.Async()

    /// Reverses the direction of current BidiFlow.
    let inline rev (bidi: BidiFlow<_,_,_,_,_>) = bidi.Reversed()

    /// Add the first BidiFlow as the next step of a second BidiFlow.
    let inline atop (bidi1: BidiFlow<_,_,_,_,_>) (bidi2: BidiFlow<_,_,_,_,_>) = bidi2.Atop(bidi1)

    /// Add the fist BidiFlow as the next step of a second BidiFlow.
    let inline atopMat (bidi1: BidiFlow<_,_,_,_,'mat2>) (matFn: 'mat -> 'mat2 -> 'mat3) (bidi2: BidiFlow<_,_,_,_,'mat>) = 
        bidi2.AtopMat(bidi1, Func<_,_,_>(matFn))

    /// Add provided flow as a final step of a bidirectional flow transformation.
    let inline join (flow: Flow<_,_,_>) (bidi: BidiFlow<_,_,_,_,_>) = bidi.Join(flow)
    
    /// Add provided flow as a final step of a bidirectional flow transformation.
    let inline joinMat (flow: Flow<_,_,'mat2>) (matFn: 'mat -> 'mat2 -> 'mat3)  (bidi: BidiFlow<_,_,_,_,'mat>) = 
        bidi.JoinMat(flow, Func<_,_,_>(matFn))