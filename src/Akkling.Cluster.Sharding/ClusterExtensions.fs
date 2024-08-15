//-----------------------------------------------------------------------
// <copyright file="ClusterExtensions.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Cluster.ClusterExtensions

open System
open System.Collections.Immutable
open Akka.Actor
open Akka.Cluster
open Akkling

let joinCluster (system: ActorSystem) (addresses: Address seq) : unit =
    let cluster = Cluster.Get system
    cluster.JoinSeedNodes(ImmutableList.CreateRange(addresses))

let (|IMemberEvent|_|) (msg: obj) : ClusterEvent.IMemberEvent option =
    match msg with
    | :? ClusterEvent.IMemberEvent as e -> Some e
    | _ -> None

let (|MemberJoined|MemberUp|MemberLeft|MemberExited|MemberRemoved|MemberDowned|MemberWeaklyUp|)
    (msg: ClusterEvent.IMemberEvent)
    : Choice<Member, Member, Member, Member, Member, Member, Member> =
    match msg with
    | :? ClusterEvent.MemberUp as up -> Choice1Of7(up.Member)
    | :? ClusterEvent.MemberJoined as joined -> Choice2Of7(joined.Member)
    | :? ClusterEvent.MemberLeft as left -> Choice3Of7(left.Member)
    | :? ClusterEvent.MemberExited as exited -> Choice4Of7(exited.Member)
    | :? ClusterEvent.MemberRemoved as removed -> Choice5Of7(removed.Member)
    | :? ClusterEvent.MemberDowned as downed -> Choice6Of7(downed.Member)
    | :? ClusterEvent.MemberWeaklyUp as up -> Choice7Of7(up.Member)
    | _ -> failwith ("unknown cluster event type " + msg.GetType().ToString())
