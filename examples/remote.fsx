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

open System
open Akkling
open Akka.Actor

let server = System.create "server" <| Configuration.parse """
    akka {
        actor.provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
        remote.helios.tcp {
            hostname = localhost
            port = 4500
        }
    }   
"""

let client = System.create "client" <| Configuration.parse """
    akka {
        actor.provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
        remote.helios.tcp {
            hostname = localhost
            port = 0
        }
    }   
"""

let spawnRemote addr sys name actor = 
    let options = [SpawnOption.Deploy(Deploy(RemoteScope(Address.Parse addr)))]
    spawne options sys name actor

let printer = spawnRemote "akka.tcp://server@localhost:4500" client "remote-actor" <@ actorOf2 (fun ctx msg -> printfn "%A received: %s" ctx.Self msg) @>

printer <! "hello"
printer <! "world"