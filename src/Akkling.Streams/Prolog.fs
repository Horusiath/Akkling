//-----------------------------------------------------------------------
// <copyright file="Prolog.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams

open Akka.Streams

[<AutoOpen>]
module Prolog =

    let internal toCsOption =
        function
        | Some v -> Akka.Streams.Util.Option v
        | None -> Akka.Streams.Util.Option.None

    let inline internal ofCsOption (res: Akka.Streams.Util.Option<_>) = if res.HasValue then Some res.Value else None

    type Akka.Streams.ISinkQueue<'t> with
        member x.AsyncPull() : Async<'t option> = 
            async {
                let! res = x.PullAsync() |> Async.AwaitTask
                return ofCsOption res
            }
            
    type Akka.Streams.ISourceQueue<'t> with
        member x.AsyncOffer(elem: 't) : Async<Akka.Streams.IQueueOfferResult> = x.OfferAsync(elem) |> Async.AwaitTask

    type Akka.Actor.ActorSystem with
        member x.Materializer(?settings: ActorMaterializerSettings) = ActorMaterializer.Create(x, Option.toObj settings)

[<RequireQualifiedAccess>]
module Keep =    
    let left l r = l
    let right l r = r
    let both l r = (l, r)
    let none l r = ()