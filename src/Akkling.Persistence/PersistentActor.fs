//-----------------------------------------------------------------------
// <copyright file="PersistentActor.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akka.Persistence
open Akkling
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

type PID = string

[<Interface>]
type Eventsourced<'Message> = 
    inherit Actor<'Message>
    
    /// <summary>
    /// Returns currently attached journal actor reference.
    /// </summary>
    abstract Journal : IActorRef
    
    /// <summary>
    /// Returns currently attached snapshot store actor reference.
    /// </summary>
    abstract SnapshotStore : IActorRef
    
    /// <summary>
    /// Returns value determining if current persistent actor is actually recovering.
    /// </summary>
    abstract IsRecovering : unit -> bool
    
    /// <summary>
    /// Returns last sequence number attached to latest persisted event.
    /// </summary>
    abstract LastSequenceNr : unit -> int64
    
    /// <summary>
    /// Persistent actor's identifier that doesn't change across different actor incarnations.
    /// </summary>
    abstract Pid : PID
    
    /// <summary>
    /// Flag which informs if current actor is acutally during execution of persisting handler.
    /// </summary>
    abstract HasPersisted: unit -> bool

    /// <summary>
    /// Flag which informs if current actor is acutally during execution of deffered handler.
    /// </summary>
    abstract HasDeffered : unit -> bool

and [<Interface>]PersistentContext<'Event> =
    
    /// <summary>
    /// Persists sequence of events in the event journal. Use second argument to define 
    /// function which will update state depending on events.
    /// </summary>
    abstract PersistEvent : 'Event seq -> unit
    
    /// <summary>
    /// Asynchronously persists sequence of events in the event journal. Use second argument 
    /// to define function which will update state depending on events.
    /// </summary>
    abstract AsyncPersistEvent : 'Event seq -> unit
    
    /// <summary>
    /// Defers a second argument (update state callback) to be called after persisting target
    /// event will be confirmed.
    /// </summary>
    abstract DeferEvent : 'Event seq -> unit


and [<Interface>]ExtEventsourced<'Message> =
    inherit Eventsourced<'Message>
    inherit PersistentContext<'Message>
    inherit ExtActor<'Message>

and PersistentEffect<'Message> =
    | Persist of 'Message
    | PersistAll of 'Message seq
    | PersistAsync of 'Message
    | PersistAllAsync of 'Message seq
    | Defer of 'Message seq
    interface Effect with
        member this.OnApplied(context, message) = 
            match context with
            | :? ExtEventsourced<'Message> as persistentContext ->
                match this with
                | Persist(event) -> persistentContext.PersistEvent [event]
                | PersistAll(events) -> persistentContext.PersistEvent events
                | PersistAsync(event) -> persistentContext.AsyncPersistEvent [event]
                | PersistAllAsync(events) -> persistentContext.AsyncPersistEvent events
                | Defer(events) -> persistentContext.DeferEvent events
            | _ -> raise (Exception("Cannot use persistent effects in context of non-persistent actor"))
            
