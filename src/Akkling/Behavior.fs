//-----------------------------------------------------------------------
// <copyright file="Behavior.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open System
open Akka.Actor

/// <summary>
/// Exposes an Akka.NET actor API accessible from inside of F# continuations
/// </summary>
[<Interface>] 
type Actor<'Message> = 
    inherit IActorRefFactory
    inherit ICanWatch
    
    /// <summary>
    /// Gets <see cref="IActorRef" /> for the current actor.
    /// </summary>
    abstract Self : IActorRef<'Message>
    
    /// <summary>
    /// Gets <see cref="ActorSystem" /> for the current actor.
    /// </summary>
    abstract System : ActorSystem
    
    /// <summary>
    /// Returns a sender of current message or <see cref="ActorRefs.NoSender" />, if none could be determined.
    /// </summary>
    abstract Sender<'Response> : unit -> IActorRef<'Response>
    
    /// <summary>
    /// Returns a parrent of current actor.
    /// </summary>
    abstract Parent<'Other> : unit -> IActorRef<'Other>

    /// <summary>
    /// Lazy logging adapter. It won't be initialized until logging function will be called. 
    /// </summary>
    abstract Log : Lazy<Akka.Event.ILoggingAdapter>
    
    /// <summary>
    /// Stashes the current message (the message that the actor received last)
    /// </summary>
    abstract Stash : unit -> unit
    
    /// <summary>
    /// Unstash the oldest message in the stash and prepends it to the actor's mailbox.
    /// The message is removed from the stash.
    /// </summary>
    abstract Unstash : unit -> unit
    
    /// <summary>
    /// Unstashes all messages by prepending them to the actor's mailbox.
    /// The stash is guaranteed to be empty afterwards.
    /// </summary>
    abstract UnstashAll : unit -> unit
    
    /// <summary>
    /// Sets or clears a timeout before <see="ReceiveTimeout"/> message will be send to an actor.
    /// </summary>
    abstract SetReceiveTimeout : TimeSpan option -> unit
    
    /// <summary>
    /// Schedules a message to be transmited in specified delay.
    /// </summary>
    abstract Schedule<'Scheduled> : TimeSpan -> IActorRef<'Scheduled> -> 'Scheduled -> ICancelable
    
    /// <summary>
    /// Schedules a message to be repeatedly transmited, starting at specified delay with provided intervals.
    /// </summary>
    abstract ScheduleRepeatedly<'Scheduled> : TimeSpan -> TimeSpan -> IActorRef<'Scheduled> -> 'Scheduled -> ICancelable

    /// <summary>
    /// A raw Actor context in it's untyped form.
    /// </summary>
    abstract UntypedContext : IActorContext 

    abstract Unhandled : obj -> unit
    abstract Stop : IActorRef<'T>  -> unit
            
