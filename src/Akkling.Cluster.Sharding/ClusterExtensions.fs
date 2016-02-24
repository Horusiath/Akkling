//-----------------------------------------------------------------------
// <copyright file="ClusterExtensions.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016 Bartosz Sypytkowski <gttps://github.com/Horusiath>
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
    cluster.JoinSeedNodes (ImmutableList.CreateRange(addresses))

let (|IMemberEvent|_|) (msg: obj) : ClusterEvent.IMemberEvent option =
    match msg with
    | :? ClusterEvent.IMemberEvent as e -> Some e
    | _ -> None

let (|MemberUp|MemberExited|MemberRemoved|) (msg: ClusterEvent.IMemberEvent): Choice<Member, Member, Member> =
    match msg with
    | :? ClusterEvent.MemberUp as up -> Choice1Of3 (up.Member)
    | :? ClusterEvent.MemberExited as exit -> Choice2Of3 (exit.Member)  
    | :? ClusterEvent.MemberRemoved as removed -> Choice3Of3 (removed.Member)
    | _ -> failwith ("unknown cluster event type " + msg.GetType().ToString())