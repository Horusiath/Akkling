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
#r "../src/Akkling.DistributedData/bin/Debug/DotNetty.Common.dll"
#r "../src/Akkling.DistributedData/bin/Debug/DotNetty.Buffers.dll"
#r "../src/Akkling.DistributedData/bin/Debug/DotNetty.Codecs.dll"
#r "../src/Akkling.DistributedData/bin/Debug/DotNetty.Handlers.dll"
#r "../src/Akkling.DistributedData/bin/Debug/DotNetty.Transport.dll"
#r "../src/Akkling.DistributedData/bin/Debug/FsPickler.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Google.Protobuf.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.Remote.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.Cluster.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.DistributedData.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akka.Serialization.Hyperion.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akkling.dll"
#r "../src/Akkling.DistributedData/bin/Debug/Akkling.DistributedData.dll"

open Akka.Cluster
open Akka.DistributedData
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