#load "../.paket/load/net452/Akka.Serialization.Hyperion.fsx"
#load "../.paket/load/net452/Akka.DistributedData.fsx"
#r "../src/Akkling.Cluster.Sharding/bin/Debug/net452/Akkling.dll"
#r "../src/Akkling.DistributedData/bin/Debug/net452/Akkling.DistributedData.dll"

open Akka.Cluster
open Akka.DistributedData
open Akkling
open Akkling.DistributedData
open Akkling.DistributedData.Consistency

let system = System.create "system" <| Configuration.parse """
akka.actor.provider = cluster
akka.remote.dot-netty.tcp {
    hostname = "127.0.0.1"
    port = 4551
}
"""
let cluster = Cluster.Get system
let ddata = DistributedData.Get system

// some helper functions
let (++) set e = ORSet.add cluster e set

// initialize set
let set = [ 1; 2; 3 ] |> List.fold (++) ORSet.empty

let key = ORSet.key "test-set"

// write that up in replicator under key 'test-set'
ddata.AsyncUpdate(key, set, writeLocal)
|> Async.RunSynchronously

// read data 
async {
    let! reply = ddata.AsyncGet(key, readLocal)
    match reply with
    | Some value -> printfn "Data for key %A: %A" key value
    | None -> printfn "Data for key '%A' not found" key
} |> Async.RunSynchronously

// delete data 
ddata.AsyncDelete(key, writeLocal) |> Async.RunSynchronously