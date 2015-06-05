namespace Akkling

open Akka.Actor
open System

[<AutoOpen>]
module Watchers = 
    /// <summary>
    /// Orders a <paramref name="watcher"/> to monitor an actor targeted by provided <paramref name="subject"/>.
    /// When an actor refered by subject dies, a watcher should receive a <see cref="Terminated"/> message.
    /// </summary>
    let monitor (subject : IActorRef) (watcher : ICanWatch) : IActorRef = watcher.Watch subject
    
    /// <summary>
    /// Orders a <paramref name="watcher"/> to stop monitoring an actor refered by provided <paramref name="subject"/>.
    /// </summary>
    let demonitor (subject : IActorRef) (watcher : ICanWatch) : IActorRef = watcher.Unwatch subject

[<AutoOpen>]
module EventStreaming = 
    /// <summary>
    /// Subscribes an actor reference to target channel of the provided event stream.
    /// </summary>
    let subscribe (ref : IActorRef<'Message>) (eventStream : Akka.Event.EventStream) : bool = 
        eventStream.Subscribe(ref, typeof<'Message>)
    
    /// <summary>
    /// Unubscribes an actor reference from target channel of the provided event stream.
    /// </summary>
    let unsubscribe (ref : IActorRef<'Message>) (eventStream : Akka.Event.EventStream) : bool = 
        eventStream.Unsubscribe(ref, typeof<'Message>)
    
    /// <summary>
    /// Publishes an event on the provided event stream. Event channel is resolved from event's type.
    /// </summary>
    let publish (event : 'Event) (eventStream : Akka.Event.EventStream) : unit = eventStream.Publish event
