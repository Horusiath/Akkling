//-----------------------------------------------------------------------
// <copyright file="PersistentActor.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akka.Persistence
open Akkling
open Akka.Event
open Newtonsoft.Json.Linq
open System.Threading.Tasks

type PID = string

[<Interface>]
type Eventsourced<'Message> =
    inherit Actor<'Message>

    /// <summary>
    /// Returns currently attached journal actor reference.
    /// </summary>
    abstract Journal: IActorRef

    /// <summary>
    /// Returns currently attached snapshot store actor reference.
    /// </summary>
    abstract SnapshotStore: IActorRef

    /// <summary>
    /// Returns value determining if current persistent actor is actually recovering.
    /// </summary>
    abstract IsRecovering: unit -> bool

    /// <summary>
    /// Returns last sequence number attached to latest persisted event.
    /// </summary>
    abstract LastSequenceNr: unit -> int64

    /// <summary>
    /// Persistent actor's identifier that doesn't change across different actor incarnations.
    /// </summary>
    abstract Pid: PID

    /// <summary>
    /// Flag which informs if current actor is actually during execution of persisting handler.
    /// </summary>
    abstract HasPersisted: unit -> bool

    /// <summary>
    /// Flag which informs if current actor is actually during execution of deferred handler.
    /// </summary>
    abstract HasDeferred: unit -> bool

and [<Interface>] PersistentContext<'Event> =

    /// <summary>
    /// Persists sequence of events in the event journal. Use second argument to define
    /// function which will update state depending on events.
    /// </summary>
    abstract PersistEvent: 'Event seq * (unit -> unit) -> unit

    /// <summary>
    /// Asynchronously persists sequence of events in the event journal. Use second argument
    /// to define function which will update state depending on events.
    /// </summary>
    abstract AsyncPersistEvent: 'Event seq * (unit -> unit) -> unit

    /// <summary>
    /// Defers a second argument (update state callback) to be called after persisting target
    /// event will be confirmed.
    /// </summary>
    abstract DeferEvent: 'Event seq * (unit -> unit) -> unit

    abstract LoadSnapshot: string * SnapshotSelectionCriteria * int64 -> unit

    abstract SaveSnapshot: obj -> unit

    abstract DeleteSnapshot: from: int64 -> unit

    abstract DeleteSnapshots: SnapshotSelectionCriteria -> unit

    abstract DeleteMessages: upto: int64 -> unit


and [<Interface>] ExtEventsourced<'Message> =
    inherit Eventsourced<'Message>
    inherit PersistentContext<'Message>
    inherit ExtActor<'Message>

