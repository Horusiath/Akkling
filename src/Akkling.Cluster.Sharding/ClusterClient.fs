//-----------------------------------------------------------------------
// <copyright file="ClusterClient.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Cluster.ClusterClient

open Akkling
open Akka.Actor
open Akka.Cluster.Tools.Client

/// <summary>
/// Returns cluster client receptionist, allowing actors to register themselves to be visible outside the cluster.
/// </summary>
let receptionist (system: ActorSystem) : ClusterClientReceptionist = ClusterClientReceptionist.Get(system)

/// <summary>
/// Returns actor reference to cluster client, allowing you to send messages to cluster.
/// </summary>
let clusterClient (system: ActorSystem) : IActorRef<obj> = typed <| system.ActorOf(ClusterClient.Props(ClusterClientSettings.Create system))