module Akkling.Persistence

open System
open Akka.Persistence
open Akka.Actor
open Akkling
open Microsoft.FSharp.Quotations

[<AbstractClass>]
type EventsourcedActor() = 
    inherit PersistentActor()
    interface IWithUnboundedStash with
        member val Stash = null with get, set
        
/// <summary>
/// Exposes an Akka.NET actor APi accessible from inside of F# continuations -> <see cref="Cont{'In, 'Out}" />
/// </summary>
[<Interface>]
type EventsourcedContext<'Message> = 
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
    /// Gets the current actor context.
    /// </summary>
    abstract Context : IActorContext
    
    /// <summary>
    /// Returns a sender of current message or <see cref="ActorRefs.NoSender" />, if none could be determined.
    /// </summary>
    abstract Sender<'Response> : unit -> IActorRef<'Response>
    
    /// <summary>
    /// Explicit signalization of unhandled message.
    /// </summary>
    abstract Unhandled : 'Message -> unit
    
    /// <summary>
    /// Lazy logging adapter. It won't be initialized until logging function will be called. 
    /// </summary>
    abstract Log : Lazy<Akka.Event.ILoggingAdapter>
    
    /// <summary>
    /// Defers provided function to be invoked when actor stops, regardless of reasons.
    /// </summary>
    abstract Defer : (unit -> unit) -> unit
    
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

type FunPersistentActor<'Message, 'Returned>(actor : EventsourcedContext<'Message> -> Cont<'Message, 'Returned>) as this =
    inherit EventsourcedActor()
    let mutable deferables = []
    
    let mutable state = 
        let self' = this.Self
        let context = UntypedActor.Context :> IActorContext
        actor { new EventsourcedContext<'Message> with
                    member __.Receive() = Input
                    member __.Self = typed self'
                    member __.Context = context
                    member __.Sender<'Response>() = typed (this.Sender()) :> IActorRef<'Response>
                    member __.Unhandled msg = this.Unhandled msg
                    member __.ActorOf(props, name) = context.ActorOf(props, name)
                    member __.ActorSelection(path : string) = context.ActorSelection(path)
                    member __.ActorSelection(path : ActorPath) = context.ActorSelection(path)
                    member __.Watch(aref : IActorRef) = context.Watch aref
                    member __.Unwatch(aref : IActorRef) = context.Unwatch aref
                    member __.Log = lazy (Akka.Event.Logging.GetLogger(context))
                    member __.Defer fn = deferables <- fn :: deferables
                    member __.Stash() = (this :> IWithUnboundedStash).Stash.Stash()
                    member __.Unstash() = (this :> IWithUnboundedStash).Stash.Unstash()
                    member __.UnstashAll() = (this :> IWithUnboundedStash).Stash.UnstashAll() }
    
    new(actor : Expr<EventsourcedContext<'Message> -> Cont<'Message, 'Returned>>) = FunPersistentActor(actor.Compile () ())
    member __.Sender() : IActorRef = base.Sender
    member __.Unhandled msg = base.Unhandled msg
    
    override x.ReceiveRecover msg = 
        match state with
        | Func f -> 
            match msg with
            | :? 'Message as m -> state <- f m
            | _ -> 
                let serializer = UntypedActor.Context.System.Serialization.FindSerializerForType typeof<obj> :?> Akka.Serialization.NewtonSoftJsonSerializer
                match Serialization.tryDeserializeJObject serializer.Serializer msg with
                | Some(m) -> state <- f m
                | None -> x.Unhandled msg
        | Return _ -> x.PostStop()
        true

    override x.ReceiveCommand msg = 
        match state with
        | Func f -> 
            match msg with
            | :? 'Message as m -> state <- f m
            | _ -> 
                let serializer = UntypedActor.Context.System.Serialization.FindSerializerForType typeof<obj> :?> Akka.Serialization.NewtonSoftJsonSerializer
                match Serialization.tryDeserializeJObject serializer.Serializer msg with
                | Some(m) -> state <- f m
                | None -> x.Unhandled msg
        | Return _ -> x.PostStop()
        true
    
    override x.PostStop() = 
        base.PostStop()
        List.iter (fun fn -> fn()) deferables