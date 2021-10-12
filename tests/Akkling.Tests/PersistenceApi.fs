﻿//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.PersistenceApi

open Akkling
open Akkling.Persistence
open Akkling.TestKit
open Akka.Actor
open System
open System.Threading
open Xunit

type Request = 
    | Action of int64 * string
    | ReqAck

type SenderMsg =
    | Req of string    
    | ActionAck of int64

let config = Configuration.parse """
akka.loglevel = DEBUG
akka.persistence.at-least-once-delivery.redeliver-interval = 1s
akka.scheduler.implementation = "Akka.Actor.HashedWheelTimerScheduler, Akka"
"""
let unreliable dropMod target (ctx: Actor<_>) =
    let rec loop count = actor {
        let! msg = ctx.Receive()
        if (count + 1) % dropMod <> 0 then target <<! msg
        return! loop (count + 1) }
    loop 0

let destination target (ctx: Actor<_>) =
    let rec loop received = actor {
        let! msg = ctx.Receive()
        match msg with
        | Action(id,_) when Set.contains id received ->
            ctx.Sender() <! ActionAck id
            return! loop received
        | Action(id,_) -> 
            target <! msg
            return! loop (Set.add id received)
    }
    loop Set.empty

let sender destinations (ctx: Actor<obj>) =
    let alod = AtLeastOnceDelivery.createDefault ctx
    let rec loop () = actor {
        let! msg = ctx.Receive()
        match msg with
        | :? SenderMsg as snd ->
            match snd with
            | Req payload -> 
                let dest = Map.find (payload.[0]) destinations
                alod.Deliver(ActorPath.Parse dest, fun id -> Action(id, payload)) |> ignore
                ctx.Sender() <! ReqAck
            | ActionAck id ->
                alod.Confirm id |> ignore
        | other -> alod.Receive ctx msg |> ignore
        return! loop () }
    loop ()

[<Fact>]
let ``at-least-once delivery semantics should redeliver messages`` () = test config <| fun tck ->
    Akka.Persistence.Persistence.Instance.Apply(tck.Sys) |> ignore
    let probe = tck.CreateTestProbe()
    let dest = spawn tck "destination" <| props(destination (typed probe.Ref))
    let destinations = Map.ofList [ 'a', (spawn tck "unreliable" <| props(unreliable 3 dest)).Path.ToString() ]
    let snd = retype (spawn tck "sender" <| props(sender destinations))

    snd <! Req "a-1"
    expectMsg tck ReqAck |> ignore
    probe.ExpectMsg (Action(1L, "a-1")) |> ignore
    
    snd <! Req "a-2"
    expectMsg tck ReqAck |> ignore
    probe.ExpectMsg (Action(2L, "a-2")) |> ignore
        
    snd <! Req "a-3"
    snd <! Req "a-4"
    expectMsg tck ReqAck |> ignore
    expectMsg tck ReqAck |> ignore
    
    // a-3 was lost ...
    probe.ExpectMsg (Action(4L, "a-4")) |> ignore
    // ... and then redelivered
    probe.ExpectMsg (Action(3L, "a-3")) |> ignore
    expectNoMsgWithin tck (TimeSpan.FromSeconds 1.)

[<Fact>]
let ``PersistenceLifecycleEvent should be fired``() = testDefault <| fun tck ->
    let pref = spawn tck "p-1" <| propsPersist (fun ctx ->
        let rec loop () = actor {
            let! (msg: obj) = ctx.Receive()
            match msg with
            | :? PersistentLifecycleEvent as e -> 
                typed tck.TestActor <! e
                return! loop ()
            | _ -> return Unhandled }
        loop ())
    expectMsg tck ReplaySucceed |> ignore

[<Fact>]
let ``Effects.andThen should be called after event was persisted``() = testDefault <| fun tck ->
    let q = typed tck.TestActor
    let pref = spawn tck "p-1" <| propsPersist (fun ctx -> 
        let rec loop () = actor {
            let! (msg: string) = ctx.Receive()
            match msg with
            | "cmd" ->
                return Persist "evt"
                       |> Effects.andThen (fun _ -> q <! "andThen1" )
                       |> Effects.andThen (fun _ -> q <! "andThen2" )
            | "evt" ->
                q <! "evt"
                return Ignore
            | _ -> return Unhandled }
        loop ())
    pref <! "cmd"
    expectMsg tck "evt" |> ignore
    expectMsg tck "andThen1" |> ignore
    expectMsg tck "andThen2" |> ignore
    