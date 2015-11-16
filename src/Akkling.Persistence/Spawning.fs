//-----------------------------------------------------------------------
// <copyright file="Spawning.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akkling
open Microsoft.FSharp.Quotations

module Linq = 
    open System.Linq.Expressions
    open Akkling.Linq
    
    type PersistentExpression = 
        
        static member ToExpression(f : System.Linq.Expressions.Expression<System.Func<FunPersistentActor<'Command>>>) = 
            match f with
            | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]))) -> 
                Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<FunPersistentActor<'Command>>>
            | _ -> failwith "Doesn't match"
        
        static member ToExpression(f : System.Linq.Expressions.Expression<System.Func<FunPersistentView<'Event>>>) = 
            match f with
            | Lambda(_, Invoke(Call(null, Method "ToFSharpFunc", Ar [| Lambda(_, p) |]))) -> 
                Expression.Lambda(p, [||]) :?> System.Linq.Expressions.Expression<System.Func<FunPersistentView<'Event>>>
            | _ -> failwith "Doesn't match"

[<AutoOpen>]
module Spawn =

    /// <summary>
    /// Spawns a persistent actor instance.
    /// </summary>
    /// <param name="actorFactory">Object responsible for actor instantiation.</param>
    /// <param name="pid">Identifies uniquely current actor across different incarnations. It's necessary to identify it's event source.</param>
    /// <param name="fn">Aggregate containing state of the actor, but also an event- and command-handling behavior.</param>
    /// <param name="state">Initial state of an actor.</param>
    /// <param name="options">Additional spawning options.</param>
    let spawnPersiste (options : SpawnOption list) (actorFactory : IActorRefFactory) (pid : PID) (fn : Expr<Eventsourced<'Command> -> Behavior<'Command>>) : IActorRef<'Command> = 
        let e = 
            Linq.PersistentExpression.ToExpression
                (fun () -> new FunPersistentActor<'Command>(fn, pid))
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, pid))

    /// <summary>
    /// Spawns a persistent actor instance.
    /// </summary>
    /// <param name="actorFactory">Object responsible for actor instantiation.</param>
    /// <param name="pid">Identifies uniquely current actor across different incarnations. It's necessary to identify it's event source.</param>
    /// <param name="fn">Aggregate containing state of the actor, but also an event- and command-handling behavior.</param>
    /// <param name="state">Initial state of an actor.</param>
    /// <param name="options">Additional spawning options.</param>
    let spawnPersistOpt (options : SpawnOption list) (actorFactory : IActorRefFactory) (pid : PID) (fn : Eventsourced<'Command> -> Behavior<'Command>) : IActorRef<'Command> = 
        let e = 
            Linq.PersistentExpression.ToExpression
                (fun () -> new FunPersistentActor<'Command>(fn, pid))
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, pid))

    let inline spawnPersist factory pid fn = spawnPersistOpt [] factory pid fn
    
    /// <summary>
    /// Spawns a persistent view instance. Unlike actor's views are readonly versions of statefull, recoverable actors.
    /// </summary>
    /// <param name="actorFactory">Object responsible for actor instantiation.</param>
    /// <param name="pid">Identifies uniquely current actor across different incarnations. It's necessary to identify it's event source.</param>
    /// <param name="viewId">Identifies uniquely current view's state. It's different that event source, since many views with different internal states can relate to single event source.</param>
    /// <param name="options">Additional spawning options.</param>
    let spawnViewe (options : SpawnOption list)  (actorFactory : IActorRefFactory) (pid : PID) (viewId : PID) (f : Expr<View<'Message> -> Behavior<'Message>>): IActorRef<'Message> = 
        let e = Linq.PersistentExpression.ToExpression(fun () -> new FunPersistentView<'Message>(f, pid, viewId))
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, viewId))

    /// <summary>
    /// Spawns a persistent view instance. Unlike actor's views are readonly versions of statefull, recoverable actors.
    /// </summary>
    /// <param name="actorFactory">Object responsible for actor instantiation.</param>
    /// <param name="pid">Identifies uniquely current actor across different incarnations. It's necessary to identify it's event source.</param>
    /// <param name="viewId">Identifies uniquely current view's state. It's different that event source, since many views with different internal states can relate to single event source.</param>
    /// <param name="options">Additional spawning options.</param>
    let spawnViewOpt (options : SpawnOption list)  (actorFactory : IActorRefFactory) (pid : PID) (viewId : PID) (f : View<'Message> -> Behavior<'Message>): IActorRef<'Message> = 
        let e = Linq.PersistentExpression.ToExpression(fun () -> new FunPersistentView<'Message>(f, pid, viewId))
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, viewId))

    let spawnView factory pid viewId fn = spawnViewOpt [] factory pid viewId fn

