//-----------------------------------------------------------------------
// <copyright file="Actors.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Actors

open System
open Akka.Actor
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation    

type IO<'T> = 
    | Input

/// <summary>
/// Exposes an Akka.NET actor APi accessible from inside of F# continuations -> <see cref="Cont{'In, 'Out}" />
/// </summary>
[<Interface>]
type Actor<'Message> = 
    inherit IActorRefFactory
    inherit ICanWatch
    
    /// <summary>
    /// Explicitly retrieves next incoming message from the mailbox.
    /// </summary>
    abstract Receive : unit -> IO<'Message>
    
    /// <summary>
    /// Gets <see cref="IActorRef" /> for the current actor.
    /// </summary>
    abstract Self : IActorRef<'Message>
        
    /// <summary>
    /// Returns a sender of current message or <see cref="ActorRefs.NoSender" />, if none could be determined.
    /// </summary>
    abstract Sender<'Response> : unit -> IActorRef<'Response>
        
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
    abstract ScheduleRepeatedly<'Scheduled>  : TimeSpan -> TimeSpan -> IActorRef<'Scheduled> -> 'Scheduled -> ICancelable

type TypedContext<'Message>(context: IActorContext, stashed: IWithUnboundedStash) as this =
    let self = context.Self
    interface Actor<'Message> with
        member __.Receive() = Input
        member __.Self = typed self
        member __.Sender<'Response>() = typed (context.Sender) :> IActorRef<'Response>
        member __.ActorOf(props, name) = context.ActorOf(props, name)
        member __.ActorSelection(path : string) = context.ActorSelection(path)
        member __.ActorSelection(path : ActorPath) = context.ActorSelection(path)
        member __.Watch(aref : IActorRef) = context.Watch aref
        member __.Unwatch(aref : IActorRef) = context.Unwatch aref
        member __.Log = lazy (Akka.Event.Logging.GetLogger(context))
        member __.Stash() = stashed.Stash.Stash()
        member __.Unstash() = stashed.Stash.Unstash()
        member __.UnstashAll() = stashed.Stash.UnstashAll() 
        member __.SetReceiveTimeout timeout = context.SetReceiveTimeout (Option.toNullable timeout)
        member __.Schedule (delay: TimeSpan) target message = context.System.Scheduler.ScheduleTellOnceCancelable(delay, target, message, self)
        member __.ScheduleRepeatedly (delay: TimeSpan) (interval: TimeSpan) target message = context.System.Scheduler.ScheduleTellOnceCancelable(delay, target, message, self)
        
[<AbstractClass>]
type Actor() = 
    inherit UntypedActor()
    interface IWithUnboundedStash with
        member val Stash = null with get, set
        
type LifecycleEvent =
    | PreStart
    | PostStop
    | PreRestart of cause:exn * message:obj
    | PostRestart of cause:exn

type Effect = interface end
type ActorEffect = 
    | Unhandled 
    | Stop
    | Ignore
    interface Effect

type Behavior<'Message> = 
    | Become of ('Message->Behavior<'Message>)
    | Return of Effect


type FunActor<'Message>(actor : Actor<'Message> -> Behavior<'Message>) as this = 
    inherit Actor()

    let untypedContext = UntypedActor.Context :> IActorContext
    let ctx = TypedContext<'Message>(untypedContext, this)
    let mutable behavior = actor ctx

    new(actor : Expr<Actor<'Message> -> Behavior<'Message>>) = FunActor(actor.Compile () ())

    member __.Next (current: Behavior<'Message>) (context: Actor<'Message>) (message: obj)  : Behavior<'Message> =
        match message with
        | :? 'Message as msg ->
            match current with
            | Become(fn) -> fn msg
            | _ -> current             
        | :? LifecycleEvent ->
            // we don't treat unhandled lifecycle events as casual unhandled messages
            current
        | other -> 
            this.Unhandled other
            current

    member __.Handle msg = 
        let nextBehavior = this.Next behavior ctx msg
        match nextBehavior with
        | Return effect ->
            match effect with
            | :? ActorEffect as actorEffect -> 
                match actorEffect with
                | Ignore -> ()
                | Unhandled -> this.Unhandled (msg :> obj)
                | Stop -> untypedContext.Stop(untypedContext.Self)
            | _ -> ()
        | _ -> behavior <- nextBehavior

    member __.Sender() : IActorRef = base.Sender

    override this.OnReceive msg = 
        this.Handle msg
    
    override this.PostStop() = 
        base.PostStop()
        this.Handle PostStop

    override this.PreStart() =
        base.PreStart()
        this.Handle PreStart
        
    override this.PreRestart(cause, msg) =
        base.PreRestart(cause, msg)
        this.Handle (PreRestart(cause, msg))

    override this.PostRestart(cause) =
        base.PostRestart cause
        this.Handle (PostRestart cause)
