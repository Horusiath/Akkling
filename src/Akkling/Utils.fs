//-----------------------------------------------------------------------
// <copyright file="Utils.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Actor
open System

[<AutoOpen>]
module Watchers = 
    /// <summary>
    /// Orders a <paramref name="watcher"/> to monitor an actor targeted by provided <paramref name="subject"/>.
    /// When an actor referred by subject dies, a watcher should receive a <see cref="Terminated"/> message.
    /// </summary>
    let inline monitor (watcher : #ICanWatch) (subject : IActorRef<'Message>) : IActorRef = watcher.Watch (untyped subject)
    
    /// <summary>
    /// Orders a <paramref name="watcher"/> to monitor an actor targeted by provided <paramref name="subject"/>.
    /// When an actor referred by subject dies, a watcher should receive a <paramref name="reply"/>.
    /// </summary>
    let inline monitorWith (reply: 'Reply) (watcher : #ICanWatch) (subject : IActorRef<'Message>) : IActorRef = watcher.WatchWith(untyped subject, box reply)
    
    /// <summary>
    /// Orders a <paramref name="watcher"/> to stop monitoring an actor referred by provided <paramref name="subject"/>.
    /// </summary>
    let inline demonitor (watcher : #ICanWatch) (subject : IActorRef<'Message>) : IActorRef = watcher.Unwatch (untyped subject)

[<AutoOpen>]
module EventStreaming = 
    /// <summary>
    /// Subscribes an actor reference to target channel of the provided event stream.
    /// </summary>
    let subscribe (ref : IActorRef<'Message>) (eventStream : Akka.Event.EventStream) : bool = 
        eventStream.Subscribe(untyped ref, typeof<'Message>)
    
    /// <summary>
    /// Unubscribes an actor reference from target channel of the provided event stream.
    /// </summary>
    let unsubscribe (ref : IActorRef<'Message>) (eventStream : Akka.Event.EventStream) : bool = 
        eventStream.Unsubscribe(untyped ref, typeof<'Message>)
    
    /// <summary>
    /// Publishes an event on the provided event stream. Event channel is resolved from event's type.
    /// </summary>
    let publish (event : 'Event) (eventStream : Akka.Event.EventStream) : unit = eventStream.Publish event
