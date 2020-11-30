#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Remote"
#r "nuget: Akkling"

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
