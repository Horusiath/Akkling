//-----------------------------------------------------------------------
// <copyright file="PersistentActor.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akka.Persistence
open Akkling
open Akka.Event
open Newtonsoft.Json.Linq

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
    abstract PersistEvent : 'Event seq * (unit->unit) -> unit

    /// <summary>
    /// Asynchronously persists sequence of events in the event journal. Use second argument
    /// to define function which will update state depending on events.
    /// </summary>
    abstract AsyncPersistEvent : 'Event seq * (unit->unit) -> unit

    /// <summary>
    /// Defers a second argument (update state callback) to be called after persisting target
    /// event will be confirmed.
    /// </summary>
    abstract DeferEvent : 'Event seq * (unit->unit) -> unit


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
    | AndThen of PersistentEffect<'Message> * (unit -> unit)
    interface Effect<'Message> with
        member __.WasHandled() = true
        member this.OnApplied(context, message) =
            let rec apply (ctx: ExtEventsourced<'Message>) effect (callback: (unit->unit)) =
                match effect with
                | Persist(event) -> ctx.PersistEvent([event], callback)
                | PersistAll(events) -> ctx.PersistEvent(events, callback)
                | PersistAsync(event) -> ctx.AsyncPersistEvent([event], callback)
                | PersistAllAsync(events) -> ctx.AsyncPersistEvent(events, callback)
                | Defer(events) -> ctx.DeferEvent(events, callback)
                | AndThen(inner, next) ->
                    let composed = if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then next else callback>>next
                    apply ctx inner composed
                    
            match context with
            | :? ExtEventsourced<'Message> as pctx -> apply pctx this Unchecked.defaultof<_>
            | _ -> raise (Exception("Cannot use persistent effects in context of non-persistent actor"))

and TypedPersistentContext<'Message, 'Actor when 'Actor :> FunPersistentActor<'Message>>(context : IActorContext, actor : 'Actor) as this =
    let self = context.Self
    let mutable hasPersisted = false
    let mutable hasDeffered = false
    let persisting callback = 
        Action<'Message>(fun e ->
            try
                hasPersisted <- true
                actor.Handle e
                callback ()
            finally
                hasPersisted <- false)
    let deferring callback = 
        Action<'Message>(fun e ->
            try
                hasPersisted <- true
                actor.Handle e
                callback ()
            finally
                hasPersisted <- false)
    member private this.Persisting = persisting id
    member private this.Deferring = deferring id
    interface ExtEventsourced<'Message> with
        member __.UntypedContext = context
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
        member __.WatchWith(aref: IActorRef, message: obj) = context.WatchWith(aref, message)
        member __.Unwatch(aref : IActorRef) = context.Unwatch aref
        member __.Log = lazy (Akka.Event.Logging.GetLogger(context))
        member __.Stash() = actor.Stash.Stash()
        member __.Unstash() = actor.Stash.Unstash()
        member __.UnstashAll() = actor.Stash.UnstashAll()
        member __.SetReceiveTimeout timeout = context.SetReceiveTimeout(Option.toNullable timeout)
        member __.Schedule (delay : TimeSpan) target message =
            context.System.Scheduler.ScheduleTellOnceCancelable(delay, untyped target, message, self)
        member __.ScheduleRepeatedly (delay : TimeSpan) (interval : TimeSpan) target message =
            context.System.Scheduler.ScheduleTellOnceCancelable(delay, untyped target, message, self)
        member __.Incarnation() = actor :> ActorBase
        member __.Stop(ref : IActorRef<'T>) = context.Stop(untyped ref)
        member __.Unhandled(msg) = actor.InternalUnhandled(msg)
        member __.Journal = actor.Journal
        member __.SnapshotStore = actor.SnapshotStore
        member __.IsRecovering () = actor.IsRecovering
        member __.LastSequenceNr () = actor.LastSequenceNr
        member __.Pid = actor.PersistenceId
        member this.PersistEvent(events, callback) =
            let cb = if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then this.Persisting else persisting callback
            actor.PersistAll(events, cb)
        member __.AsyncPersistEvent(events, callback) =
            let cb = if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then this.Persisting else persisting callback
            actor.PersistAllAsync(events, cb)
        member __.DeferEvent(events, callback) =
            let cb = if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then this.Deferring else deferring callback
            events |> Seq.iter (fun e -> actor.DeferAsync(e, cb))
        member __.Become(effect) = actor.Become(effect)

and PersistentLifecycleEvent =
    | ReplaySucceed
    | ReplayFailed of cause:exn * msg:obj
    | PersistFailed of cause:exn * evt:obj * sequenceNr:int64
    | PersistRejected of cause:exn * evt:obj * sequenceNr:int64
    interface IDeadLetterSuppression
    interface UnhandledSuppression

and FunPersistentActor<'Message>(actor : Eventsourced<'Message> -> Effect<'Message>) as this =
    inherit UntypedPersistentActor()
    let untypedContext = UntypedActor.Context
    let ctx = TypedPersistentContext<'Message, FunPersistentActor<'Message>>(untypedContext, this)
    let mutable behavior =
        match actor ctx with
        | :? Become<'Message> as effect -> effect.Effect
        | effect -> effect
    
    member __.Become (effect : Effect<'Message>) = behavior <- effect

    member __.Handle (msg: obj) =
        match msg with
        | Message msg -> behavior.OnApplied(ctx, msg)
        | :? UnhandledSuppression -> ()
        | msg -> base.Unhandled msg

    member __.Sender() : IActorRef = base.Sender
    member __.InternalUnhandled(message: obj) : unit = base.Unhandled message
    override this.PersistenceId = this.Self.Path.Name
    override this.OnCommand msg = this.Handle msg
    override this.OnRecover msg = this.Handle msg
    override this.OnReplaySuccess() = this.Handle ReplaySucceed
    override this.OnRecoveryFailure(e, msg) = this.Handle (ReplayFailed(e, msg))
    override this.OnPersistFailure(e, evt, sequenceNr) = this.Handle (PersistFailed(e, evt, sequenceNr))
    override this.OnPersistRejected(e, evt, sequenceNr) = this.Handle (PersistRejected(e, evt, sequenceNr))

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

module Effects =
    
    let inline andThen (callback: unit->unit) (effect: PersistentEffect<'event>) = AndThen(effect, callback)