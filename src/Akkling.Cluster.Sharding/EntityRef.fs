//-----------------------------------------------------------------------
// <copyright file="ClusterSharding.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2020 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Cluster.Sharding

open Akka.Actor
open Akkling.ActorRefs
open Akka.Cluster.Sharding
open Akka.Util

/// <summary>
/// A typed reference for a sharded entity, which lifecycle and placement is managed by cluster sharding plugin.
/// It may be serialized and used to route a message to an entity (even after rebalancing) or to create it ad-hoc
/// if it didn't exist yet. IEntityRef cannot be used on nodes which have not started shard regions for a 
/// corresponding <see cref="TypeName"/>.
/// </summary>
[<Interface>]
type IEntityRef<'Message> =

    /// Type name for a local shard region. It must be started in order to use this object.
    abstract member TypeName: string

    /// <summary>
    /// Shard identifier. Entities are managed in shards. Entities sharing the same <see cref="TypeName"/> and
    /// <see cref="ShardId"/> are always guaranteed to live on the same machine.
    /// </summary>
    abstract member ShardId: string

    /// <summary>
    /// Unique entity identifier in scope of the shard. Along with <see cref="TypeName"/> and <see cref="ShardId"/> 
    /// is a globaly distinct identifier of a target entity, persisted between its incarnations and rebalancing between nodes.
    /// </summary>
    abstract member EntityId: string
    inherit ICanTell<'Message>
    
type ShardEnvelope = 
    { ShardId: string
      EntityId: string
      Message: obj }

[<Sealed>]
type internal EntityRef<'Message>(shardRegion: IActorRef, typeName: string, shardId: string, entityId:string) =
    interface IEntityRef<'Message> with
        member __.TypeName = typeName
        member __.ShardId = shardId
        member __.EntityId = entityId
    interface ICanTell<'Message> with
        member __.Ask(msg, ?timeout) = async {
                let env = { ShardId = shardId; EntityId = entityId; Message = box msg }
                let! reply = shardRegion.Ask(env, Option.toNullable timeout) |> Async.AwaitTask
                match reply with
                | :? Status.Failure as f -> 
                    raise f.Cause
                    return Unchecked.defaultof<_>()
                | other -> return other :?> _ }            
        member __.Tell(msg, sender) =
            let env = { ShardId = shardId; EntityId = entityId; Message = box msg }
            shardRegion.Tell(env, sender)
        member __.Underlying = upcast shardRegion
    interface ISurrogated with
        member __.ToSurrogate(_system) = upcast { TypeName = typeName; ShardId = shardId; EntityId = entityId }

and EntityRefSurrogate<'Message> = 
    { TypeName: string
      ShardId: string
      EntityId: string }
    interface ISurrogate with
        member x.FromSurrogate(system) = 
            let sharding = ClusterSharding.Get system
            let shardRegion = sharding.ShardRegion x.TypeName
            upcast EntityRef<'Message>(shardRegion, x.TypeName, x.ShardId, x.EntityId)

type EntityFac<'Message> =
    { ShardRegion: IActorRef;
      TypeName: string }
    member x.RefFor shardId entityId = EntityRef<'Message>(x.ShardRegion, x.TypeName, shardId, entityId) :> IEntityRef<'Message>
    
module EntityRefs =

    /// <summary>
    /// Returns an entity message extractor prepared to work with <see cref="IEntityRef{T}"/>.
    /// </summary>
    /// <param name="env"></param>
    let inline entityRefExtractor<'Message> (env: ShardEnvelope): (string * string * 'Message)  = 
        (env.ShardId, env.EntityId, env.Message :?> 'Message)


