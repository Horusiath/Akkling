open System
open System.IO
#if INTERACTIVE
let cd = Path.Combine(__SOURCE_DIRECTORY__, "../src/Akkling.DistributedData/bin/Debug")
System.IO.Directory.SetCurrentDirectory(cd)
#endif

#r "../src/Akkling.DistributedData/bin/Debug/System.Collections.Immutable.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling.DistributedData/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Helios.dll"
#r "../src/Akkling.DistributedData/bin/Debug/FsPickler.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Google.ProtocolBuffers.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Google.ProtocolBuffers.Serialization.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.Remote.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Google.ProtocolBuffers.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.Cluster.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.DistributedData.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.Serialization.Hyperion.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akkling.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akkling.DistributedData.dll"

open Akka.Cluster
open Akkling
open Akkling.DistributedData
open Akkling.DistributedData.Consistency

let system = System.create "system" <| Configuration.parse """
akka.actor.provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
akka.remote.helios.tcp {
    hostname = "127.0.0.1"
    port = 4551
}
"""
let cluster = Cluster.Get system
let replicator = getReplicator system

// some helper functions
let (++) set e = ORSet.add cluster e set

// initialize set
let set = [ 1; 2; 3 ] |> List.fold (++) ORSet.empty

let key = ORSet.key "test-set"

// write that up in replicator under key 'test-set'
async {
    let! reply = (retype replicator) <? update (ORSet.merge set) writeLocal set key
    match reply.Value with
    | UpdateSuccess(k, _) -> printfn "Data modified for key '%A'" k
    | DataDeleted k -> printfn "Data already deleted: '%A'" k
    | UpdateTimeout k -> printfn "Update of value for the key '%A' timed out" k
} |> Async.RunSynchronously

// read data 
async {
    let! reply = (retype replicator) <? get readLocal key
    match reply.Value with
    | GetSuccess(k, (data : ORSet<int>), _) -> printfn "Data for key %A: %A" k (data |> ORSet.value)
    | NotFound k -> printfn "Data for key '%A' not found" k
    | DataDeleted k -> printfn "Data for key '%A' has been deleted" k
    | GetFailure(k, _) -> printfn "Data for key '%A' didn't received in time" k
} |> Async.RunSynchronously

// delete data 
async {
    let! reply = (retype replicator) <? delete writeLocal key
    match reply.Value with
    | DeleteSuccess(k, _) -> printfn "Deleted data for key '%A'"  k
    | DeleteFailure(k, _) -> printfn "Timed out data deletion for key '%A'" k
    | DataDeleted k -> printfn "Data for key '%A' no longer exists" k
} |> Async.RunSynchronously