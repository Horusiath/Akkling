//-----------------------------------------------------------------------
// <copyright file="Collections.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2016 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.DistributedData

open System
open System.Collections.Generic
open Akka.Cluster
open Akka.DistributedData

[<RequireQualifiedAccess>]
module Flag = 
    let inline key id = FlagKey id
    let empty = Akka.DistributedData.Flag.False
    let inline merge (x: Flag) (y: Flag) = x.Merge(y)
    let inline value (x: Flag) = x.Enabled
    let inline on (x: Flag) = x.SwitchOn()

[<RequireQualifiedAccess>]
module GCounter = 
    let inline key id = GCounterKey id
    let empty = Akka.DistributedData.GCounter.Empty
    let inline merge (x: GCounter) (y: GCounter) = x.Merge(y)
    let inline value (x: GCounter) = x.Value
    let inline inc (node: Cluster) (n: uint64) (x: GCounter) = x.Increment(node, n)
    
[<RequireQualifiedAccess>]
module PNCounter = 
    let inline key id = PNCounterKey id
    let empty = Akka.DistributedData.PNCounter.Empty
    let inline merge (x: PNCounter) (y: PNCounter) = x.Merge(y)
    let inline value (x: PNCounter) = x.Value
    let inline inc (node: Cluster) (n: int64) (x: PNCounter) = x.Increment(node, n)
    let inline dec (node: Cluster) (n: int64) (x: PNCounter) = x.Decrement(node, n)
    
type GSet<'t> = Akka.DistributedData.GSet<'t>

[<RequireQualifiedAccess>]
module GSet = 
    let inline key<'t> id = GSetKey<'t> id
    let empty<'t> = Akka.DistributedData.GSet<'t>.Empty
    let inline merge<'t> (x: GSet<'t>) (y: GSet<'t>) = x.Merge(y)
    let inline value<'t> (x: GSet<'t>) = x.Elements
    let inline add<'t> (e: 't) (x: GSet<'t>) = x.Add e
    let inline isEmpty<'t> (x: GSet<'t>) = x.Count = 0
    let inline count<'t> (x: GSet<'t>) = x.Count
    let inline contains<'t> (e: 't) (x: GSet<'t>) = x.Contains e
    let ofList<'t> (list: 't list) = list |> List.fold (fun gset e -> add e gset) empty
    let ofArray<'t> (array: 't []) = array |> Array.fold (fun gset e -> add e gset) empty
    let ofSeq<'t> (seq: 't seq) = seq |> Seq.fold (fun gset e -> add e gset) empty
    let toList<'t> (x: GSet<'t>) = x.Elements |> List.ofSeq
    let toArray<'t> (x: GSet<'t>) = x.Elements |> Array.ofSeq
    
type ORSet<'t> = Akka.DistributedData.ORSet<'t>

[<RequireQualifiedAccess>]
module ORSet = 
    let inline key<'t> id = ORSetKey<'t> id
    let empty<'t> = Akka.DistributedData.ORSet<'t>.Empty
    let inline merge<'t> (x: ORSet<'t>) (y: ORSet<'t>) = x.Merge(y)
    let inline value<'t> (x: ORSet<'t>) = x.Elements
    let inline add<'t> (node: Cluster) (e: 't) (x: ORSet<'t>) = x.Add(node, e)
    let inline remove<'t> (node: Cluster) (e: 't) (x: ORSet<'t>) = x.Remove(node, e)
    let inline isEmpty<'t> (x: ORSet<'t>) = x.IsEmpty
    let inline count<'t> (x: ORSet<'t>) = x.Count
    let inline contains<'t> (e: 't) (x: ORSet<'t>) = x.Contains e
    let inline clear<'t> (node: Cluster) (x: ORSet<'t>) = x.Clear(node.SelfUniqueAddress)
    let ofList<'t> (node: Cluster) (list: 't list) = list |> List.map (fun e -> KeyValuePair(node.SelfUniqueAddress, e)) |> ORSet.Create
    let ofArray<'t> (node: Cluster) (array: 't []) = array |> Array.map (fun e -> KeyValuePair(node.SelfUniqueAddress, e)) |> ORSet.Create
    let ofSeq<'t> (node: Cluster) (seq: 't seq) = seq |> Seq.map (fun e -> KeyValuePair(node.SelfUniqueAddress, e)) |> ORSet.Create
    let toList<'t> (x: GSet<'t>) = x.Elements |> List.ofSeq
    let toArray<'t> (x: GSet<'t>) = x.Elements |> Array.ofSeq
    
