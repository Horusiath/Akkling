﻿//-----------------------------------------------------------------------
// <copyright file="Actors.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------
[<AutoOpen>]
module Akkling.Actors

open System
open Akka.Actor
open Newtonsoft.Json.Linq
open System.Threading.Tasks

type IO<'T> = | Input

let (|Message|_|) msg =
    match box msg with
    | :? 'Message as msg -> Some msg
    | :? JObject as jobj -> Some <| jobj.ToObject<'Message>()
    | _ -> None

/// <summary>
/// Exposes an Akka.NET actor API accessible from inside of F# continuations
/// </summary>
[<Interface>]
type Actor<'Message> =
    inherit IActorRefFactory
    inherit ICanWatch

    /// <summary>
    /// Explicitly retrieves next incoming message from the mailbox.
    /// </summary>
    abstract Receive: unit -> IO<'Message>

    /// <summary>
    /// Gets <see cref="IActorRef" /> for the current actor.
    /// </summary>
    abstract Self: IActorRef<'Message>

    /// <summary>
    /// Gets <see cref="ActorSystem" /> for the current actor.
    /// </summary>
    abstract System: ActorSystem

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
    abstract Log: Lazy<Akka.Event.ILoggingAdapter>

    /// <summary>
    /// Stashes the current message (the message that the actor received last)
    /// </summary>
    abstract Stash: unit -> unit

    /// <summary>
    /// Unstash the oldest message in the stash and prepends it to the actor's mailbox.
    /// The message is removed from the stash.
    /// </summary>
    abstract Unstash: unit -> unit

    /// <summary>
    /// Unstashes all messages by prepending them to the actor's mailbox.
    /// The stash is guaranteed to be empty afterwards.
    /// </summary>
    abstract UnstashAll: unit -> unit

    /// <summary>
    /// Sets or clears a timeout before <see cref="ReceiveTimeout"/> message will be send to an actor.
    /// </summary>
    abstract SetReceiveTimeout: TimeSpan option -> unit

    /// <summary>
    /// Schedules a message to be transmitted in specified delay.
    /// </summary>
    abstract Schedule<'Scheduled> : TimeSpan -> IActorRef<'Scheduled> -> 'Scheduled -> ICancelable

    /// <summary>
    /// Schedules a message to be repeatedly transmitted, starting at specified delay with provided intervals.
    /// </summary>
    abstract ScheduleRepeatedly<'Scheduled> : TimeSpan -> TimeSpan -> IActorRef<'Scheduled> -> 'Scheduled -> ICancelable

    /// <summary>
    /// A raw Actor context in it's untyped form.
    /// </summary>
    abstract UntypedContext: IActorContext

[<Interface>]
type ExtContext =
    /// <summary>
    /// Returns current actor incarnation
    /// </summary>
    abstract Incarnation: unit -> ActorBase

    /// <summary>
    /// Stops execution of provided actor.
    /// </summary>
    abstract Stop: IActorRef<'T> -> unit

    /// <summary>
    /// Marks message as unhandled.
    /// </summary>
    abstract Unhandled: obj -> unit

/// <summary>
/// Exposes an Akka.NET extended actor API accessible from inside of F# continuations
/// </summary>
[<Interface>]
type ExtActor<'Message> =
    inherit Actor<'Message>
    inherit ExtContext

    abstract Become: Effect<'Message> -> unit
    abstract RunTask: Func<Task> -> unit

and [<Interface>] Effect<'Message> =
    abstract WasHandled: unit -> bool
    abstract OnApplied: ExtActor<'Message> * 'Message -> unit

type Receive<'Message, 'Context when 'Context :> Actor<'Message>> = 'Context -> 'Message -> Effect<'Message>

