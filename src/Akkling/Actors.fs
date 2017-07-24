//-----------------------------------------------------------------------
// <copyright file="Actors.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open System
open Akka.Actor
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

type TypedContext<'Message, 'Actor when 'Actor :> ActorBase and 'Actor :> IWithUnboundedStash>(context : IActorContext, actor : 'Actor) as this = 
    let self = context.Self
    member __.Incarnation() = actor :> ActorBase
    interface Actor<'Message> with
        member __.Stop(ref : IActorRef<'T>) = context.Stop(untyped(ref))
        member __.Unhandled(msg) = 
            match box actor with
            | :? FunActor<'Message> as act -> act.InternalUnhandled(msg)
            | _ -> raise (Exception("Couldn't use actor in typed context"))
        member __.UntypedContext = context
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
            context.System.Scheduler.ScheduleTellOnceCancelable(delay, untyped target, message, self)
        member __.ScheduleRepeatedly (delay : TimeSpan) (interval : TimeSpan) target message = 
            context.System.Scheduler.ScheduleTellRepeatedlyCancelable(delay, interval, untyped target, message, self)

and [<AbstractClass>]Actor() = 
    inherit UntypedActor()
    interface IWithUnboundedStash with
        member val Stash = null with get, set

and LifecycleEvent = 
    | PreStart
    | PostStop
    | PreRestart of cause : exn * message : obj
    | PostRestart of cause : exn

and FunActor<'Message>(initialBehavior : Behavior<'Message, _>) as this = 
    inherit Actor()
    let untypedContext = UntypedActor.Context :> IActorContext
    let ctx = TypedContext<'Message, FunActor<'Message>>(untypedContext, this)
    let mutable behavior = 
        match initialBehavior with
        | :? Deferred<_,_> as defer -> defer.Factory(ctx)
        | _ -> initialBehavior
        
    let runAsync(a : Async<'t>): unit =
            Akka.Dispatch.ActorTaskScheduler.RunTask(System.Func<System.Threading.Tasks.Task>(fun () -> 
                let task = a |> Async.StartAsTask
                upcast task ))
        
    let processNext (next: Behavior<_,_>) msg current =
        behavior <- next.OnApply ctx msg current
        
    member __.Handle (msg: obj) = 
        let current = behavior
        let next =
            match msg with
            | :? 'Message as userMessage -> current.ReceiveMessage ctx userMessage            
            | signal -> current.ReceiveSignal ctx signal // currently we have no common interface for handling signals
        match next with
        | Synchronous newBehavior -> processNext newBehavior msg current
        | Asynchronous cont -> async {
            let! newBehavior = cont
            do processNext newBehavior msg current } |> runAsync
    
    member __.Sender() : IActorRef = base.Sender
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