type ORMap<'t,'v when 'v :> IReplicatedData> = ORDictionary<'t,'v>

[<RequireQualifiedAccess>]
module ORMap = 
    let inline key id = ORDictionaryKey id
    let empty<'k, 'v when 'v :> IReplicatedData> : ORMap<'k, 'v> = ORMap<'k, 'v>.Empty
    let inline merge (x: ORMap<_,_>) (y: ORMap<_,_>) = x.Merge(y)
    let inline value (x: ORMap<_,_>) = x.Entries
    let inline add (node: Cluster) k v (x: ORMap<_,_>) = x.SetItem(node, k, v)
    let inline remove (node: Cluster) k (x: ORMap<_,_>) = x.Remove(node, k)
    let inline isEmpty (x: ORMap<_,_>) = x.IsEmpty
    let inline count (x: ORMap<_,_>) = x.Count
    let inline containsKey k (x: ORMap<_,_>) = x.ContainsKey k
    let tryFind k (x: ORMap<_,_>) = 
        match x.TryGetValue k with
        | true, e -> Some e
        | false, _ -> None
    let inline find k (x: ORMap<_,_>) = x.[k]
    let inline keys (x: ORMap<_,_>) = x.Keys
    let inline values (x: ORMap<_,_>) = x.Values
    let ofList (node: Cluster) (list) : ORMap<_,_> = list |> List.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> ORDictionary.Create
    let ofArray (node: Cluster) (array) : ORMap<_,_> = array |> Array.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> ORDictionary.Create
    let ofMap (node: Cluster) (map) : ORMap<_,_> = map |> Map.toSeq |> Seq.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> ORDictionary.Create
    let ofSeq (node: Cluster) (seq) : ORMap<_,_> = seq |> Seq.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> ORDictionary.Create
    let toList (x: ORMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> List.ofSeq
    let toArray (x: ORMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> Array.ofSeq
    let toSeq (x: ORMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value))
    let toMap (x: ORMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> Map.ofSeq

type ORMultiMap<'t, 'v> = ORMultiDictionary<'t, 'v>

[<RequireQualifiedAccess>]
module ORMultiMap = 
    let inline key id = ORMultiDictionaryKey id
    let empty<'k, 'v when 'v :> IReplicatedData> : ORMultiMap<'k, 'v> = ORMultiMap<'k, 'v>.Empty
    let inline merge (x: ORMultiMap<_,_>) (y: ORMultiMap<_,_>) = x.Merge(y)
    let inline value (x: ORMultiMap<_,_>) = x.Entries
    let inline add (node: Cluster) k (items: 'v seq) (x: ORMultiMap<_,_>) = x.SetItems(node, k, System.Collections.Immutable.ImmutableHashSet.Create<'v>(items |> Seq.toArray))
    let inline addItem (node: Cluster) k v (x: ORMultiMap<_,_>) = x.AddItem(node, k, v)
    let inline remove (node: Cluster) k (x: ORMultiMap<_,_>) = x.Remove(node, k)
    let inline removeItem (node: Cluster) k v (x: ORMultiMap<_,_>) = x.RemoveItem(node, k, v)
    let inline replaceItem (node: Cluster) k vold vnew (x: ORMultiMap<_,_>) = x.ReplaceItem(node, k, vold, vnew)
    let inline isEmpty (x: ORMultiMap<_,_>) = x.IsEmpty
    let inline count (x: ORMultiMap<_,_>) = x.Count
    let inline containsKey k (x: ORMultiMap<_,_>) = x.ContainsKey k
    let tryFind k (x: ORMultiMap<_,_>) = 
        match x.TryGetValue k with
        | true, e -> Some e
        | false, _ -> None
    let find k (x: ORMultiMap<_,_>) = x.[k]
    let keys (x: ORMultiMap<_,_>) = x.Keys
    let toSeq (x: ORMultiMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value |> Set.ofSeq))
    let toList (x: ORMultiMap<_,_>) = toSeq x |> Seq.toList
    let toArray (x: ORMultiMap<_,_>) = toSeq x |> Seq.toArray
    let toMap (x: ORMultiMap<_,_>) = toSeq x |> Map.ofSeq

type LWWReg<'t> = LWWRegister<'t>

[<RequireQualifiedAccess>]
module LWWReg = 
    let inline key<'t> id = LWWRegisterKey<'t> id
    let inline create<'t> (node: Cluster) (value: 't): LWWReg<'t> = LWWRegister(node.SelfUniqueAddress, value)
    let inline createWithTimestamp<'t> (node: Cluster) (timestamp: int64) (value: 't): LWWReg<'t> = LWWRegister(node.SelfUniqueAddress, value, timestamp)
    let inline createWithClock<'t> (node: Cluster) (clock: int64 -> 't -> int64) (value: 't): LWWReg<'t> = LWWRegister(node.SelfUniqueAddress, value, Clock<'t>(clock))
    let inline merge<'t> (x: LWWReg<'t>) (y: LWWReg<'t>) = x.Merge y
    let inline value<'t> (x: LWWReg<'t>) = x.Value
    let inline set<'t> (node: Cluster) v (x: LWWReg<_>) = x.WithValue(node.SelfUniqueAddress, v)
    let inline timestamp<'t> (x: LWWReg<'t>) = x.Timestamp
    let inline lastUpdatedBy<'t> (x: LWWReg<'t>) = x.UpdatedBy

