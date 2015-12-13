//-----------------------------------------------------------------------
// <copyright file="MessagePatterns.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
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
    

let (|ActorIdentity|_|) (msg: obj) : ('CorrelationId * IActorRef<'T> option) option =
    match msg with
    | :? ActorIdentity as identity -> 
        let ref = 
            if identity.Subject <> null
            then Some (typed identity.Subject)
            else None
        Some((identity.MessageId :?> 'CorrelationId, ref))
    | _ -> None