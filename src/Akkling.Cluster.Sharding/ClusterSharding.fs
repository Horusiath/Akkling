//-----------------------------------------------------------------------
// <copyright file="ClusterSharding.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Cluster.Sharding.ClusterSharding

open System
open Akka.Actor
open Akka.Cluster
open Akka.Cluster.Sharding
open Akkling

// With the addition of ShardId overload computed from entityId TypedMessageExtractor no longer provides
// the best fit for IMessageExtractor implementation but can still be used in with the new version of
// IMessageExtractor - the new ShardId overload uses messageHint to compute ShardId.
// To take full advantage of the new version of IMessageExtractor TypedMessageExtractor constructor should
// be changed to accept extractor argument with a different function signature.
// Such change will break backward compatibility of spawnSharded method, so there should be considered
// a new function (with a different name) to spawn sharded actors.
type internal TypedMessageExtractor<'Envelope, 'Message>(extractor: 'Envelope -> string * string * 'Message) =
    interface IMessageExtractor with
        member this.ShardId message =
            match message with
            | :? 'Envelope as env ->
                let shardId, _, _ = extractor (env)
                shardId
            | _ -> null

        member this.ShardId(_entityId, messageHint) =
            match messageHint with
            | :? 'Envelope as env ->
                let shardId, _, _ = extractor (env)
                shardId
            | _ -> null

        member this.EntityId message =
            match message with
            | :? 'Envelope as env ->
                let _, entityId, _ = extractor (env)
                entityId
            | _ -> null

        member this.EntityMessage message =
            match message with
            | :? 'Envelope as env ->
                let _, _, msg = extractor (env)
                box msg
            | _ -> null


open Akkling.Persistence
// HACK over persistent actors
type FunPersistentShardingActor<'Message>(actor: Eventsourced<'Message> -> Effect<'Message>) as this =
    inherit FunPersistentActor<'Message>(actor)
    // sharded actors are produced in path like /user/{name}/{shardId}/{entityId}, therefore "{name}/{shardId}/{entityId}" is peristenceId of an actor
    let pid =
        this.Self.Path.Parent.Parent.Name
        + "/"
        + this.Self.Path.Parent.Name
        + "/"
        + this.Self.Path.Name

    override this.PersistenceId = pid

// this function hacks persistent functional actors props by replacing them with dedicated sharded version using different PeristenceId strategy
let internal adjustPersistentProps (props: Props<'Message>) : Props<'Message> =
    if props.ActorType = typeof<FunPersistentActor<'Message>> then
        { props with
            ActorType = typeof<FunPersistentShardingActor<'Message>> }
    else
        props

/// <summary>
/// Creates a shard region responsible for managing shards located on the current cluster node as well as routing messages to shards on external nodes.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnSharded
    (extractor: 'Envelope -> string * string * 'Message)
    (system: ActorSystem)
    (name: string)
    (props: Props<'Message>)
    : IActorRef<'Envelope> =
    let clusterSharding = ClusterSharding.Get(system)
    let adjustedProps = adjustPersistentProps props

    let shardRegion =
        clusterSharding.Start(
            name,
            adjustedProps.ToProps(),
            ClusterShardingSettings.Create(system),
            new TypedMessageExtractor<'Envelope, 'Message>(extractor)
        )

    typed shardRegion

/// <summary>
/// Creates an Async returning shard region responsible for managing shards located on the current cluster node as well as routing messages to shards on external nodes.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnShardedAsync
    (extractor: 'Envelope -> string * string * 'Message)
    (system: ActorSystem)
    (name: string)
    (props: Props<'Message>)
    : Async<IActorRef<'Envelope>> =
    let clusterSharding = ClusterSharding.Get(system)
    let adjustedProps = adjustPersistentProps props

    async {
        let! shardRegion =
            clusterSharding.StartAsync(
                name,
                adjustedProps.ToProps(),
                ClusterShardingSettings.Create(system),
                new TypedMessageExtractor<'Envelope, 'Message>(extractor)
            )
            |> Async.AwaitTask

        return typed shardRegion
    }

/// <summary>
/// Creates a shard region and returns a factory function which for a given `shardId` and `entityId` returns a <see cref="IEntityRef{T}"/> representing
/// a serializable entity reference to a created sharded actor. This ref can be passed as message payload and will always point to a correct entity location
/// even after rebalancing.
/// </summary>
/// <param name="system"></param>
/// <param name="name"></param>
/// <param name="props"></param>
let entityFactoryFor (system: ActorSystem) (name: string) (props: Props<'Message>) : EntityFac<'Message> =
    let clusterSharding = ClusterSharding.Get(system)
    let adjustedProps = adjustPersistentProps props

    let shardRegion =
        clusterSharding.Start(
            name,
            adjustedProps.ToProps(),
            ClusterShardingSettings.Create(system),
            new TypedMessageExtractor<_, _>(EntityRefs.entityRefExtractor)
        )

    { ShardRegion = shardRegion
      TypeName = name }


/// <summary>
/// Creates a cluster shard proxy used for routing messages to shards on external nodes without hosting any shards by itself.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnShardedProxy
    (extractor: 'Envelope -> string * string * 'Message)
    (system: ActorSystem)
    (name: string)
    (roleOption: string option)
    : IActorRef<'Envelope> =
    let clusterSharding = ClusterSharding.Get(system)

    let role =
        match roleOption with
        | Some r -> r
        | _ -> ""

    let shardRegionProxy =
        clusterSharding.StartProxy(name, role, new TypedMessageExtractor<'Envelope, 'Message>(extractor))

    typed shardRegionProxy

/// <summary>
/// Creates an Async returning cluster shard proxy used for routing messages to shards on external nodes without hosting any shards by itself.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnShardedProxyAsync
    (extractor: 'Envelope -> string * string * 'Message)
    (system: ActorSystem)
    (name: string)
    (roleOption: string option)
    : Async<IActorRef<'Envelope>> =
    let clusterSharding = ClusterSharding.Get(system)

    let role =
        match roleOption with
        | Some r -> r
        | _ -> ""

    async {
        let! shardRegion =
            clusterSharding.StartProxyAsync(name, role, new TypedMessageExtractor<'Envelope, 'Message>(extractor))
            |> Async.AwaitTask

        return typed shardRegion
    }

type ClusterShardingEffect<'Message> =
    | Passivate of obj

    interface Effect<'Message> with
        member _.WasHandled() = true

        member this.OnApplied(context: ExtActor<'Message>, message: 'Message) =
            match this with
            | Passivate stopMessage -> context.Parent() <! Akka.Cluster.Sharding.Passivate(stopMessage)

/// <summary>
/// Returns an actor effect causing actor to send passivation request to it's shard.
/// Afterwards shard will send <see cref="PoisonPill"/> message back to actor to stop it.
/// </summary>
let inline passivate (_: 'Any) : Effect<'Message> =
    ClusterShardingEffect.Passivate(PoisonPill.Instance) :> Effect<'Message>