type LWWMap<'k, 'v> = LWWDictionary<'k, 'v>

[<RequireQualifiedAccess>]
module LWWMap = 
    let inline key id = LWWDictionaryKey id
    let empty<'k, 'v> : LWWMap<'k, 'v> = LWWDictionary.Empty
    let inline merge (x: LWWMap<_,_>) (y: LWWMap<_,_>) = x.Merge(y)
    let inline value (x: LWWMap<_,_>) = x.Entries
    let inline add (node: Cluster) k v (x: LWWMap<_,_>) = x.SetItem(node, k, v)
    let inline remove (node: Cluster) k (x: LWWMap<_,_>) = x.Remove(node, k)
    let inline isEmpty (x: LWWMap<_,_>) = x.IsEmpty
    let inline count (x: LWWMap<_,_>) = x.Count
    let inline containsKey k (x: LWWMap<_,_>) = x.ContainsKey k 
    let tryFind k (x: LWWMap<_,_>) = 
        match x.TryGetValue k with
        | true, e -> Some e
        | false, _ -> None
    let inline find k (x: LWWMap<_,_>) = x.[k]
    let inline keys (x: LWWMap<_,_>) = x.Keys
    let inline values (x: LWWMap<_,_>) = x.Values
    let ofList (node: Cluster) (list) : LWWMap<_,_> = list |> List.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> LWWDictionary.Create
    let ofArray (node: Cluster) (array) : LWWMap<_,_> = array |> Array.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> LWWDictionary.Create
    let ofMap (node: Cluster) (map) : LWWMap<_,_> = map |> Map.toSeq |> Seq.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> LWWDictionary.Create
    let ofSeq (node: Cluster) (seq) : LWWMap<_,_> = seq |> Seq.map (fun (k, v) -> (node.SelfUniqueAddress, k, v)) |> LWWDictionary.Create
    let toList (x: LWWMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> List.ofSeq
    let toArray (x: LWWMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> Array.ofSeq
    let toSeq (x: LWWMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value))
    let toMap (x: LWWMap<_,_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> Map.ofSeq

type PNCounterMap<'k> = PNCounterDictionary<'k>

[<RequireQualifiedAccess>]
module PNCounterMap =
    let inline key id = PNCounterDictionaryKey id
    let empty<'k, 'v> : PNCounterMap<'k> = PNCounterDictionary.Empty
    let inline merge (x: PNCounterMap<_>) (y: PNCounterMap<_>) = x.Merge(y)
    let inline value (x: PNCounterMap<_>) = x.Entries
    let inline incKey (node: Cluster) k v (x: PNCounterMap<_>) = x.Increment(node, k, v)
    let inline decKey (node: Cluster) k v (x: PNCounterMap<_>) = x.Decrement(node, k, v)
    let inline remove (node: Cluster) k (x: PNCounterMap<_>) = x.Remove(node, k)
    let inline isEmpty (x: PNCounterMap<_>) = x.IsEmpty
    let inline count (x: PNCounterMap<_>) = x.Count
    let inline containsKey k (x: PNCounterMap<_>) = x.ContainsKey k 
    let tryFind k (x: PNCounterMap<_>) = 
        match x.TryGetValue k with
        | true, e -> Some e
        | false, _ -> None
    let inline find k (x: PNCounterMap<_>) = x.[k]
    let inline keys (x: PNCounterMap<_>) = x.Keys
    let inline values (x: PNCounterMap<_>) = x.Values
    let toList (x: PNCounterMap<_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> List.ofSeq
    let toArray (x: PNCounterMap<_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> Array.ofSeq
    let toSeq (x: PNCounterMap<_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value))
    let toMap (x: PNCounterMap<_>) = x.Entries |> Seq.map (fun entry -> (entry.Key, entry.Value)) |> Map.ofSeq