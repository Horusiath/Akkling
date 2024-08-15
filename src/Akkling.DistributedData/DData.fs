//-----------------------------------------------------------------------
// <copyright file="DData.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.DistributedData.DData

open System
open System.Collections.Generic
open Akka.Cluster
open Akka.DistributedData
open Akkling

let update modify consistency init key = Dsl.Update(key, init, consistency, System.Func<_,_>(modify))
let get consistency key = Dsl.Get(key, consistency)
let delete consistency key = Dsl.Delete(key, consistency)
    
let (|DataDeleted|_|) (msg: obj) =
    match msg with
    | :? DataDeleted as d -> Some d.Key
    | _ -> None
    
let (|GetSuccess|_|) (msg: obj) : (IKey * #IReplicatedData * obj) option =
    match msg with
    | :? GetSuccess as s when (s.Data :? 't) -> Some (s.Key, s.Data :?> 't, s.Request)
    | _ -> None
    
let (|GetFailure|_|) (msg: obj) =
    match msg with
    | :? GetFailure as d -> Some (d.Key, d.Request)
    | _ -> None
    
let (|NotFound|_|) (msg: obj) =
    match msg with
    | :? NotFound as d -> Some (d.Key, d.Request)
    | _ -> None
    
let (|UpdateSuccess|_|) (msg: obj) =
    match msg with
    | :? UpdateSuccess as d -> Some (d.Key, d.Request)
    | _ -> None
    
let (|ModifyFailure|_|) (msg: obj) =
    match msg with
    | :? ModifyFailure as d -> Some (d.Key, d.Cause, d.Request)
    | _ -> None
    
let (|UpdateTimeout|_|) (msg: obj) =
    match msg with
    | :? UpdateTimeout as d -> Some (d.Key, d.Cause, d.Request)
    | _ -> None
    
let (|DeleteSuccess|_|) (msg: obj) =
    match msg with
    | :? DeleteSuccess as d -> Some (d.Key, d.AlreadyDeleted)
    | _ -> None
    
let (|DeleteFailure|_|) (msg: obj) =
    match msg with
    | :? ReplicationDeleteFailure as d -> Some (d.Key, d.AlreadyDeleted)
    | _ -> None

let (|Changed|_|) (msg: obj) : (IKey * #IReplicatedData) option=
    match msg with
    | :? Changed as d when (d.Data :? 't) -> Some (d.Key, d.Data :?> 't)
    | _ -> None
    
let (|GetKeysIdsResult|_|) (msg: obj) =
    match msg with
    | :? GetKeysIdsResult as d -> Some (d.Keys |> Set.ofSeq)
    | _ -> None

/// Merges together two CRDTs of the same type.
let inline merge<'t when 't:> IReplicatedData<'t>>(x:'t) (y:'t): 't = x.Merge(y)

/// Merge operator - used to merge two CRDTs of the same type.
let inline (<*>) x y = merge x y

open System.Threading

type Akka.DistributedData.DistributedData with
    
    member x.TypedReplicator: IActorRef<IReplicatorMessage> = typed x.Replicator

    member x.AsyncGetKeys(?token: CancellationToken): Async<Set<string>> = 
        let t = defaultArg token CancellationToken.None
        async {
            let! reply = x.GetKeysAsync(t) |> Async.AwaitTask
            return Set.ofSeq reply }

    member x.AsyncGet(key, ?consistency, ?token): Async<_ option> =
        let c = defaultArg consistency (Dsl.ReadLocal :> IReadConsistency)
        let t = defaultArg token CancellationToken.None
        async {
            let! reply = x.GetAsync(key, c, t) |> Async.AwaitTask
            return reply |> Option.ofObj }

    member x.AsyncUpdate(key, value, ?consistency, ?token): Async<unit> =
        let c = defaultArg consistency (Dsl.WriteLocal :> IWriteConsistency)
        let t = defaultArg token CancellationToken.None
        x.UpdateAsync(key, value, c, t) |> Async.AwaitTask

    member x.AsyncDelete(key, ?consistency, ?token) : Async<unit> = 
        let c = defaultArg consistency (Dsl.WriteLocal :> IWriteConsistency)
        let t = defaultArg token CancellationToken.None
        x.DeleteAsync(key, c, t) |> Async.AwaitTask
