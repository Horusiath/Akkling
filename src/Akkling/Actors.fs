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

type Behavior<'Message, 'Out> = 
    | Become of ('Message->Behavior<'Message, 'Out>)
    | Unhandled of Behavior<'Message, 'Out>
    | Return of 'Out


type FunActor<'Message, 'Out>(actor : Actor<'Message> -> Behavior<'Message, 'Out>) as this = 
    inherit Actor()

    let untypedContext = UntypedActor.Context :> IActorContext
    let ctx = TypedContext<'Message>(untypedContext, this)
    let mutable behavior = actor ctx
        
    let next (current: Behavior<'Message, 'Out>) (context: Actor<'Message>) (message: obj)  : Behavior<'Message, 'Out> =
        match message with
        | :? 'Message as msg ->
            match current with
            | Become(fn) -> fn msg
            | Unhandled wrapped ->
                this.Unhandled message
                wrapped
            | Return _ ->
                failwith <| sprintf "Actor %A has been stopped" context.Self
        | other -> 
            this.Unhandled other
            current

    let handle msg = 
        behavior <- next behavior ctx msg
        match behavior with
        | Return _ -> untypedContext.Stop(untypedContext.Self)
        | _ -> ()

    new(actor : Expr<Actor<'Message> -> Behavior<'Message, 'Out>>) = FunActor(actor.Compile () ())
    member __.Sender() : IActorRef = base.Sender
    member __.Unhandled msg = base.Unhandled msg

    override this.OnReceive msg = 
        handle msg
    
    override x.PostStop() = 
        base.PostStop()
        handle PostStop

    override x.PreStart() =
        base.PreStart()
        handle PreStart
        
    override x.PreRestart(cause, msg) =
        base.PreRestart(cause, msg)
        handle (PreRestart(cause, msg))

    override x.PostRestart(cause) =
        base.PostRestart cause
        handle (PostRestart cause)
