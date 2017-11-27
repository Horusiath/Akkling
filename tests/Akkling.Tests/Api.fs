//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.Api

open Akkling
open Akkling.TestKit
open Akka.Actor
open System
open Xunit
open Akka.Routing
open Akka.Routing

[<Fact>]
let ``configuration loader should load data from app.config``() = 
    let config = Configuration.load()
    config.HasPath "akka.test.value" |> equals true
    config.GetInt "akka.test.value" |> equals 10

[<Fact>]
let ``can serialize expression decider``() = 
    let decider = ExprDecider <@ fun e -> Directive.Resume @>
    use sys = System.create "system" (Configuration.defaultConfig())
    let serializer = sys.Serialization.FindSerializerFor decider
    let bytes = serializer.ToBinary decider
    let des = serializer.FromBinary(bytes, typeof<ExprDecider>) :?> IDecider
    des.Decide(Exception()) |> equals (Directive.Resume)

type TraceableMessage<'a> = 
    { Trace : TraceMetadata
      Message : 'a }

and TraceMetadata = 
    | NoTrace
    | Trace of TraceId : uint64 * SpanId : uint64 * ParentSpanId : uint64 option

type Availability = 
    | NotFound
    | UnableToDetermineAvailability of Reason : string
    | Availability of AvailabilityAtTimeHorizon list

and AvailabilityAtTimeHorizon = 
    { AvailableAsOf : DateTime
      Quantity : decimal }

type ItemAvailabilityMessage = 
    | QueryItemAvailability of Handle : string * Options : AvailabilityOptions
    | Reconfigure

and AvailabilityOptions = 
    { SkipCache : bool }
    static member Defaults = { SkipCache = false }

type ItemAvailabilityReply = 
    | AvailabilityReply of Handle : string * Availability : Availability

[<Fact>]
let ``can serialize discriminated unions``() = 
    let x = 
        { Trace = Trace(10UL, 1000UL, Some 11000UL)
          Message = 
              Availability([ { AvailableAsOf = DateTime.Now
                               Quantity = 10M } ]) }
    
    use sys = System.create "system" (Configuration.defaultConfig())
    let serializer = sys.Serialization.FindSerializerFor x
    let bytes = serializer.ToBinary x
    let des = serializer.FromBinary(bytes, typeof<TraceableMessage<Availability>>) :?> TraceableMessage<Availability>
    des |> equals x

[<Fact>]
let ``can serialize typed actor ref``() = 
    use sys = System.create "system" (Configuration.defaultConfig())
    let echo = spawn sys "echo" <| (props Behaviors.echo)
    let serializer = sys.Serialization.FindSerializerFor echo
    let bytes = serializer.ToBinary echo
    let des = serializer.FromBinary(bytes, echo.GetType()) :?> IActorRef<string>
    des |> equals echo
    
type TestMsg = { Ref: IActorRef<string> }

[<Fact>]
let ``Typed actor refs are serializable/deserializable in both directions`` () : unit = testDefault <| fun tck ->
    let aref = spawn tck "actor" (props Behaviors.echo)
    let msg = { Ref = aref }
    let serializer = tck.Sys.Serialization.FindSerializerFor msg
    let bin = serializer.ToBinary(msg)
    let deserialized = serializer.FromBinary(bin, null) :?> TestMsg
    
    Assert.Equal(msg, deserialized)
    msg.Ref <! "ok"
    expectMsg tck "ok" |> ignore
    
[<Fact>]
let ``Typed props are serializable/deserializable in both directions`` () : unit = testDefault <| fun tck ->
    let p = { (propse <@ Behaviors.echo @>) with 
                 Deploy = Some Deploy.Local; 
                 Router = Some Akka.Routing.NoRouter.NoRouter; 
                 SupervisionStrategy = Some (SupervisorStrategy.StoppingStrategy  :> SupervisorStrategy);
                 Mailbox = Some "xyz123";
                 Dispatcher = Some "xyz" }
    let serializer = tck.Sys.Serialization.FindSerializerFor p
    let bin = serializer.ToBinary(p)
    let deserialized = serializer.FromBinary(bin, null) :?> Props<obj>
    
    Assert.Equal(p.ActorType, deserialized.ActorType)
    Assert.NotNull(p.Args)
    Assert.Equal(p.Args.Length, 1)
    Assert.IsType<Microsoft.FSharp.Quotations.Expr<Actor<obj> -> Effect<obj>>>(p.Args.[0]) |> ignore
    Assert.Equal(p.Deploy.Value.Scope, deserialized.Deploy.Value.Scope)
    Assert.Equal(p.Deploy.Value.RouterConfig, deserialized.Deploy.Value.RouterConfig)
    
type InnerUnion = 
    | Inner of int * string

type OuterUnion = 
    | Succeed of string * InnerUnion
    | Fail
    
let testBehavior (mailbox:Actor<_>) msg =
    match msg with
    | Succeed("a-11", Inner(11, "a-12")) -> mailbox.Sender() <! msg
    | _ -> mailbox.Sender() <! Fail  
    |> ignored

[<Fact(Skip="FIXME: hanging out in multi-test runs")>]
let ``can serialize and deserialize discriminated unions over remote nodes`` () =   

    let remoteConfig port = 
        sprintf """
        akka { 
            actor {
                ask-timeout = 10s
                provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"
            }
            remote {
                helios.tcp {
                    port = %i
                    hostname = localhost
                }
            }
        }
        """ port
        |> Configuration.parse

    use server = System.create "server-system" (remoteConfig 9911)
    use client = System.create "client-system" (remoteConfig 0)

    let aref = spawn client "a-1" { (propse <@ actorOf2 testBehavior @> ) with Deploy = Some(Deploy(RemoteScope (Address.Parse "akka.tcp://server-system@localhost:9911"))) }
    let msg = Succeed("a-11", Inner(11, "a-12"))
    let response : OuterUnion = aref <? msg |> Async.RunSynchronously
    response |> equals msg

[<Fact>]
let ``monitor works on typed refs`` () = testDefault <| fun tck ->
    let ref = spawn tck "actor" (props Behaviors.echo)
    monitor tck ref
    ref <! PoisonPill.Instance
    expectTerminated tck ref |> ignore
    
[<Fact>]
let ``demonitor works on typed refs`` () = testDefault <| fun tck ->
    let ref = spawn tck "actor" (props Behaviors.echo)
    monitor tck ref
    demonitor tck ref
    ref <! PoisonPill.Instance
    expectNoMsg tck

type RouterMsg =
    | Inc
    | Fail
    | GetState

type SupervisorMsg = 
    | State of int
    | DirectiveInvoked
    
[<Fact(Skip="FIXME")>]
let ``custom supervisor strategy works with routers`` () = testDefault <| fun tck ->
    let t = typed tck.TestActor
    let router = BroadcastPool(2)
    let strategy = Strategy.OneForOne((fun ex -> 
        t <! DirectiveInvoked
        Directive.Resume), retries = 4)
    let rec a state = 
        function
        | Inc -> become(a (state+1))
        | Fail -> failwith "EXPECTED"
        | GetState -> 
            t <! State state
            become(a state)
    let p = props <| actorOf (a 0)
    let router = spawn tck "router-1" { p with Router = Some (upcast router) ; SupervisionStrategy = Some strategy }
    
    router <! Inc
    router <! Inc
    router <! Inc
    router <! Fail
    router <! GetState
    
    expectMsg tck <| DirectiveInvoked |> ignore
    expectMsg tck <| State 3 |> ignore