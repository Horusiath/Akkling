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
#r "../packages/Akka.Persistence.Sql.Common/lib/net45/Akka.Persistence.Sql.Common.dll"
#r "../packages/System.Data.SQLite.Core/lib/net45/System.Data.SQLite.dll"
#r "../packages/Akka.Persistence.Sqlite/lib/net45/Akka.Persistence.Sqlite.dll"
#r "../src/Akkling.Cluster.Sharding/bin/Debug/Akkling.Cluster.Sharding.dll"

open System
open Akkling
open Akkling.Cluster
open Akkling.Cluster.Sharding
open Akka.Actor
open Akka.Cluster

let configWithPort port = 
    let config = Configuration.parse("""
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
            journal {
              plugin = "akka.persistence.journal.sqlite"
              sqlite {
                connection-string = "Data Source=.\\store.db;Version=3;"
                auto-initialize = true
              }
            }
            snapshot-store {
              plugin = "akka.persistence.snapshot-store.sqlite"
              sqlite {
                connection-string = "Data Source=.\\store.db;Version=3;"
                auto-initialize = true
              }
            }
          }
        }
        """)
    config.WithFallback(Tools.Singleton.ClusterSingletonManager.DefaultConfig())
    
// first cluster system with sharding region up and ready
let system1 = System.create "cluster-system" (configWithPort 5000)
let shardRegion1 = spawnSharded id system1 "printer" <| props (Behaviors.printf "Received: %s\n")

// second cluster system with sharding region up and ready
let system2 = System.create "cluster-system" (configWithPort 5001)
let shardRegion2 = spawnSharded id system2 "printer" <| props (Behaviors.printf "Received: %s\n")

// shard region will distribute messages to entities in corresponding shards
shardRegion1 <! ("shard-1", "entity-1", "hello world 1")
shardRegion1 <! ("shard-2", "entity-1", "hello world 2")
shardRegion1 <! ("shard-3", "entity-1", "hello world 3")
shardRegion1 <! ("shard-4", "entity-1", "hello world 4")