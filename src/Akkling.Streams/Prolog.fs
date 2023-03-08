//-----------------------------------------------------------------------
// <copyright file="Prolog.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open System
open Akka.Streams

[<AutoOpen>]
module Prolog =
    open Akka.Streams.Dsl

    type Akka.Streams.ISinkQueue<'t> with
        member x.AsyncPull() : Async<'t option> = 
            async {
                let! res = x.PullAsync() |> Async.AwaitTask
                return if res.HasValue then Some res.Value else None
            }
    
    let inline toCsOption (o: 'v option) : Akka.Util.Option<'v> =
        match o with
        | Some v -> Akka.Util.Option.Create(v)
        | None -> Akka.Util.Option<'v>.None
            
    let inline ofCsOption (o: Akka.Util.Option<'v>) = if o.HasValue then Some o.Value else None
            
    type Akka.Streams.ISourceQueue<'t> with
        member x.AsyncOffer(elem: 't) : Async<Akka.Streams.IQueueOfferResult> = x.OfferAsync(elem) |> Async.AwaitTask

    type Akka.Actor.ActorSystem with
        member x.Materializer(?settings: ActorMaterializerSettings) = ActorMaterializer.Create(x, Option.toObj settings)

    type Akka.Streams.Dsl.Tcp.ServerBinding with
        member x.AsyncUnbind() = x.Unbind() |> Async.AwaitTask

    type Akka.Streams.Dsl.ZipWith with
        static member create (fn: 'i0 -> 'i1 -> 'o) = ZipWith<'i0,'i1,'o>(Func<_,_,_>(fn))
        static member create (fn: 'i0 -> 'i1 -> 'i2 -> 'o) = ZipWith<'i0,'i1,'i2,'o>(Func<_,_,_,_>(fn))
        static member create (fn: 'i0 -> 'i1 -> 'i2 -> 'i3 -> 'o) = ZipWith<'i0,'i1,'i2,'i3,'o>(Func<_,_,_,_,_>(fn))
        static member create (fn: 'i0 -> 'i1 -> 'i2 -> 'i3 -> 'i4 -> 'o) = ZipWith<'i0,'i1,'i2,'i3,'i4,'o>(Func<_,_,_,_,_,_>(fn))
        static member create (fn: 'i0 -> 'i1 -> 'i2 -> 'i3 -> 'i4 -> 'i5 -> 'o) = ZipWith<'i0,'i1,'i2,'i3,'i4,'i5,'o>(Func<_,_,_,_,_,_,_>(fn))
        static member create (fn: 'i0 -> 'i1 -> 'i2 -> 'i3 -> 'i4 -> 'i5 -> 'i6 -> 'o) = ZipWith<'i0,'i1,'i2,'i3,'i4,'i5,'i6,'o>(Func<_,_,_,_,_,_,_,_>(fn))
        static member create (fn: 'i0 -> 'i1 -> 'i2 -> 'i3 -> 'i4 -> 'i5 -> 'i6 -> 'i7 -> 'o) = ZipWith<'i0,'i1,'i2,'i3,'i4,'i5,'i6,'i7,'o>(Func<_,_,_,_,_,_,_,_,_>(fn))

    type Akka.Streams.Dsl.UnzipWith with        
        static member create (fn: 'i -> ValueTuple<'o0, 'o1>) = UnzipWith<'i,'o0,'o1>(Func<_,_>(fn))
        static member create (fn: 'i -> ValueTuple<'o0, 'o1, 'o2>) = UnzipWith<'i,'o0,'o1,'o2>(Func<_,_>(fn))
        static member create (fn: 'i -> ValueTuple<'o0, 'o1, 'o2, 'o3>) = UnzipWith<'i,'o0,'o1,'o2,'o3>(Func<_,_>(fn))
        static member create (fn: 'i -> ValueTuple<'o0, 'o1, 'o2, 'o3, 'o4>) = UnzipWith<'i,'o0,'o1,'o2,'o3,'o4>(Func<_,_>(fn))
        static member create (fn: 'i -> ValueTuple<'o0, 'o1, 'o2, 'o3, 'o4, 'o5>) = UnzipWith<'i,'o0,'o1,'o2,'o3,'o4,'o5>(Func<_,_>(fn))
        static member create (fn: 'i -> ValueTuple<'o0, 'o1, 'o2, 'o3, 'o4, 'o5, 'o6>) = UnzipWith<'i,'o0,'o1,'o2,'o3,'o4,'o5,'o6>(Func<_,_>(fn))


[<RequireQualifiedAccess>]
module Keep =    
    let left l r = l
    let right l r = r
    let both l r = (l, r)
    let none l r = ()