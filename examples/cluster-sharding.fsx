#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../packages/Helios/lib/net45/Helios.dll"
#r "../packages/FsPickler/lib/net45/FsPickler.dll"
#r "../packages/Google.ProtocolBuffers/lib/net40/Google.ProtocolBuffers.dll"
#r "../packages/Google.ProtocolBuffers/lib/net40/Google.ProtocolBuffers.Serialization.dll"
#r "../packages/Akka.Remote/lib/net45/Akka.Remote.dll"
#r "../src/Akkling.Persistence/bin/Debug/Google.ProtocolBuffers.dll"
#r "../src/Akkling.Persistence/bin/Debug/Akka.Persistence.dll"
#r "../src/Akkling.Persistence/bin/Debug/Akkling.Persistence.dll"
#r "../packages/Akka.Cluster/lib/net45/Akka.Cluster.dll"
#r "../packages/Akka.Cluster.Tools/lib/net45/Akka.Cluster.Tools.dll"
#r "../packages/Akka.Cluster.Sharding/lib/net45/Akka.Cluster.Sharding.dll"
#r "../src/Akkling.Cluster.Sharding/bin/Debug/Akkling.Cluster.Sharding.dll"

open System
open Akkling
open Akkling.Cluster
open Akkling.Cluster.Sharding
open Akka.Actor
open Akka.Cluster

let configWithPort port = 
    let config = Configuration.parse ("""
        akka {
          actor {
            provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
          }
          remote {
            helios.tcp {
              public-hostname = "localhost"
              hostname = "localhost"
              port = """ + port.ToString() + """
            }
          }
          cluster {
            auto-down-unreachable-after = 5s
            seed-nodes = [ "akka.tcp://cluster-system@localhost:5000/" ]
          }
          persistence {
            journal.plugin = "akka.persistence.journal.inmem"
            snapshot-store.plugin = "akka.persistence.snapshot-store.local"
          }
        }
        """)
    config.WithFallback(Tools.Singleton.ClusterSingletonManager.DefaultConfig())

let behavior (ctx : Actor<_>) msg = printfn "%A received %s" (ctx.Self.Path.ToStringWithAddress()) msg |> ignored

// spawn two separate systems with shard regions on each of them

let system1 = System.create "cluster-system" (configWithPort 5000)
let shardRegion1 = spawnSharded id system1 "printer" <| props (actorOf2 behavior)

// wait a while before starting a second system

let system2 = System.create "cluster-system" (configWithPort 5001)
let shardRegion2 = spawnSharded id system2 "printer" <| props (actorOf2 behavior)

// send hello world to entities on 4 different shards (this means that we will have 4 entities in total)
// NOTE: even thou we sent all messages through single shard region, 
//       some of them will be executed on the second one thanks to shard balancing

shardRegion1 <! ("shard-1", "entity-1", "hello world 1")
shardRegion1 <! ("shard-2", "entity-1", "hello world 2")
shardRegion1 <! ("shard-3", "entity-1", "hello world 3")
shardRegion1 <! ("shard-4", "entity-1", "hello world 4")

// check which shards have been build on the second shard region

open Akka.Cluster.Sharding

let printShards shardReg =
    async {
        let! reply = (retype shardReg) <? GetShardRegionStats.Instance
        let (stats: ShardRegionStats) = reply.Value
        for kv in stats.Stats do
            printfn "\tShard '%s' has %d entities on it" kv.Key kv.Value
    } |> Async.RunSynchronously

printfn "Shards active on node 'localhost:5000':" 
printShards shardRegion1
printfn "Shards active on node 'localhost:5001':" 
printShards shardRegion2