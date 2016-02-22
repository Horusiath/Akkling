//-----------------------------------------------------------------------
// <copyright file="PersistentView.fs" company="Akka.NET Project">
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

[<Interface>]
type View<'Message> = 
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
    /// Returns last sequence number attached to latest persisted event.
    /// </summary>
    abstract LastSequenceNr : unit -> int64
    
    /// <summary>
    /// Persistent actor's identifier that doesn't change across different actor incarnations.
    /// </summary>
    abstract Pid : PID

    /// <summary>
    /// Identifier of particular view, NOT associated with view of persistent actor related to view itself.
    /// </summary>
    abstract ViewId : PID

and ViewContext<'Event> = interface end

and [<Interface>]ExtView<'Message> =
    inherit View<'Message>
    inherit ExtActor<'Message>
    inherit ViewContext<'Message>
            
and TypedViewContext<'Message, 'Actor when 'Actor :> FunPersistentView<'Message>>(context : IActorContext, actor : 'Actor) as this = 
    let self = context.Self
    interface ExtView<'Message> with
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
        member __.LastSequenceNr () = actor.LastSequenceNr
        member __.Pid = actor.PersistenceId
        member __.ViewId = actor.ViewId
            
and FunPersistentView<'Message>(actor : View<'Message> -> Effect<'Message>, persistentId: string) as this = 
    inherit PersistentView()
    let untypedContext = UntypedActor.Context
    let ctx = TypedViewContext<'Message, FunPersistentView<'Message>>(untypedContext, this)
    let mutable behavior = actor ctx
    new(actor : Expr<View<'Message> -> Effect<'Message>>, persistentId: string) = FunPersistentView(actor.Compile () (), persistentId)
    
    member __.Next (current : Effect<'Message>) (context : Actor<'Message>) (message : obj) : Effect<'Message> = 
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
        | :? Become<'Message> -> behavior <- nextBehavior
        | effect -> effect.OnApplied(ctx, msg :?> 'Message)
    
    member __.Sender() : IActorRef = base.Sender
    member __.InternalUnhandled(message: obj) : unit = base.Unhandled message
    override this.PersistenceId = persistentId
    override this.ViewId = this.Self.Path.Name
    override this.Receive msg = 
        this.Handle msg
        true
    
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