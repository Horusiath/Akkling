//-----------------------------------------------------------------------
// <copyright file="ClusterSingleton.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2024 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Cluster.Singleton

open System
open Akka.Actor
open Akka.Cluster
open Akka.Cluster.Tools.Singleton
open Akkling

/// <summary>
/// Spawns an actor in cluster singleton mode.
/// </summary>
/// <param name="stopMessage">Message used to stop an actor</param>
/// <param name="system">Actor system used to spawn an actor</param>
/// <param name="name">Actor singleton name.</param>
/// <param name="props">Props used to build an actor.</param>
let spawnSingleton (stopMessage: obj) (system: ActorSystem) (name: string) (props: Props<'Message>) : IActorRef<'Message> =
    let singletonProps = ClusterSingletonManager.Props(props.ToProps(), stopMessage, ClusterSingletonManagerSettings.Create(system))
    typed (system.ActorOf(singletonProps, name))

/// <summary>
/// Spawns an actor working as a proxy to cluster singleton provided by <paramref name="singletonPath"/>.
/// </summary>
/// <param name="system">Actor system used to spawn an actor</param>
/// <param name="name">Actor proxy name</param>
/// <param name="singletonPath">Relative path to cluster singleton actor manager</param>
let spawnSingletonProxy (system: ActorSystem) (name: string) (singletonPath: string) : IActorRef<'Message> =
    let proxyProps = ClusterSingletonProxy.Props(singletonPath, ClusterSingletonProxySettings.Create(system))
    typed (system.ActorOf(proxyProps, name))