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
open Newtonsoft.Json.Linq

type IO<'T> = 
    | Input

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
    abstract Receive : unit -> IO<'Message>
    
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

[<Interface>]
type ExtContext =
    /// <summary>
    /// Returns current actor incarnation
    /// </summary>
    abstract Incarnation : unit -> ActorBase
    
    /// <summary>
    /// Stops execution of provided actor.
    /// </summary>
    abstract Stop : IActorRef<'T> -> unit
    
    /// <summary>
    /// Marks message as unhandled.
    /// </summary>
    abstract Unhandled : obj -> unit

/// <summary>
/// Exposes an Akka.NET extended actor API accessible from inside of F# continuations 
/// </summary>
[<Interface>]
type ExtActor<'Message> = 
    inherit Actor<'Message>
    inherit ExtContext

type Receive<'Message, 'Context when 'Context :> Actor<'Message>> = 'Context -> 'Message -> Effect<'Message>
and TypedContext<'Message, 'Actor when 'Actor :> ActorBase and 'Actor :> IWithUnboundedStash>(context : IActorContext, actor : 'Actor) = 
    let self = context.Self
    interface ExtActor<'Message> with
        member __.UntypedContext = context
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
            context.System.Scheduler.ScheduleTellRepeatedlyCancelable(delay, interval, untyped target, message, self)
        member __.Incarnation() = actor :> ActorBase
        member __.Stop(ref : IActorRef<'T>) = context.Stop(untyped(ref))
        member __.Unhandled(msg) = 
            match box actor with
            | :? FunActor<'Message> as act -> act.InternalUnhandled(msg)
            | _ -> raise (Exception("Couldn't use actor in typed context"))
            
and [<AbstractClass>]Actor() = 
    inherit UntypedActor()
    interface IWithUnboundedStash with
        member val Stash = null with get, set

and LifecycleEvent = 
    | PreStart
    | PostStop
    | PreRestart of cause : exn * message : obj
    | PostRestart of cause : exn


and [<Interface>]Effect<'Message> = 
    abstract WasHandled : unit -> bool
    abstract OnApplied : ExtActor<'Message> * 'Message -> unit

and [<Struct>]Become<'Message>(next: 'Message -> Effect<'Message>) =
    member this.Next = next
    interface Effect<'Message> with
        member __.WasHandled () = true
        member __.OnApplied(_ : ExtActor<'Message>, _: 'Message) = ()    
        
and [<Struct>]AsyncEffect<'Message>(asyncEffect: Async<Effect<'Message>>) =
    member __.Effect = asyncEffect
    interface Effect<'Message> with
        member __.WasHandled () = true
        member __.OnApplied(_ : ExtActor<'Message>, _: 'Message) = ()    

and ActorEffect<'Message> = 
    | Unhandled
    | Stop
    | Ignore
    interface Effect<'Message> with
        member this.WasHandled () =
            match this with 
            | Unhandled -> false
            | _ -> true
        member this.OnApplied(context : ExtActor<'Message>, message : 'Message) = 
            match this with
            | Unhandled -> context.Unhandled message
            | Stop -> context.Stop (context.Self)
            | Ignore -> ()    

and FunActor<'Message>(actor : Actor<'Message>->Effect<'Message>) as this = 
    inherit Actor()
    let untypedContext = UntypedActor.Context :> IActorContext
    let ctx = TypedContext<'Message, FunActor<'Message>>(untypedContext, this)
    let mutable behavior = actor ctx    
    member __.Next (current : Effect<'Message>) (_context : Actor<'Message>) (message : obj) : Effect<'Message> = 
        match message with
        | :? 'Message as msg -> 
            match current with
            | :? Become<'Message> as become -> become.Next msg
            | _ -> current
        | :? LifecycleEvent -> 
            // we don't treat unhandled lifecycle events as casual unhandled messages
            current
        | :? JObject as jobj ->
            let msg = jobj.ToObject<'Message>()
            match current with
            | :? Become<'Message> as become -> become.Next msg
            | _ -> current
        | other -> 
            this.Unhandled other
            current

    member __.Handle (msg: obj) = 
        let nextBehavior = this.Next behavior ctx msg
        match nextBehavior with
        | :? Become<'Message> -> behavior <- nextBehavior
        | :? AsyncEffect<'Message> as a ->
            Akka.Dispatch.ActorTaskScheduler.RunTask(System.Func<System.Threading.Tasks.Task>(fun () -> 
                let task = 
                    async {
                        let! eff = a.Effect
                        match eff with
                        | :? Become<'Message> -> behavior <- eff
                        | effect -> effect.OnApplied(ctx, msg :?> 'Message)
                        () } |> Async.StartAsTask
                upcast task ))
        | effect -> effect.OnApplied(ctx, msg :?> 'Message)
    
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

let (|Become|_|) (effect: Effect<'Message>) =
    match effect with 
    | :? Become<'Message> as become -> Some become.Next
    | _ -> None