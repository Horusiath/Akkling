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

module Graph =

    let (->>) (op: GraphDsl.ForwardOps<'out, 'mat>) (flow: Flow<_, 'out2, unit>) =
        op.Via(flow.MapMaterializedValue(Func<_,_>(fun () -> Akka.NotUsed.Instance)))

    let (->|) (op: GraphDsl.ForwardOps<'out, 'mat>) (sink: Sink<_, 'mat>) =
        op.To(sink)

    let (<<-) (flow: Flow<_, 'out2, unit>) (op: GraphDsl.ForwardOps<'out, 'mat>) =
        op.Via(flow.MapMaterializedValue(Func<_,_>(fun () -> Akka.NotUsed.Instance)))

    let (|<-) (sink: Sink<_, 'mat>) (op: GraphDsl.ForwardOps<'out, 'mat>) =
        op.To(sink)

    let run (mat: #IMaterializer) (graph: IRunnableGraph<'mat>) =
        graph.Run mat