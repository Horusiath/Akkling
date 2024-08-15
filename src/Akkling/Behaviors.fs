
//-----------------------------------------------------------------------
// <copyright file="Actors.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Behaviors

/// <summary>
/// An empty actor behavior, which ignores all incoming messages.
/// </summary>
let ignore (context:Actor<'Message>) : Effect<'Message> = 
    let rec loop () = actor {
        let! _ = context.Receive()
        return! loop ()
    }
    loop ()

/// <summary>
/// Actor behavior, which resends message back to it's sender.
/// </summary>
let echo (context: Actor<'Message>) : Effect<'Message> =
    let rec loop () = actor {
        let! msg = context.Receive()
        context.Sender () <! msg
        return! loop ()    
    }
    loop ()

/// <summary>
/// Actor behavior, which prints message in formatted string to standard output.
/// </summary>
let printf (fmt: Printf.TextWriterFormat<'Message->unit>) (context: Actor<'Message>) : Effect<'Message> =
    let rec loop () = actor {
        let! msg = context.Receive()
        printf fmt msg
        return! loop ()    
    }
    loop ()