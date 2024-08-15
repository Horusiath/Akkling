//-----------------------------------------------------------------------
// <copyright file="FaultHandling.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Actor
open Akka.Util
open System

type Strategy =

    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider: exn -> Directive) : SupervisorStrategy =
        upcast OneForOneStrategy(System.Func<_, _>(decider))

    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider: exn -> Directive, ?retries: int, ?timeout: TimeSpan) : SupervisorStrategy =
        upcast OneForOneStrategy(Option.toNullable retries, Option.toNullable timeout, System.Func<_, _>(decider))

    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider: exn -> Directive) : SupervisorStrategy =
        upcast AllForOneStrategy(System.Func<_, _>(decider))

    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider: exn -> Directive, ?retries: int, ?timeout: TimeSpan) : SupervisorStrategy =
        upcast AllForOneStrategy(Option.toNullable retries, Option.toNullable timeout, System.Func<_, _>(decider))
