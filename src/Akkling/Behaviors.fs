
//-----------------------------------------------------------------------
// <copyright file="Actors.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Behaviors

/// <summary>
/// An empty actor behavior, which ignores all incoming messages.
/// </summary>
let ignore (context:Actor<'Message>) : Behavior<'Message> = 
    let rec loop () = actor {
        let! _ = context.Receive()
        return! loop ()
    }
    loop ()

/// <summary>
/// Actor behavior, which resends message back to it's sender.
/// </summary>
let echo (context: Actor<'Message>) : Behavior<'Message> =
    let rec loop () = actor {
        let! msg = context.Receive()
        context.Sender () <! msg
        return! loop ()    
    }
    loop ()