﻿//-----------------------------------------------------------------------
// <copyright file="FaultHandling.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Actor
open Akka.Util
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

type ExprDecider(expr : Expr<exn -> Directive>) = 

    member __.Expr = expr
    member private this.Compiled = lazy this.Expr.Compile () ()
    
    interface IDecider with
        member this.Decide(e : exn) : Directive = this.Compiled.Value(e)

type Strategy = 
    
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider : exn -> Directive) : SupervisorStrategy = 
        upcast OneForOneStrategy(System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider : exn -> Directive, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast OneForOneStrategy
                   (Option.toNullable retries, Option.toNullable timeout, System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable only to child actor which faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member OneForOne(decider : Expr<exn -> Directive>, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast OneForOneStrategy
                   (Option.toNullable retries, Option.toNullable timeout, ExprDecider decider)
    
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider : exn -> Directive) : SupervisorStrategy = 
        upcast AllForOneStrategy(System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider : exn -> Directive, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast AllForOneStrategy
                   (Option.toNullable retries, Option.toNullable timeout, System.Func<_, _>(decider))
    
    /// <summary>
    /// Returns a supervisor strategy appliable to each supervised actor when any of them had faulted during execution.
    /// </summary>
    /// <param name="retries">Defines a number of times, an actor could be restarted. If it's a negative value, there is not limit.</param>
    /// <param name="timeout">Defines time window for number of retries to occur.</param>
    /// <param name="decider">Used to determine a actor behavior response depending on exception occurred.</param>
    static member AllForOne(decider : Expr<exn -> Directive>, ?retries : int, ?timeout : TimeSpan) : SupervisorStrategy = 
        upcast AllForOneStrategy
                   (Option.toNullable retries, Option.toNullable timeout, ExprDecider decider)
