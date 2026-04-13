//-----------------------------------------------------------------------
// <copyright file="Spawning.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2026 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2026 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2026 Bartosz Sypytkowski, Vagif Abilov and contributors <https://github.com/Horusiath/Akkling>
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