and TypedContext<'Message, 'Actor when 'Actor :> ActorBase and 'Actor :> IWithUnboundedStash>
    (context: IActorContext, actor: 'Actor) =
    let self = context.Self

    interface ExtActor<'Message> with
        member _.UntypedContext = context
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
        member _.Stop(ref: IActorRef<'T>) = context.Stop(untyped (ref))

        member _.Unhandled(msg) =
            match box actor with
            | :? FunActor<'Message> as act -> act.InternalUnhandled(msg)
            | _ -> raise (Exception("Couldn't use actor in typed context"))

        member _.Become(effect: Effect<'Message>) =
            match box actor with
            | :? FunActor<'Message> as act -> act.Become effect
            | _ -> raise (Exception("Couldn't use actor in typed context"))

        member _.RunTask(task: Func<Task>) =
            match box actor with
            | :? FunActor<'Message> as act -> act.InternalRunTask(task)
            | _ -> raise (Exception("Couldn't use actor in typed context"))

and [<AbstractClass>] Actor() =
    inherit UntypedActor()

    interface IWithUnboundedStash with
        member val Stash = null with get, set

and UnhandledSuppression = interface end

and LifecycleEvent =
    | PreStart
    | PostStop
    | PreRestart of cause: exn * message: obj
    | PostRestart of cause: exn

    interface UnhandledSuppression

and [<Struct>] Become<'Message>(next: 'Message -> Effect<'Message>) =
    member this.Next = next

    member _.Effect =
        let next = next

        { new Effect<'Message> with
            member _.WasHandled() = true

            member _.OnApplied(ctx: ExtActor<'Message>, msg: 'Message) =
                let behavior = next msg
                behavior.OnApplied(ctx, msg) }

    interface Effect<'Message> with
        member _.WasHandled() = true
        member this.OnApplied(ctx: ExtActor<'Message>, _: 'Message) = ctx.Become this.Effect

and AsyncEffect<'Message> =
    | AsyncEffect of Async<Effect<'Message>>
    | TaskEffect of Task<Effect<'Message>>

    interface Effect<'Message> with
        member _.WasHandled() = true

        member this.OnApplied(ctx: ExtActor<'Message>, msg: 'Message) =
            let rec runAsync (eff: Effect<'Message>) =
                task {
                    match eff with
                    | :? AsyncEffect<'Message> as e ->
                        let! eff =
                            match e with
                            | AsyncEffect x -> Async.StartImmediateAsTask x
                            | TaskEffect x -> x

                        return! runAsync eff
                    | effect -> effect.OnApplied(ctx, msg)
                }

            let effect = this
            ctx.RunTask(fun () -> runAsync effect :> Task)

and [<Struct>] CombinedEffect<'Message>(x: Effect<'Message>, y: Effect<'Message>) =
    interface Effect<'Message> with
        member this.WasHandled() = x.WasHandled() && y.WasHandled()

        member this.OnApplied(context: ExtActor<'Message>, message: 'Message) =
            x.OnApplied(context, message)
            y.OnApplied(context, message)

and ActorEffect<'Message> =
    | Unhandled
    | Stop
    | Ignore

    interface Effect<'Message> with
        member this.WasHandled() =
            match this with
            | Unhandled -> false
            | _ -> true

        member this.OnApplied(context: ExtActor<'Message>, message: 'Message) =
            match this with
            | Unhandled -> context.Unhandled message
            | Stop -> context.Stop(context.Self)
            | Ignore -> ()

and FunActor<'Message>(actor: Actor<'Message> -> Effect<'Message>) as this =
    inherit Actor()
    let untypedContext = UntypedActor.Context :> IActorContext
    let ctx = TypedContext<'Message, FunActor<'Message>>(untypedContext, this)

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
    member this.InternalRunTask(task: Func<Task>) : unit = this.RunTask task
    member this.InternalUnhandled(message: obj) : unit = this.Unhandled message
    override this.OnReceive msg = this.Handle msg

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

let (|Become|_|) (effect: Effect<'Message>) =
    match effect with
    | :? Become<'Message> as become -> Some become.Next
    | _ -> None
