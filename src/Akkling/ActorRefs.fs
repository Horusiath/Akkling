//-----------------------------------------------------------------------
// <copyright file="ActorRefs.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.ActorRefs

open Akka.Actor
open Akka.Util
open System
open System.Threading.Tasks

/// <summary>
/// Result of <see cref="ICanTell{Message}.Ask"/> operation, can be either <see cref="Ok" /> or <see cref="Error" />.
/// </summary>
type AskResult<'TResult> =
    | Ok of result: 'TResult
    | Error of exn
    with member x.Value =
            match x with
            | Ok result -> result
            | Error err -> raise <| InvalidOperationException("Accessed the Value of an AskResult in an error state.", err)

let private tryCast<'Result>(t: Task<obj>) : AskResult<'Result> =
    if t.IsFaulted then Error t.Exception
    else
        try
            match t.Result with
            | :? 'TResult as res -> Ok res
            | :? AskResult<'TResult> as res -> res
            | :? Status.Failure as fail -> Error fail.Cause
            | :? exn as ex -> Error ex
            | other ->
                let msg = sprintf "Ask expected type %s but received type %s: %A" (typeof<'TResult>.FullName) (other.GetType().FullName) other
                Error(InvalidCastException msg)
        with ex ->
            Error ex

/// <summary>
/// Typed version of <see cref="ICanTell"/> interface. Allows to tell/ask using only messages of restricted type.
/// </summary>
[<Interface>]
type ICanTell<'Message> =
    //inherit ICanTell
    abstract Tell : 'Message * IActorRef -> unit
    abstract Ask : 'Message * TimeSpan option -> Async<AskResult<'Response>>

    abstract member Underlying : ICanTell

[<Interface>]
type IInternalTypedActorRef =
    abstract member Underlying : IActorRef
    abstract member MessageType : Type

/// <summary>
/// Typed version of <see cref="IActorRef"/> interface. Allows to tell/ask using only messages of restricted type.
/// </summary>
[<Interface>]
type IActorRef<'Message> =
    inherit IInternalTypedActorRef
    inherit ICanTell<'Message>
    //inherit IActorRef

    inherit IEquatable<IActorRef<'Message>>
    inherit IComparable<IActorRef<'Message>>
    inherit ISurrogated
    inherit IComparable
    
    /// <summary>
    /// Changes the type of handled messages, returning new typed ActorRef.
    /// </summary>
    abstract Retype<'T> : unit -> IActorRef<'T>
    abstract Forward : 'Message -> unit

    abstract member Path : ActorPath

/// <summary>
/// Wrapper around untyped instance of IActorRef interface.
/// </summary>
[<Struct>]
[<CustomEquality>]
[<CustomComparison>]
type TypedActorRef<'Message>(underlyingRef : IActorRef) =

    /// <summary>
    /// Gets an underlying actor reference wrapped by current object.
    /// </summary>
    member __.Underlying = underlyingRef

    override __.ToString () = underlyingRef.ToString ()
    override __.GetHashCode () = underlyingRef.GetHashCode ()
    override this.Equals o =
        match o with
        | :? IInternalTypedActorRef as ref -> underlyingRef.Equals(ref.Underlying)
        | _ -> false

    interface IInternalTypedActorRef with
        
        member __.Underlying = underlyingRef
        member __.MessageType = typeof<'Message>

    interface IActorRef<'Message> with

        /// <summary>
        /// Changes the type of handled messages, returning new typed ActorRef.
        /// </summary>
        member __.Retype<'T>() = TypedActorRef<'T>(underlyingRef) :> IActorRef<'T>

        member __.Tell(message : 'Message, sender : IActorRef) = underlyingRef.Tell(message :> obj, sender)
        member __.Forward(message : 'Message) = underlyingRef.Forward(message)
        member __.Ask(message : 'Message, timeout : TimeSpan option) : Async<AskResult<'Response>> =
            underlyingRef
                .Ask(message, Option.toNullable timeout)
                .ContinueWith(tryCast<'Response>, TaskContinuationOptions.ExecuteSynchronously)
            |> Async.AwaitTask
        member __.Underlying = underlyingRef :> ICanTell
        member __.Path = underlyingRef.Path

        member __.Equals other =
            underlyingRef.Equals(other.Underlying)

        member __.CompareTo (other: obj) =
            match other with
            | :? IInternalTypedActorRef as typed -> underlyingRef.CompareTo(typed.Underlying)
            | _ -> underlyingRef.CompareTo(other)

        member __.CompareTo (other: IActorRef<'Message>) =
            underlyingRef.CompareTo(other.Underlying)

    interface ISurrogated with
        member this.ToSurrogate system =
            let surrogate : TypedActorRefSurrogate<'Message> = { Wrapped = underlyingRef }
            surrogate :> ISurrogate

and TypedActorRefSurrogate<'Message> =
    { Wrapped : IActorRef }
    interface ISurrogate with
        member this.FromSurrogate system =
            let tref = new TypedActorRef<'Message>(this.Wrapped)
            tref :> ISurrogated

/// <summary>
/// Returns typed wrapper over provided actor reference.
/// </summary>
let inline typed (actorRef : IActorRef) : IActorRef<'Message> =
    (TypedActorRef<'Message> actorRef) :> IActorRef<'Message>

/// <summary>
/// Returns untyped <see cref="IActorRef" /> form of current typed actor.
/// </summary>
let inline untyped (typedRef: IActorRef<'Message>) : IActorRef =
    (typedRef :?> TypedActorRef<'Message>).Underlying

/// <summary>
/// Changes type of messages handled by provided typedRef, returning new typed actor ref.
/// </summary>
let inline retype (typedRef: IActorRef<'T>) : IActorRef<'U> = typedRef.Retype<'U>()

/// <summary>
/// Typed wrapper for <see cref="ActorSelection"/> objects.
/// </summary>
[<Struct>]
[<CustomEquality>]
[<CustomComparison>]
type TypedActorSelection<'Message>(selection : ActorSelection) =

    /// <summary>
    /// Returns an underlying untyped <see cref="ActorSelection"/> instance.
    /// </summary>
    member __.Underlying = selection

    /// <summary>
    /// Gets and actor ref anchor for current selection.
    /// </summary>
    member __.Anchor with get (): IActorRef<'Message> = typed selection.Anchor

    /// <summary>
    /// Gets string representation for all elements in actor selection path.
    /// </summary>
    member __.PathString with get () = selection.PathString

    /// <summary>
    /// Gets collection of elements, actor selection path is build from.
    /// </summary>
    member __.Path with get () = selection.Path

    override __.ToString () = selection.ToString ()

    /// <summary>
    /// Tries to resolve an actor reference from current actor selection.
    /// </summary>
    member __.ResolveOne (timeout: TimeSpan): Async<IActorRef<'Message>> =
        let convertToTyped (t: System.Threading.Tasks.Task<IActorRef>) = typed t.Result
        selection.ResolveOne(timeout).ContinueWith(convertToTyped)
        |> Async.AwaitTask

    override x.Equals (o:obj) =
        if obj.ReferenceEquals(x, o) then true
        else match o with
        | :? TypedActorSelection<'Message> as t -> x.Underlying.Equals t.Underlying
        | _ -> x.Underlying.Equals o

    override __.GetHashCode () = selection.GetHashCode() ^^^ typeof<'Message>.GetHashCode()

    interface ICanTell with
        member __.Tell(message : obj, sender : IActorRef) = selection.Tell(message, sender)

    interface ICanTell<'Message> with
        member __.Tell(message : 'Message, sender : IActorRef) : unit = selection.Tell(message, sender)
        member __.Ask(message : 'Message, timeout : TimeSpan option) : Async<AskResult<'Response>> =
            selection
                .Ask(message, Option.toNullable timeout)
                .ContinueWith(tryCast<'Response>, TaskContinuationOptions.ExecuteSynchronously)
            |> Async.AwaitTask

        member __.Underlying = selection :> ICanTell

    interface IComparable with
        member this.CompareTo other =
            match other with
            | :? TypedActorSelection<_> as typed -> typed.Underlying.PathString.CompareTo (this.Underlying.PathString)
            | :? ActorSelection as untyped -> untyped.PathString.CompareTo (this.Underlying.PathString)
            | _ -> -1

/// <summary>
/// Unidirectional send operator.
/// Sends a message object directly to actor tracked by actorRef.
/// </summary>
let inline (<!) (actorRef : #ICanTell<'Message>) (msg : 'Message) : unit =
    actorRef.Tell(msg, ActorCell.GetCurrentSelfOrNoSender())

/// <summary>
/// Bidirectional send operator. Sends a message object directly to actor
/// tracked by actorRef and awaits for response send back from corresponding actor.
/// </summary>
let inline (<?) (tell : #ICanTell<'Message>) (msg : 'Message) : Async<AskResult<'Response>> = tell.Ask<'Response>(msg, None)

/// <summary>
/// Unidirectional forward operator.
/// Sends a message object directly to actor tracked by actorRef without overriding it's sender.
/// </summary>
let inline (<<!) (actorRef : #IActorRef<'Message>) (msg : 'Message) : unit =
    actorRef.Forward(msg)

/// Pipes an output of asynchronous expression directly to the recipients mailbox.
let pipeTo (sender : IActorRef) (recipient : ICanTell<'Message>) (computation : Async<'Message>): unit =
    let success (result : 'Message) : unit = recipient.Tell(result, sender)
    let failure (err : exn) : unit = recipient.Underlying.Tell(Status.Failure(err), sender)
    Async.StartWithContinuations(computation, success, failure, failure)

/// Pipe operator which sends an output of asynchronous expression directly to the recipients mailbox.
let inline (|!>) (computation : Async<'Message>) (recipient : ICanTell<'Message>) =
    pipeTo ActorRefs.NoSender recipient computation

/// Pipe operator which sends an output of asynchronous expression directly to the recipients mailbox
let inline (<!|) (recipient : ICanTell<'Message>) (computation : Async<'Message>) =
    pipeTo ActorRefs.NoSender recipient computation

/// <summary>
/// Returns an instance of <see cref="ActorSelection" /> for specified path.
/// If no matching receiver will be found, a <see cref="ActorRefs.NoSender" /> instance will be returned.
/// </summary>
let inline select (selector : IActorRefFactory) (path : string) : TypedActorSelection<'Message> =
    TypedActorSelection(selector.ActorSelection path)
