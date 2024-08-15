//-----------------------------------------------------------------------
// <copyright file="Patterns.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Persistence.Patterns

open Akka.Persistence

let (|SnapshotOffer|_|) (msg: obj) : 'Snapshot option =
    match msg with
    | :? SnapshotOffer as d ->
        match d.Snapshot with
        | :? 'Snapshot as snapshot -> Some snapshot
        | _ -> None
    | _ -> None

let (|Persisted|_|) (context: Eventsourced<'Message>) (msg: 'Message) : 'Message option =
    if context.HasPersisted ()
    then Some msg
    else None

let (|Deffered|_|) (context: Eventsourced<'Message>) (msg: 'Message) : 'Message option =
    if context.HasDeffered ()
    then Some msg
    else None

let (|PersistentLifecycleEvent|_|) (message: obj) : PersistentLifecycleEvent option =
    match message with
    | :? PersistentLifecycleEvent as e -> Some e
    | _ -> None