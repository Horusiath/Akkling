//-----------------------------------------------------------------------
// <copyright file="TestKit.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Streams.TestKit

open System
open Reactive.Streams
open Akkling.TestKit
open Akka.Streams.TestKit

[<AutoOpen>]
module Probes =

    let inline publisherProbe<'t> (initialPendingRequests: int64) (tck: Tck) =
        tck.CreatePublisherProbe<'t>(initialPendingRequests)

    let inline manualPublisherProbe<'t> (autoOnSubscribe: bool) (tck: Tck) =
        tck.CreateManualPublisherProbe<'t>(autoOnSubscribe)
        
    let inline subscriberProbe<'t> (tck: Tck) = tck.CreateSubscriberProbe<'t>()

    let inline manualSubscriberProbe<'t> (tck: Tck) = tck.CreateManualSubscriberProbe<'t>()

    let inline sinkProbe<'t> (tck: Tck) = tck.SinkProbe<'t>()

    let inline sourceProbe<'t> (tck: Tck) = tck.SourceProbe<'t>()

    let (|OnSubscribe|_|) (e: TestSubscriber.ISubscriberEvent) =
        match e with
        | :? TestSubscriber.OnSubscribe as s -> Some ()
        | _ -> None
        
    let (|OnComplete|_|) (e: TestSubscriber.ISubscriberEvent) =
        match e with
        | :? TestSubscriber.OnComplete as c -> Some ()
        | _ -> None
        
    let (|OnError|_|) (e: TestSubscriber.ISubscriberEvent) =
        match e with
        | :? TestSubscriber.OnError as e -> Some e.Cause
        | _ -> None
        
    let (|OnNext|_|) (e: TestSubscriber.ISubscriberEvent) =
        match e with
        | :? TestSubscriber.OnNext<'t> as n -> Some n.Element
        | _ -> None
        
    let (|Subscribe|_|) (e: TestPublisher.IPublisherEvent) =
        match e with
        | :? TestPublisher.Subscribe as s -> Some s.Subscription
        | _ -> None
        
    let (|CancelSubscription|_|) (e: TestPublisher.IPublisherEvent) =
        match e with
        | :? TestPublisher.CancelSubscription as s -> Some s.Subscription
        | _ -> None
        
    let (|RequestMore|_|) (e: TestPublisher.IPublisherEvent) =
        match e with
        | :? TestPublisher.RequestMore as s -> Some (s.NrOfElements, s.Subscription)
        | _ -> None
    
module SubscriberProbe =

    let inline ensureSubscription (probe: TestSubscriber.Probe<'t>) = probe.EnsureSubscription()

    let inline requestMore n (probe: TestSubscriber.Probe<'t>) = probe.Request(n)

    let inline cancelSubscription (probe: TestSubscriber.Probe<'t>) = probe.Cancel()
    
module ManualSubscriberProbe =

    let inline expectSubscription (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectSubscription()

    let inline onSubscribe (subscription: #ISubscription) (probe: TestSubscriber.ManualProbe<'t>) =
        probe.OnSubscribe(subscription)

    let inline onError (cause: #exn) (probe: TestSubscriber.ManualProbe<'t>) = probe.OnError(cause)

    let inline onComplete (probe: TestSubscriber.ManualProbe<'t>) = probe.OnComplete()

    let inline onNext (e: 't) (probe: TestSubscriber.ManualProbe<'t>) = probe.OnNext(e)

    let inline expectEvent (e: #TestSubscriber.ISubscriberEvent) (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectEvent e

    let inline expectNext (e: 't) (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectNext e

    let inline expectNextN (e: #seq<'t>) (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectNextN e
    
    let inline expectNextUnorderedN (e: #seq<'t>) (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectNextUnorderedN e

    let inline expectComplete (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectComplete()

    let inline expectError (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectError()

    let inline expectNoMsg (probe: TestSubscriber.ManualProbe<'t>) = probe.ExpectNoMsg()

    let inline matchNext (fn: 'o -> bool) (probe: TestSubscriber.ManualProbe<'t>) = probe.MatchNext(Predicate<_>(fn))
    
module PublisherProbe =

    let inline ensureSubscription (probe: TestSubscriber.Probe<'t>) = probe.EnsureSubscription()
    
module ManualPublisherProbe =

    let inline expectSubscription (probe: TestPublisher.ManualProbe<'t>) = probe.ExpectSubscription()

    let inline subscribe (subscriber: #ISubscriber<'t>) (probe: TestPublisher.ManualProbe<'t>) =
        probe.Subscribe(subscriber)

    let inline expectRequestMore n (subscription: #ISubscription) (probe: TestPublisher.ManualProbe<'t>) =
        probe.ExpectRequest(subscription, n)
    
    let inline expectNoMsg (probe: TestPublisher.ManualProbe<'t>) = probe.ExpectNoMsg()

    let inline expectEvent (probe: TestPublisher.ManualProbe<'t>) = probe.ExpectEvent()