and PersistentEffect<'Message> =
    /// Persist an event inside of event journal. This effect will result in calling
    /// current actor's handler function upon persistent event before handling the next
    /// incoming request.
    | Persist of 'Message
    /// Persist multiple events inside of event journal. This effect will result in calling
    /// current actor's handler function upon each persistent event before handling the next
    /// incoming request.
    | PersistAll of 'Message seq
    /// Persist an event inside of event journal. This effect will result in calling
    /// current actor's handler function upon persistent event, but unlike `Persist` it will
    /// not await for persistence process - if other request will arrive while event persist is
    /// in progress, they will may be handled before persisted event handler.
    | PersistAsync of 'Message
    /// Persist multiple events inside of event journal. This effect will result in calling
    /// current actor's handler function upon each persistent event, but unlike `PersistAll` it will
    /// not await for persistence process - if other request will arrive while event persist is
    /// in progress, they will may be handled before persisted event handler.
    | PersistAllAsync of 'Message seq
    | Defer of 'Message seq
    /// This effect will trigger current actor to load snapshot. It can be used ie. in response
    /// to a `LifecycleEvent PreStart`. If a matching snapshot existed it can be retrieved back
    /// using `SnapshotOffer(state)` active pattern in current actor's message handler.
    | LoadSnapshot of pid: string * SnapshotSelectionCriteria * upto: int64
    /// Triggers a snapshot save over specified state. This action always happens asynchronously:
    /// actor will not await for its completion and it's free to handle other incoming messages
    /// while snapshot persistence is in progress.
    ///
    /// Snapshot persistence will cause either `SaveSnapshotSuccess` or `SaveSnapshotFailure`
    /// events to trigger into a current actor's message handler.
    | SaveSnapshot of obj
    /// Request effect to delete snapshot identified by a specified sequence number.
    | DeleteSnapshot of int64
    /// Request effect to delete all snapshots matching specified criteria.
    | DeleteSnapshots of SnapshotSelectionCriteria
    /// Request effect to delete all events up to a specified sequence number.
    | DeleteMessages of int64
    | AndThen of PersistentEffect<'Message> * (unit -> unit)

    interface Effect<'Message> with
        member _.WasHandled() = true

        member this.OnApplied(context, message) =
            let rec apply (ctx: ExtEventsourced<'Message>) effect (callback: (unit -> unit)) =
                match effect with
                | Persist(event) -> ctx.PersistEvent([ event ], callback)
                | PersistAll(events) -> ctx.PersistEvent(events, callback)
                | PersistAsync(event) -> ctx.AsyncPersistEvent([ event ], callback)
                | PersistAllAsync(events) -> ctx.AsyncPersistEvent(events, callback)
                | Defer(events) -> ctx.DeferEvent(events, callback)
                | LoadSnapshot(pid, criteria, toSeqNr) -> ctx.LoadSnapshot(pid, criteria, toSeqNr)
                | SaveSnapshot(state) -> ctx.SaveSnapshot(state)
                | DeleteSnapshot(seqNr) -> ctx.DeleteSnapshot(seqNr)
                | DeleteSnapshots(criteria) -> ctx.DeleteSnapshots(criteria)
                | DeleteMessages(toSeqNr) -> ctx.DeleteMessages(toSeqNr)
                | AndThen(inner, next) ->
                    let composed =
                        if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then
                            next
                        else
                            next >> callback

                    apply ctx inner composed

            match context with
            | :? ExtEventsourced<'Message> as pctx -> apply pctx this Unchecked.defaultof<_>
            | _ -> raise (Exception("Cannot use persistent effects in context of non-persistent actor"))

and TypedPersistentContext<'Message, 'Actor when 'Actor :> FunPersistentActor<'Message>>
    (context: IActorContext, actor: 'Actor) as this =
    let self = context.Self
    let mutable hasPersisted = false
    let mutable hasDeferred = false

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
                hasDeferred <- true
                actor.Handle e
                callback ()
            finally
                hasDeferred <- false)

    member private this.Persisting = persisting id
    member private this.Deferring = deferring id

    interface ExtEventsourced<'Message> with
        member _.UntypedContext = context
        member _.HasPersisted() = hasPersisted
        member _.HasDeferred() = hasDeferred
        member _.Receive() = Input
        member _.Self = typed self

        member _.Sender<'Response>() =
            typed (context.Sender) :> IActorRef<'Response>

        member _.Parent<'Other>() =
            typed (context.Parent) :> IActorRef<'Other>

        member _.System = context.System
        member _.ActorOf(props, name) = context.ActorOf(props, name)
        member _.ActorSelection(path: string) = context.ActorSelection(path)
        member _.ActorSelection(path: ActorPath) = context.ActorSelection(path)
        member _.Watch(aref: IActorRef) = context.Watch aref
        member _.WatchWith(aref: IActorRef, message: obj) = context.WatchWith(aref, message)
        member _.Unwatch(aref: IActorRef) = context.Unwatch aref
        member _.Log = lazy (Akka.Event.Logging.GetLogger(context))
        member _.Stash() = actor.Stash.Stash()
        member _.Unstash() = actor.Stash.Unstash()
        member _.UnstashAll() = actor.Stash.UnstashAll()

        member _.SetReceiveTimeout timeout =
            context.SetReceiveTimeout(Option.toNullable timeout)

        member _.Schedule (delay: TimeSpan) target message =
            context.System.Scheduler.ScheduleTellOnceCancelable(delay, untyped target, message, self)

        member _.ScheduleRepeatedly (delay: TimeSpan) (interval: TimeSpan) target message =
            context.System.Scheduler.ScheduleTellRepeatedlyCancelable(delay, interval, untyped target, message, self)

        member _.Incarnation() = actor :> ActorBase
        member _.Stop(ref: IActorRef<'T>) = context.Stop(untyped ref)
        member _.Unhandled(msg) = actor.InternalUnhandled(msg)
        member _.Journal = actor.Journal
        member _.SnapshotStore = actor.SnapshotStore
        member _.IsRecovering() = actor.IsRecovering
        member _.LastSequenceNr() = actor.LastSequenceNr
        member _.Pid = actor.PersistenceId

        member this.PersistEvent(events, callback) =
            let cb =
                if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then
                    this.Persisting
                else
                    persisting callback

            actor.PersistAll(events, cb)

        member _.AsyncPersistEvent(events, callback) =
            let cb =
                if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then
                    this.Persisting
                else
                    persisting callback

            actor.PersistAllAsync(events, cb)

        member _.DeferEvent(events, callback) =
            let cb =
                if obj.ReferenceEquals(callback, Unchecked.defaultof<_>) then
                    this.Deferring
                else
                    deferring callback

            events |> Seq.iter (fun e -> actor.DeferAsync(e, cb))

        member _.Become(effect) = actor.Become(effect)
        member this.DeleteSnapshot(from) = actor.DeleteSnapshot(from)
        member this.DeleteSnapshots(criteria) = actor.DeleteSnapshots(criteria)
        member this.LoadSnapshot(pid, criteria, upto) = actor.LoadSnapshot(pid, criteria, upto)
        member this.DeleteMessages(upto) = actor.DeleteMessages(upto)
        member this.SaveSnapshot(state) = actor.SaveSnapshot(state)
        member _.RunTask(task: Func<Task>) : unit = actor.InternalRunTask(task)

and PersistentLifecycleEvent =
    | ReplaySucceed
    | ReplayFailed of cause: exn * msg: obj
    | PersistFailed of cause: exn * evt: obj * sequenceNr: int64
    | PersistRejected of cause: exn * evt: obj * sequenceNr: int64

    interface IDeadLetterSuppression
    interface UnhandledSuppression

and FunPersistentActor<'Message>(actor: Eventsourced<'Message> -> Effect<'Message>) as this =
    inherit UntypedPersistentActor()
    let untypedContext = UntypedActor.Context
    let persistentId = this.Self.Path.Name

    let ctx =
        TypedPersistentContext<'Message, FunPersistentActor<'Message>>(untypedContext, this)

    let mutable behavior =
        match actor ctx with
        | :? Become<'Message> as effect -> effect.Effect
        | effect -> effect

    member _.Become(effect: Effect<'Message>) = behavior <- effect

    member _.Handle(msg: obj) =
        match msg with
        | Message msg -> behavior.OnApplied(ctx, msg)
        | :? UnhandledSuppression -> ()
        | msg -> base.Unhandled msg

    member _.Sender() : IActorRef = base.Sender
    member _.InternalRunTask(task: Func<Task>) : unit = this.RunTask task
    member _.InternalUnhandled(message: obj) : unit = base.Unhandled message
    override this.PersistenceId = persistentId
    override this.OnCommand msg = this.Handle msg
    override this.OnRecover msg = this.Handle msg
    override this.OnReplaySuccess() = this.Handle ReplaySucceed
    override this.OnRecoveryFailure(e, msg) = this.Handle(ReplayFailed(e, msg))

    override this.OnPersistFailure(e, evt, sequenceNr) =
        this.Handle(PersistFailed(e, evt, sequenceNr))

    override this.OnPersistRejected(e, evt, sequenceNr) =
        this.Handle(PersistRejected(e, evt, sequenceNr))

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

    let inline andThen (callback: unit -> unit) (effect: PersistentEffect<'event>) = AndThen(effect, callback)
