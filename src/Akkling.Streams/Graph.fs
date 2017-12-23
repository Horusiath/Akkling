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
    
    let runnable (graph: #IGraph<ClosedShape, 'mat>): RunnableGraph<'mat> =
        RunnableGraph.FromGraph(graph)

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

    /// Transform only the materialized value of this RunnableGraph, leaving all other properties as they were.
    let inline mapMaterializedValue (fn: 'mat -> 'mat2) (graph: #IRunnableGraph<'mat>) : IRunnableGraph<'mat2> =
        graph.MapMaterializedValue(Func<_,_>(fn))
        
    /// Change the attributes of this <see cref="T:Akka.Streams.IGraph`1" /> to the given ones and seal the list 
    /// of attributes. This means that further calls will not be able to remove these attributes, but instead add new ones. 
    /// Note that this operation has no effect on an empty Flow (because the attributes apply only to the contained 
    /// processing stages).
    let inline withAttributes (attributes: Attributes) (graph: #IRunnableGraph<'mat>) : IRunnableGraph<'mat> =
        graph.WithAttributes(attributes)

    /// Add the given attributes to this <see cref="T:Akka.Streams.IGraph`1" />.
    /// Further calls to <see cref="M:Akka.Streams.Dsl.IRunnableGraph`1.WithAttributes(Akka.Streams.Attributes)" />
    /// will not remove these attributes. Note that this operation has no effect on an empty Flow (because the attributes apply
    /// only to the contained processing stages).
    let inline addAttributes(attributes: Attributes) (graph: #IRunnableGraph<'mat>) : IRunnableGraph<'mat> =
        graph.AddAttributes(attributes)

    /// Add a name attribute to this Graph.
    let inline named (graph: #IRunnableGraph<'mat>) (name: string) : IRunnableGraph<'mat> =
        graph.Named(name)  

module Operators = 
    
    open Akka.Streams

    type FOps<'o,'mat> = GraphDsl.ForwardOps<'o,'mat>
    type ROps<'i,'mat> = GraphDsl.ReverseOps<'i,'mat>

    type GraphDsl.Builder<'mat> with
        member b.From(source: Source<'i,'mat>) = b.From(source :> IGraph<SourceShape<'i>,'mat>)
        member b.To(sink: Sink<'o,'mat2>) = b.To(sink :> IGraph<SinkShape<'o>,'mat2>)

    [<Struct>]
    type ForwardFunctor = ForwardFunctor with
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: FlowShape<'i,'o>) = l.Via(r)
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: IGraph<FlowShape<'i,'o>, 'mat>) = l.Via(r)
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: UniformFanInShape<'i,'o2> ) = l.Via(r)
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: UniformFanOutShape<'i,'o2> ) = l.Via(r)
                                       
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: IGraph<SinkShape<'i>,'mat>) = l.To(r)
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: SinkShape<'i>) = l.To(r)
        static member inline (?<-) (l: FOps<'o,'mat>, ForwardFunctor, r: Inlet<'i>) = l.To(r)

    [<Struct>]
    type ReverseFunctor = ReverseFunctor with
        static member inline (?<-) (l: ROps<'i,'mat>, ReverseFunctor, r: FlowShape<'i,'o>) = l.Via(r)
        static member inline (?<-) (l: ROps<'i,'mat>, ReverseFunctor, r: IGraph<FlowShape<'i,'o>, 'mat>) = l.Via(r)
        static member inline (?<-) (l: ROps<'i,'mat>, ReverseFunctor, r: UniformFanInShape<'i2,'o> ) = l.Via(r)
        static member inline (?<-) (l: ROps<'i,'mat>, ReverseFunctor, r: UniformFanOutShape<'i2,'o> ) = l.Via(r)
        
        //static member inline (?<-) (l: GraphDsl.ReverseOps<'i,'mat>, ReverseFunctor, r: IGraph<SourceShape<'o>,'mat>) = l.From(r)
        //static member inline (?<-) (l: GraphDsl.ReverseOps<'i,'mat>, ReverseFunctor, r: SourceShape<'o>) = l.From(r)
        //static member inline (?<-) (l: GraphDsl.ReverseOps<'i,'mat>, ReverseFunctor, r: Outlet<'o>) = l.From(r)
                
    let inline (=>) (ops: FOps<'o,'mat>) (right) = (ops ? (ForwardFunctor) <- right)
    let inline (<=) (ops: ROps<'i,'mat>) (right) = (ops ? (ReverseFunctor) <- right)