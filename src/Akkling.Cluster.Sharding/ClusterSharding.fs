//-----------------------------------------------------------------------
// <copyright file="ClusterSharding.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Cluster.Sharding.ClusterSharding

open System
open Akka.Actor
open Akka.Cluster
open Akka.Cluster.Sharding
open Akkling

type internal TypedMessageExtractor<'Envelope, 'Message>(extractor: 'Envelope -> string*string*'Message) =
    interface IMessageExtractor with
        member this.ShardId message =
            match message with
            | :? 'Envelope as env -> 
                let shardId, _, _ = (extractor(env))
                shardId
            | _ -> null
        member this.EntityId message =
            match message with
            | :? 'Envelope as env -> 
                let _, entityId, _ = (extractor(env))
                entityId
            | _ -> null
        member this.EntityMessage message =
            match message with
            | :? 'Envelope as env -> 
                let _, _, msg = (extractor(env))
                box msg
            | _ -> null
            

type Extractor<'Envelope, 'Message> = 'Envelope -> string*string*'Message
type ShardResolver = string -> string

type internal TypedMessageExtractorEx<'Envelope, 'Message>(extractor: Extractor<_,'Message>, shardResolver :ShardResolver) =
    interface IMessageExtractor with
        member _.ShardId message =
            match message with
            | :? 'Envelope as env ->
                let shardId, _, _ = extractor env
                shardId
            | :? ShardRegion.StartEntity as e -> shardResolver (e.EntityId)
            | _ -> invalidOp <| message.ToString()
        member _.EntityId message =
            match message with
            | :? 'Envelope as env ->
                let _, entityId, _ = extractor env
                entityId
            | other -> invalidOp <| string other
        member _.EntityMessage message =
            match message with
            | :? 'Envelope as env ->
                let _, _, msg = extractor env
                box msg
            | other -> invalidOp <| string other

open Akkling.Persistence
// HACK over persistent actors
type FunPersistentShardingActor<'Message>(actor : Eventsourced<'Message> -> Effect<'Message>) as this =
    inherit FunPersistentActor<'Message>(actor)
    // sharded actors are produced in path like /user/{name}/{shardId}/{entityId}, therefore "{name}/{shardId}/{entityId}" is peristenceId of an actor
    let pid = this.Self.Path.Parent.Parent.Name + "/" + this.Self.Path.Parent.Name + "/" + this.Self.Path.Name
    override this.PersistenceId = pid

// this function hacks persistent functional actors props by replacing them with dedicated sharded version using different PeristenceId strategy
let internal adjustPersistentProps (props: Props<'Message>) : Props<'Message> =
    if props.ActorType = typeof<FunPersistentActor<'Message>> 
    then { props with ActorType = typeof<FunPersistentShardingActor<'Message>> }
    else props

/// <summary>
/// Creates a shard region responsible for managing shards located on the current cluster node as well as routing messages to shards on external nodes.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnSharded (extractor: 'Envelope -> string*string*'Message) (system: ActorSystem) (name: string) (props: Props<'Message>) : IActorRef<'Envelope> =
    let clusterSharding = ClusterSharding.Get(system)
    let adjustedProps = adjustPersistentProps props
    let shardRegion = clusterSharding.Start(name, adjustedProps.ToProps(), ClusterShardingSettings.Create(system), new TypedMessageExtractor<'Envelope, 'Message>(extractor))
    typed shardRegion
    
/// <summary>
/// Creates an Async returning shard region responsible for managing shards located on the current cluster node as well as routing messages to shards on external nodes.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnShardedAsync (extractor: 'Envelope -> string*string*'Message) (system: ActorSystem) (name: string) (props: Props<'Message>) : Async<IActorRef<'Envelope>> =
    let clusterSharding = ClusterSharding.Get(system)
    let adjustedProps = adjustPersistentProps props
    async {
        let! shardRegion = clusterSharding.StartAsync(name, adjustedProps.ToProps(), ClusterShardingSettings.Create(system), new TypedMessageExtractor<'Envelope, 'Message>(extractor)) |> Async.AwaitTask
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
    let shardRegion = clusterSharding.Start(name, adjustedProps.ToProps(), ClusterShardingSettings.Create(system), new TypedMessageExtractor<_,_>(EntityRefs.entityRefExtractor))
    { ShardRegion = shardRegion; TypeName = name }
/// <summary>
/// Creates a shard region and returns a factory function which for a given `shardId` and `entityId` returns a <see cref="IEntityRef{T}"/> representing
/// a serializable entity reference to a created sharded actor. This ref can be passed as message payload and will always point to a correct entity location
/// even after rebalancing. It also offers support for remember entities
/// </summary>
/// <param name="system"></param>
/// <param name="shardResolver">used for shard resolution from entity id. For remember entities support</param>
/// <param name="name"></param>
/// <param name="props"></param>
let entityFactoryForEx (system: ActorSystem) (shardResolver:ShardResolver) (name: string) (props: Props<'Message>) (rememberEntities) : EntityFac<'Message> =
    let clusterSharding = ClusterSharding.Get(system)
    let adjustedProps = adjustPersistentProps props
    let shardSettings =
        match rememberEntities with
        | true -> ClusterShardingSettings.Create(system).WithRememberEntities(true)
        | _ -> ClusterShardingSettings.Create(system);
    let shardRegion =
        clusterSharding.Start(name, adjustedProps.ToProps(),
            shardSettings, new TypedMessageExtractorEx<_,_>(EntityRefs.entityRefExtractor, shardResolver))
    { ShardRegion = shardRegion; TypeName = name }

/// <summary>
/// Creates a cluster shard proxy used for routing messages to shards on external nodes without hosting any shards by itself.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnShardedProxy (extractor: 'Envelope -> string*string*'Message) (system: ActorSystem) (name: string) (roleOption: string option) : IActorRef<'Envelope> =
    let clusterSharding = ClusterSharding.Get(system)
    let role = 
        match roleOption with
        | Some r -> r
        | _ -> ""
    let shardRegionProxy = clusterSharding.StartProxy(name, role, new TypedMessageExtractor<'Envelope, 'Message>(extractor))
    typed shardRegionProxy
    
/// <summary>
/// Creates an Async returning cluster shard proxy used for routing messages to shards on external nodes without hosting any shards by itself.
/// Extractor is a function returning tuple of ShardId*EntityId*Message used to determine routing path of message to the destination actor.
/// </summary>
let spawnShardedProxyAsync (extractor: 'Envelope -> string*string*'Message) (system: ActorSystem) (name: string) (roleOption: string option) : Async<IActorRef<'Envelope>> =
    let clusterSharding = ClusterSharding.Get(system)
    let role = 
        match roleOption with
        | Some r -> r
        | _ -> ""
    async {
        let! shardRegion = clusterSharding.StartProxyAsync(name, role, new TypedMessageExtractor<'Envelope, 'Message>(extractor)) |> Async.AwaitTask
        return typed shardRegion
    }

type ClusterShardingEffect<'Message> =
    | Passivate of obj
    interface Effect<'Message> with
        member __.WasHandled() = true
        member this.OnApplied(context : ExtActor<'Message>, message : 'Message) = 
            match this with
            | Passivate stopMessage -> context.Parent() <! Akka.Cluster.Sharding.Passivate(stopMessage)
            
/// <summary>
/// Returns an actor effect causing actor to send passivation request to it's shard. 
/// Afterwards shard will send <see cref="PoisonPill"/> message back to actor to stop it.
/// </summary>
let inline passivate (_: 'Any) : Effect<'Message> = ClusterShardingEffect.Passivate(PoisonPill.Instance) :> Effect<'Message>