[<AbstractClass>]
type Behavior<'Message, 'Context when 'Context :> Actor<'Message>>() =
    abstract member ReceiveMessage: 'Context -> 'Message -> AsyncVal<Behavior<'Message, 'Context>>
    abstract member ReceiveSignal: 'Context -> obj -> AsyncVal<Behavior<'Message, 'Context>>
    abstract member OnSignal: ('Context -> obj -> AsyncVal<Behavior<'Message, 'Context>>) -> Behavior<'Message, 'Context>
    abstract member OnApply: 'Context -> obj -> Behavior<'Message, 'Context> -> Behavior<'Message, 'Context> 

/// Mute behaviors are special-cased.
[<AbstractClass>]
type internal MuteBehavior<'Message, 'Context when 'Context :> Actor<'Message>>() =
    inherit Behavior<'Message, 'Context>()
    override __.ReceiveMessage _ _ = invalidOp "Muted behavior doesn't support message processing"
    override x.OnSignal _ =  invalidOp "Muted behavior doesn't support signal handler switching"
    
[<Sealed>]
type internal Same<'Message, 'Context when 'Context :> Actor<'Message>> private() = 
    inherit MuteBehavior<'Message, 'Context>()
    static member Instance: Behavior<'Message, 'Context> = upcast Same<'Message, 'Context>()
    override __.ReceiveSignal _ _ = invalidOp "Same behavior doesn't support signal processing"
    override __.OnApply _ _ prev = prev

[<Sealed>]
type internal Empty<'Message, 'Context when 'Context :> Actor<'Message>> private() = 
    inherit MuteBehavior<'Message, 'Context>()
    static member Instance: Behavior<'Message, 'Context> = upcast Empty<'Message, 'Context>()
    override this.ReceiveMessage ctx _ = 
        ctx.Unhandled ()
        Synchronous (upcast this)
    override this.ReceiveSignal _ _ = Synchronous (upcast this)
    override this.OnApply _ _ _ = upcast this

[<Sealed>]
type internal Ignored<'Message, 'Context when 'Context :> Actor<'Message>> private() = 
    inherit MuteBehavior<'Message, 'Context>()
    static member Instance: Behavior<'Message, 'Context> = upcast Ignored<'Message, 'Context>()
    override this.ReceiveMessage _ _ = Synchronous (upcast this)
    override this.ReceiveSignal _ _ = Synchronous (upcast this)
    override this.OnApply _ _ _ = upcast this

[<Sealed>]
type internal Unhandled<'Message, 'Context when 'Context :> Actor<'Message>> private() = 
    inherit MuteBehavior<'Message, 'Context>()
    static member Instance: Behavior<'Message, 'Context> = upcast Unhandled<'Message, 'Context>()
    override __.ReceiveSignal _ _ = invalidOp "Unhandled behavior doesn't support signal processing"
    override __.OnApply ctx msg prev = 
        ctx.Unhandled msg
        prev
    
[<Sealed>]
type internal Stopped<'Message, 'Context when 'Context :> Actor<'Message>> private() = 
    inherit MuteBehavior<'Message, 'Context>()
    static member Instance: Behavior<'Message, 'Context> = upcast Stopped<'Message, 'Context>()
    override __.ReceiveSignal _ _ = invalidOp "Stopped behavior doesn't support signal processing"
    override __.OnApply ctx _ prev = 
        ctx.Stop(ctx.Self)
        prev

[<Sealed>]
type internal Deferred<'Message, 'Context when 'Context :> Actor<'Message>>(fac: 'Context -> Behavior<'Message, 'Context>) =
    inherit MuteBehavior<'Message, 'Context>()
    member __.Factory = fac
    override __.ReceiveSignal _ _ = invalidOp "Deferred behavior doesn't support signal processing"
    override __.OnApply _ _ _ = invalidOp "Deferred behavior should never be applied as a result"

[<Sealed>]
type Immutable<'Message, 'Context when 'Context :> Actor<'Message>>(
    receive: 'Context -> 'Message -> AsyncVal<Behavior<'Message, 'Context>>,
    signal: 'Context -> obj -> AsyncVal<Behavior<'Message, 'Context>>) =
    inherit Behavior<'Message, 'Context>()
    
    override __.ReceiveMessage ctx msg = receive ctx msg
    override __.ReceiveSignal ctx sign = signal ctx sign
    override __.OnSignal newSignal = upcast Immutable(receive, newSignal)
    override this.OnApply _ _ _ = upcast this
    
/// Behavior module exposes combinator functions for building functional, typed actor behaviors.
[<RequireQualifiedAccess>]
module Behavior =
    
    /// Return this behavior to mark that previous behavior should be reused. No side effects.
    let inline same<'Message, 'Context when 'Context :> Actor<'Message>> : Behavior<'Message, 'Context> = Same.Instance

    /// Return this behavior to mark currently processed message as unhandled and then reuse previous behavior.
    let inline unhandled<'Message, 'Context when 'Context :> Actor<'Message>> : Behavior<'Message, 'Context> = Unhandled.Instance

    /// An empty behavior will simply unhandle all processed messages. It replaces previous behavior.
    let inline empty<'Message, 'Context when 'Context :> Actor<'Message>> : Behavior<'Message, 'Context> = Empty.Instance

    /// An ignore behavior will blackhole all incomming messages siliently (without unhandling). It replaces previous behavior.
    let inline ignore<'Message, 'Context when 'Context :> Actor<'Message>> : Behavior<'Message, 'Context> = Ignored.Instance

    /// Stop behavior will order current actor to stop itself.
    let inline stop<'Message, 'Context when 'Context :> Actor<'Message>> : Behavior<'Message, 'Context> = Stopped.Instance

    /// Deferred behavior doesn't wait for a message to come. It gets executed immediatelly upon actor creation and can be used for initializing actor behaviors.
    let inline deferred<'Message, 'Context when 'Context :> Actor<'Message>>(fac) : Behavior<'Message, 'Context> = upcast Deferred(fac)

    /// A classic behavior used for handling incoming messages using synchronous code execution.
    let immutable<'Message, 'Context when 'Context :> Actor<'Message>>(receive: 'Context -> 'Message -> Behavior<'Message, 'Context>) : Behavior<'Message, 'Context> =
        let onMessage = fun ctx msg -> Synchronous (receive ctx msg)
        let onReceive = fun _ _ -> Synchronous ignore
        upcast Immutable(onMessage, onReceive)
        
    /// A classic behavior used for handling incoming messages using asynchronous code execution.
    let immutableAsync<'Message, 'Context when 'Context :> Actor<'Message>>(receive: 'Context -> 'Message -> Async<Behavior<'Message, 'Context>>) : Behavior<'Message, 'Context> =
        let onMessage = fun ctx msg -> Asynchronous (receive ctx msg)
        let onReceive = fun _ _ -> Synchronous ignore
        upcast Immutable(onMessage, onReceive)
        
    /// Overrides signal handlers for user-defined behaviors. It wil throw InvalidOperationException if used on system behaviors.
    let inline onSignal (newSignal: 'Context -> obj -> Behavior<'Message, 'Context>) (behavior: Behavior<'Message, 'Context>) =
        let onSig = fun ctx s -> Synchronous (newSignal ctx s)
        behavior.OnSignal onSig
    
    /// Overrides signal handlers for user-defined behaviors. It wil throw InvalidOperationException if used on system behaviors.
    let inline onSignalAsync (newSignal: 'Context -> obj -> Async<Behavior<'Message, 'Context>>) (behavior: Behavior<'Message, 'Context>) =
        let onSig = fun ctx s -> Asynchronous (newSignal ctx s)
        behavior.OnSignal onSig