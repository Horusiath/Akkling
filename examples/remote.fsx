#load "../.paket/load/net452/Akka.Serialization.Hyperion.fsx"
#load "../.paket/load/net452/Akka.Remote.fsx"
#r "../src/Akkling/bin/Debug/net452/Akkling.dll"

open System
open Akkling
open Akka.Actor

let server = System.create "server" <| Configuration.parse """
    akka {
        actor.provider = remote
        remote.dot-netty.tcp {
            hostname = localhost
            port = 4500
        }
    }
"""

let client = System.create "client" <| Configuration.parse """
    akka {
        actor.provider = remote
        remote.dot-netty.tcp {
            hostname = localhost
            port = 0
        }
    }
"""

let remoteProps addr actor = { propse actor with Deploy = Some (Deploy(RemoteScope(Address.Parse addr))) }

let printer =
    spawn client "remote-actor" (remoteProps "akka.tcp://server@localhost:4500" <@ actorOf2 (fun ctx msg -> printfn "%A received: %s" ctx.Self msg |> ignored) @>)

printer <! "hello"
printer <! "world"