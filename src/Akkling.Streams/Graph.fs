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

type Builder<'Mat> = GraphDsl.Builder<'Mat>
type Builder = Builder<unit>

module Graph =

    /// Uses provided builder function to create a runnable graph.
    let runnable (fn: GraphDsl.Builder<'mat> -> unit) = 
        RunnableGraph.FromGraph(GraphDsl.CreateMaterialized(System.Func<_,_>(fun b -> fn(b); ClosedShape.Instance)))

    /// Creates a partial (open) graph using provided builder function.
    let partial (fn: GraphDsl.Builder<'mat> -> #Shape) =
        GraphDsl.CreateMaterialized(System.Func<_,_>(fn))

    /// Run provided runnable graph using given materializer
    let run (mat: #IMaterializer) (graph: IRunnableGraph<'mat>) =
        graph.Run mat
