//-----------------------------------------------------------------------
// <copyright file="MessagePatterns.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------
[<AutoOpen>]
module Akkling.MessagePatterns

open Akka.Actor

/// <summary>
/// Active pattern that matches message agains <see cref="Terminated"/> message.
/// First parameter is ref to terminated actor, second is existence confirmed, third address terminated flag.
/// </summary>
let (|Terminated|_|) (msg: obj) : (IActorRef<'T> * bool * bool) option =
    match msg with
    | :? Terminated as t -> Some((typed t.ActorRef, t.ExistenceConfirmed, t.AddressTerminated))
    | _ -> None
    
/// <summary>
/// Active pattern that matches message agains <see cref="ActorIdentity"/> message.
/// This is the result of <see cref="Identify"/> request send with matching correlation id.
/// Response contains actor ref of the requested identity or None if no actor was found.
/// </summary>
let (|ActorIdentity|_|) (msg: obj) : ('CorrelationId * IActorRef<'T> option) option =
    match msg with
    | :? ActorIdentity as identity -> 
        let ref = 
            if identity.Subject <> null
            then Some (typed identity.Subject)
            else None
        Some((identity.MessageId :?> 'CorrelationId, ref))
    | _ -> None