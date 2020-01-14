//-----------------------------------------------------------------------
// <copyright file="Patterns.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
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

let (|Recovering|_|) (context: Eventsourced<'Message>) (msg: 'Message) : 'Message option =
    if context.IsRecovering ()
    then Some msg
    else None
