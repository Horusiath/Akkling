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

[<RequireQualifiedAccess>]
module Graph =
    
    /// Creates a complete runnable graph given graph construction function.
    let runnable (builder: GraphDsl.Builder<'mat> -> unit) : RunnableGraph<'mat> =
        RunnableGraph.FromGraph(GraphDsl.CreateMaterialized(Func<_,_>(fun b ->
            builder b
            ClosedShape.Instance)))

    /// Creates a new partial graph using provided builder function
    let create (builder: GraphDsl.Builder<'mat> -> 'shape): IGraph<'shape, 'mat> =
        GraphDsl.CreateMaterialized(Func<_,_>(builder))

    let create1 (builder: GraphDsl.Builder<'mat> -> 'shape -> 'shapeOut) (shape:#IGraph<'shape,'mat>) : IGraph<'shapeOut, 'mat> =
        GraphDsl.Create(shape, Func<_,_,_>(builder))
        
    let create2 (combineFn: 'mat1 -> 'mat2 -> 'mat3) (builder: GraphDsl.Builder<'mat3> -> 'shape1 -> 'shape2 -> 'shapeOut) (shape1:#IGraph<'shape1,'mat1>) (shape2:#IGraph<'shape2,'mat2>) : IGraph<'shapeOut, 'mat3> =
        GraphDsl.Create(shape1, shape2, Func<_,_,_>(combineFn), Func<_,_,_,_>(builder))
        
    let create3 (combineFn: 'mat1 -> 'mat2 -> 'mat3 -> 'mat4) (builder: GraphDsl.Builder<'mat4> -> 'shape1 -> 'shape2 -> 'shape3 -> 'shapeOut) (shape1:#IGraph<'shape1,'mat1>) (shape2:#IGraph<'shape2,'mat2>) (shape3:#IGraph<'shape3,'mat3>) : IGraph<'shapeOut, 'mat4> =
        GraphDsl.Create(shape1, shape2, shape3, Func<_,_,_,_>(combineFn), Func<_,_,_,_,_>(builder))
        
    let create4 (combineFn: 'mat1 -> 'mat2 -> 'mat3 -> 'mat4 -> 'mat5) (builder: GraphDsl.Builder<'mat5> -> 'shape1 -> 'shape2 -> 'shape3 -> 'shape4 -> 'shapeOut) (shape1:#IGraph<'shape1,'mat1>) (shape2:#IGraph<'shape2,'mat2>) (shape3:#IGraph<'shape3,'mat3>) (shape4:#IGraph<'shape4,'mat4>) : IGraph<'shapeOut, 'mat5> =
        GraphDsl.Create(shape1, shape2, shape3, shape4, Func<_,_,_,_,_>(combineFn), Func<_,_,_,_,_,_>(builder))
        
    let create5 (combineFn: 'mat1 -> 'mat2 -> 'mat3 -> 'mat4 -> 'mat5 -> 'mat6) (builder: GraphDsl.Builder<'mat6> -> 'shape1 -> 'shape2 -> 'shape3 -> 'shape4 -> 'shape5 -> 'shapeOut) (shape1:#IGraph<'shape1,'mat1>) (shape2:#IGraph<'shape2,'mat2>) (shape3:#IGraph<'shape3,'mat3>) (shape4:#IGraph<'shape4,'mat4>) (shape5:#IGraph<'shape5,'mat5>) : IGraph<'shapeOut, 'mat6> =
        GraphDsl.Create(shape1, shape2, shape3, shape4, shape5, Func<_,_,_,_,_,_>(combineFn), Func<_,_,_,_,_,_,_>(builder))

    /// Executes provided graph using provided materializer.
    let run (mat: #IMaterializer) (graph: #IRunnableGraph<'mat>) =
        graph.Run mat