and TypedPersistentContext<'Message, 'Actor when 'Actor :> FunPersistentActor<'Message>>(context : IActorContext, actor : 'Actor) as this = 
    let self = context.Self
    let mutable hasPersisted = false
    let mutable hasDeffered = false
    member private this.Persisting =
        Action<'Message>( fun e ->
            hasPersisted <- true
            actor.Handle e
            hasPersisted <- false)
    member private this.Deffering =
        Action<'Message>( fun e ->
            hasDeffered <- true
            actor.Handle e
            hasDeffered <- false)
    interface ExtEventsourced<'Message> with
        member __.HasPersisted () = hasPersisted
        member __.HasDeffered () = hasDeffered
        member __.Receive() = Input
        member __.Self = typed self
        member __.Sender<'Response>() = typed (context.Sender) :> IActorRef<'Response>
        member __.Parent<'Other>() = typed (context.Parent) :> IActorRef<'Other>
        member __.System = context.System
        member __.ActorOf(props, name) = context.ActorOf(props, name)
        member __.ActorSelection(path : string) = context.ActorSelection(path)
        member __.ActorSelection(path : ActorPath) = context.ActorSelection(path)
        member __.Watch(aref : IActorRef) = context.Watch aref
        member __.Unwatch(aref : IActorRef) = context.Unwatch aref
        member __.Log = lazy (Akka.Event.Logging.GetLogger(context))
        member __.Stash() = actor.Stash.Stash()
        member __.Unstash() = actor.Stash.Unstash()
        member __.UnstashAll() = actor.Stash.UnstashAll()
        member __.SetReceiveTimeout timeout = context.SetReceiveTimeout(Option.toNullable timeout)
        member __.Schedule (delay : TimeSpan) target message = 
            context.System.Scheduler.ScheduleTellOnceCancelable(delay, target, message, self)
        member __.ScheduleRepeatedly (delay : TimeSpan) (interval : TimeSpan) target message = 
            context.System.Scheduler.ScheduleTellOnceCancelable(delay, target, message, self)
        member __.Incarnation() = actor :> ActorBase
        member __.Stop(ref : IActorRef<'T>) = context.Stop(untyped ref)
        member __.Unhandled(msg) = 
            match box actor with
            | :? FunActor<'Message> as act -> act.InternalUnhandled(msg)
            | _ -> raise (Exception("Couldn't use actor in typed context"))
        member __.Journal = actor.Journal
        member __.SnapshotStore = actor.SnapshotStore
        member __.IsRecovering () = actor.IsRecovering
        member __.LastSequenceNr () = actor.LastSequenceNr
        member __.Pid = actor.PersistenceId
        member this.PersistEvent(events) = 
            actor.Persist(events, this.Persisting)
        member __.AsyncPersistEvent(events) = 
            actor.PersistAsync(events, this.Persisting)
        member __.DeferEvent(events)  = 
            actor.Defer(events, this.Deffering)

and PersistentLifecycleEvent =
    | ReplaySucceed
    | ReplayFailed
    
and FunPersistentActor<'Message>(actor : Eventsourced<'Message> -> Behavior<'Message>, pid: PID) as this = 
    inherit UntypedPersistentActor()
    let untypedContext = UntypedActor.Context
    let ctx = TypedPersistentContext<'Message, FunPersistentActor<'Message>>(untypedContext, this)
    let mutable behavior = actor ctx
    new(actor : Expr<Eventsourced<'Message> -> Behavior<'Message>>, pid: PID) = FunPersistentActor(actor.Compile () (), pid)
    
    member __.Next (current : Behavior<'Message>) (context : Actor<'Message>) (message : obj) : Behavior<'Message> = 
        match message with
        | :? 'Message as msg -> 
            match current with
            | Become(fn) -> fn msg
            | _ -> current
        | :? LifecycleEvent | :? PersistentLifecycleEvent -> 
            // we don't treat unhandled lifecycle events as casual unhandled messages
            current
        | other -> 
            base.Unhandled other
            current
    
    member __.Handle (msg: obj) : unit = 
        let nextBehavior = this.Next behavior ctx msg
        match nextBehavior with
        | Return effect -> effect.OnApplied(ctx, msg :?> 'Message)
        | _ -> behavior <- nextBehavior
    
    member __.Sender() : IActorRef = base.Sender
    member __.InternalUnhandled(message: obj) : unit = base.Unhandled message
    override this.PersistenceId = pid
    override this.OnCommand msg = this.Handle msg
    override this.OnRecover msg = this.Handle msg
    
    override this.PostStop() = 
        base.PostStop()
        this.Handle PostStop
    
    override this.PreStart() = 
        base.PreStart()
        this.Handle PreStart
    
    override this.PreRestart(cause, msg) = 
        base.PreRestart(cause, msg)
        this.Handle(PreRestart(cause, msg))
    
    override this.PostRestart(cause) = 
        base.PostRestart cause
        this.Handle(PostRestart cause)