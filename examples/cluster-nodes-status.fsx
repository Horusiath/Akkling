#r "nuget: Akka.Serialization.Hyperion"
#r "nuget: Akka.Cluster.Sharding"
#r "nuget: Akkling"
#r "nuget: Akkling.Persistence"
#r "nuget: Akkling.Cluster.Sharding"

open System
open System.IO
open Akka.Actor
open Akka.Configuration
open Akka.Cluster
open Akka.Cluster.Tools.Singleton
open Akka.Cluster.Sharding
open Akka.Persistence

open Akkling
open Akkling.Persistence
open Akkling.Cluster
open Akkling.Cluster.Sharding
open Hyperion

let configWithPort port =
    let config = Configuration.parse ("""
        akka {
            actor {
              provider = cluster
              serializers {
                hyperion = "Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion"
              }
              serialization-bindings {
                "System.Object" = hyperion
              }
            }
          remote {
            dot-netty.tcp {
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
    config.WithFallback(ClusterSingletonManager.DefaultConfig())

type ClusterStatus = Get
let clusterStatus = fun (ctx : Actor<_>) ->
    let log = ctx.Log.Value
    let cluster = Cluster.Get(ctx.System)
    let rec loop nodes = actor {
        let! (msg: obj) = ctx.Receive ()
        match msg with
        | LifecycleEvent e ->
            match e with
            | PreStart ->
                cluster.Subscribe(untyped ctx.Self, ClusterEvent.InitialStateAsEvents,
                    [| typedefof<ClusterEvent.IMemberEvent> |])
                log.Info $"Actor subscribed to Cluster status updates: {ctx.Self}"
            | PostStop ->
                cluster.Unsubscribe(untyped ctx.Self)
                log.Info $"Actor unsubscribed from Cluster status updates: {ctx.Self}"

            | _ -> return Unhandled
            return! loop nodes
        | IMemberEvent e ->
            match e with
            | MemberJoined m ->
                log.Info $"Node joined: {m}"
                return! loop (nodes |> Map.add (m.Address.ToString(), m.UniqueAddress.ToString()) m.Status)
            | MemberUp m ->
                log.Info $"Node up: {m}"
                return! loop (nodes |> Map.add (m.Address.ToString(), m.UniqueAddress.ToString()) m.Status)
            | MemberWeaklyUp m ->
                log.Info $"Node weakly up: {m}"
                return! loop (nodes |> Map.add (m.Address.ToString(), m.UniqueAddress.ToString()) m.Status)
            | MemberLeft m ->
                log.Info $"Node left: {m}"
                return! loop (nodes |> Map.add (m.Address.ToString(), m.UniqueAddress.ToString()) m.Status)
            | MemberDowned m ->
                log.Info $"Node downed: {m}"
                return! loop (nodes |> Map.add (m.Address.ToString(), m.UniqueAddress.ToString()) m.Status)
            | MemberExited m ->
                log.Info $"Node exited: {m}"
                return! loop (nodes |> Map.add (m.Address.ToString(), m.UniqueAddress.ToString()) m.Status)
            | MemberRemoved m ->
                log.Info $"Node removed: {m}"
                return! loop (nodes |> Map.remove (m.Address.ToString(), m.UniqueAddress.ToString()))
            return! loop nodes
        | :? ClusterStatus as cs ->
            match cs with ClusterStatus.Get -> ctx.Sender() <! nodes
            return! loop nodes
        | _ -> return Unhandled
    }
    loop (Map.empty)

let system1 = System.create "cluster-system" (configWithPort 5000)
let node1 = spawn system1 "clusterStatus" <| props (clusterStatus)

// wait a while before starting a second system
System.Threading.Thread.Sleep(200)

let system2 = System.create "cluster-system" (configWithPort 5001)

let printNodes clusterStatus =
    async {
        let! reply = (retype clusterStatus) <? ClusterStatus.Get
        let (stats: Map<string * string, MemberStatus>) = reply
        for kv in stats do
            let (a, ua) = kv.Key
            let ms = kv.Value
            printfn $"\tNode '%s{a}':'%s{ua}' status: '%A{ms}'"
    } |> Async.RunSynchronously

printNodes node1

let system3 = System.create "cluster-system" (configWithPort 5002)
let node3 = spawn system3 "clusterStatus" <| props (clusterStatus)

printNodes node3
