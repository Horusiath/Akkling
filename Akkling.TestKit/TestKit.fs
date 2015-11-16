//-----------------------------------------------------------------------
// <copyright file="TestKit.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.TestKit

open System
open Akkling
open Akka.TestKit
open Akka.TestKit.Xunit2

type Tck = TestKit

/// <summary>
/// Runs a test case function in context of TestKit aware actor system.
/// </summary>
/// <param name="config">Configuration used for actor system initialization.</param>
/// <param name="fn">Test case function</param>
let test (config : Akka.Configuration.Config) (fn : Tck -> unit) = 
    use system = System.create "test-system" config
    use tck = new TestKit(system)
    fn tck

/// <summary>
/// Runs a test case function using default configuration.
/// </summary>
let testDefault t = test (Configuration.defaultConfig()) t

let inline probe (tck: Tck) : TestProbe = tck.CreateTestProbe()
let inline barrier (tck: Tck) (count: int) : TestBarrier = tck.CreateTestBarrier count
let inline latch (tck: Tck) (count: int) : TestLatch = tck.CreateTestLatch count
let inline testActor (tck: Tck) (name: string) : IActorRef<'t> = typed <| tck.CreateTestActor name
let inline monitor (tck : Tck) (ref : IActorRef<'t>) : unit = tck.Watch ref |> ignore
let inline demonitor (tck: Tck) (ref: IActorRef<'t>) : unit = tck.Unwatch ref |> ignore

let expectMsg (tck : Tck) (msg : 't) : 't option = 
    let reply = tck.ExpectMsg<'t>(msg, Nullable(), "")
    if reply <> Unchecked.defaultof<'t> then Some reply
    else None

let expectMsgWithin (tck : Tck) (timeout: TimeSpan) (msg : 't) : 't option = 
    let reply = tck.ExpectMsg<'t>(msg, Nullable(timeout), "")
    if reply <> Unchecked.defaultof<'t> then Some reply
    else None

let expectMsgFilter (tck: Tck) (predicate: 't->bool) : 't option =
    let reply = tck.ExpectMsg<'t>(Predicate<'t>(predicate))
    if reply <> Unchecked.defaultof<'t> then Some reply
    else None
    
let expectMsgFilterWithin (tck: Tck) (timeout: TimeSpan) (predicate: 't->bool) : 't option =
    let reply = tck.ExpectMsg<'t>(Predicate<'t>(predicate), Nullable(timeout))
    if reply <> Unchecked.defaultof<'t> then Some reply
    else None

let inline expectNoMsg (tck : Tck) : unit = tck.ExpectNoMsg()
let inline expectNoMsgWithin (tck: Tck) (timeout: TimeSpan) : unit = tck.ExpectNoMsg timeout

let inline expectMsgAllOf (tck : Tck) (messages : 't seq) : unit = 
    Array.ofSeq messages
    |> tck.ExpectMsgAllOf
    |> ignore

let inline expectMsgAnyOf (tck : Tck) (messages : 't seq) : unit = tck.ExpectMsgAnyOf(Array.ofSeq messages) |> ignore
let inline expectTerminated (tck : Tck) (ref : IActorRef<'t>) : bool =
    tck.ExpectTerminated(ref, Nullable(), "").AddressTerminated
let inline expectTerminatedWithin (timeout : TimeSpan) (tck : Tck) (ref : IActorRef<'t>) : bool = 
    tck.ExpectTerminated(ref, Nullable(timeout), "").AddressTerminated

let inline ignoreMessages (tck: Tck) (predicate: obj -> bool) : unit = tck.IgnoreMessages(Func<obj, bool>(predicate))
let inline receiveN (tck: Tck) (n: int) : obj seq = tck.ReceiveN(n) :> System.Collections.Generic.IEnumerable<obj>
let inline receiveOne (tck: Tck) : 't = tck.ReceiveOne() :?> 't
let inline receiveOneWithin (tck: Tck) (timeout: TimeSpan) : 't = tck.ReceiveOne(Nullable(timeout)) :?> 't
let inline within (tck: Tck) (max: TimeSpan) (fn: unit->'t) : 't = tck.Within(max, Func<'t>(fn))

let inline deadLettersEvents<'t> (tck: Tck) : IEventFilterApplier = tck.EventFilter.DeadLetter(typeof<'t>)
let inline exnEvents<'t when 't :> exn> (tck: Tck) : IEventFilterApplier = tck.EventFilter.Exception(typeof<'t>)
let inline errorEvents (tck: Tck) : IEventFilterApplier = tck.EventFilter.Error()
let inline warningEvents (tck: Tck) : IEventFilterApplier = tck.EventFilter.Warning()
let inline infoEvents (tck: Tck) : IEventFilterApplier = tck.EventFilter.Info()
let inline debugEvents (tck: Tck) : IEventFilterApplier = tck.EventFilter.Debug()

let inline expectEvent (n:int) (applier: IEventFilterApplier) (fn: unit->'t): 't = applier.Expect(n, Func<'t>(fn))
let inline expectEventWithin (timeout: TimeSpan) (n:int) (applier: IEventFilterApplier) (fn: unit->'t): 't = applier.Expect(n, timeout, Func<'t>(fn))

