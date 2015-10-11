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
let test (config : Akka.Configuration.Config) (fn : Tck -> unit) : unit = 
    use system = System.create "test-system" config
    use tck = new TestKit(system)
    fn tck

/// <summary>
/// Runs a test case function using default configuration.
/// </summary>
let testDefault = test (Configuration.defaultConfig())

let inline monitor (tck : Tck) (ref : IActorRef<'t>) : unit = tck.Watch ref |> ignore

let expectMsg (tck : Tck) (msg : 't) : 't option = 
    let reply = tck.ExpectMsg<'t>(msg, Nullable(), "")
    if reply <> Unchecked.defaultof<'t> then Some reply
    else None

let inline expectNoMsg (tck: Tck) : unit = tck.ExpectNoMsg ()

let inline expectTerminated (tck : Tck) (ref : IActorRef<'t>) : bool = 
    tck.ExpectTerminated(ref, Nullable(), "").AddressTerminated
