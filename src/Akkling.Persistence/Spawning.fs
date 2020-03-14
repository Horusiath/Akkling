//-----------------------------------------------------------------------
// <copyright file="Spawning.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akkling

[<AutoOpen>]
module Props =

    /// <summary>
    /// Creates a props describing a way to incarnate persistent actor with behavior described by <paramref name="receive"/> function.
    /// </summary>
    let propsPersist (receive: Eventsourced<'Message> -> Effect<'Message>) : Props<'Message> =
        Props<'Message>.Create<FunPersistentActor<'Message>, Eventsourced<'Message>, 'Message>(receive)
        
