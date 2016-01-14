//-----------------------------------------------------------------------
// <copyright file="Spawning.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Actor
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

[<RequireQualifiedAccess>]
module Configuration = 
    /// Parses provided HOCON string into a valid Akka configuration object.
    let parse = Akka.Configuration.ConfigurationFactory.ParseString
    
    /// Returns default Akka for F# configuration.
    let defaultConfig () = Akka.Configuration.ConfigurationFactory.Default()
    
    /// Loads Akka configuration from the project's .config file.
    let load = Akka.Configuration.ConfigurationFactory.Load

module System = 
    /// Creates an actor system with remote deployment serialization enabled.
    let create (name : string) (config : Akka.Configuration.Config) : ActorSystem = 
        let extConfig = config.WithFallback(Configuration.parse """
            akka.actor {
                serializers {
                    wire = "Akkling.Serialization.WireSerializer, Akkling"
                }
                serialization-bindings {
                  "System.Object" = wire
                }
            }
        """)
        let system = ActorSystem.Create(name, extConfig)
        let exprSerializer = Akkling.Serialization.ExprSerializer(system :?> ExtendedActorSystem)
        system.Serialization.AddSerializer(exprSerializer)
        system.Serialization.AddSerializationMap(typeof<Expr>, exprSerializer)
        system

[<AutoOpen>]
module Spawn = 
    type SpawnOption = 
        | Deploy of Deploy
        | Router of Akka.Routing.RouterConfig
        | SupervisorStrategy of SupervisorStrategy
        | Dispatcher of string
        | Mailbox of string
    
    let rec applySpawnOptions (props : Props) (opt : SpawnOption list) : Props = 
        match opt with
        | [] -> props
        | h :: t -> 
            let p = 
                match h with
                | Deploy d -> props.WithDeploy d
                | Router r -> props.WithRouter r
                | SupervisorStrategy s -> props.WithSupervisorStrategy s
                | Dispatcher d -> props.WithDispatcher d
                | Mailbox m -> props.WithMailbox m
            applySpawnOptions p t
    
    /// <summary>
    /// Spawns an actor using specified actor computation expression, using an Expression AST.
    /// The actor code can be deployed remotely.
    /// </summary>
    /// <param name="actorFactory">Either actor system or parent actor</param>
    /// <param name="name">Name of spawned child actor</param>
    /// <param name="expr">F# expression compiled down to receive function used by actor for response for incoming request</param>
    /// <param name="options">List of options used to configure actor creation</param>
    let spawne (options : SpawnOption list) (actorFactory : IActorRefFactory) (name : string) 
        (expr : Expr<Actor<'Message> -> Behavior<'Message>>) : IActorRef<'Message> = 
        let e = Linq.Expression.ToExpression(fun () -> new FunActor<'Message>(expr))
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, name)) :> IActorRef<'Message>
    
    /// <summary>
    /// Spawns an actor using specified actor computation expression, with custom spawn option settings.
    /// The actor can only be used locally. 
    /// </summary>
    /// <param name="actorFactory">Either actor system or parent actor</param>
    /// <param name="name">Name of spawned child actor</param>
    /// <param name="f">Used by actor for handling response for incoming request</param>
    /// <param name="options">List of options used to configure actor creation</param>
    let spawnOpt (options : SpawnOption list) (actorFactory : IActorRefFactory) (name : string) (f : Actor<'Message> -> Behavior<'Message>) : IActorRef<'Message> = 
        let e = Linq.Expression.ToExpression(fun () -> new FunActor<'Message>(f))
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, name)) :> IActorRef<'Message>
            
    /// <summary>
    /// Spawns an actor using specified actor computation expression.
    /// The actor can only be used locally. 
    /// </summary>
    /// <param name="actorFactory">Either actor system or parent actor</param>
    /// <param name="name">Name of spawned child actor</param>
    /// <param name="f">Used by actor for handling response for incoming request</param>
    let spawn (actorFactory : IActorRefFactory) (name : string) (f : Actor<'Message> -> Behavior<'Message>) : IActorRef<'Message> = 
        spawnOpt [] actorFactory name f 
    
    /// <summary>
    /// Spawns an actor using specified actor quotation, with custom spawn option settings.
    /// The actor can only be used locally. 
    /// </summary>
    /// <param name="actorFactory">Either actor system or parent actor</param>
    /// <param name="name">Name of spawned child actor</param>
    /// <param name="f">Used to create a new instance of the actor</param>
    /// <param name="options">List of options used to configure actor creation</param>
    let spawnObjOpt (actorFactory : IActorRefFactory) (name : string) (f : Quotations.Expr<unit -> #ActorBase>) 
        (options : SpawnOption list) : IActorRef<'Message> = 
        let e = Linq.Expression.ToExpression<'Actor> f
        let props = applySpawnOptions (Props.Create e) options
        typed (actorFactory.ActorOf(props, name)) :> IActorRef<'Message>
    
    /// <summary>
    /// Spawns an actor using specified actor quotation.
    /// The actor can only be used locally. 
    /// </summary>
    /// <param name="actorFactory">Either actor system or parent actor</param>
    /// <param name="name">Name of spawned child actor</param>
    /// <param name="f">Used to create a new instance of the actor</param>
    let spawnObj (actorFactory : IActorRefFactory) (name : string) (f : Quotations.Expr<unit -> #ActorBase>) : IActorRef<'Message> = 
        spawnObjOpt actorFactory name f []
    
    /// <summary>
    /// Wraps provided function with actor behavior. 
    /// It will be invoked each time, an actor will receive a message. 
    /// </summary>
    let actorOf (fn : 'Message -> #Effect) (mailbox : Actor<'Message>) : Behavior<'Message> = 
        let rec loop() = 
            actor { 
                let! msg = mailbox.Receive()
                return fn msg 
            }
        loop()
    
    /// <summary>
    /// Wraps provided function with actor behavior. 
    /// It will be invoked each time, an actor will receive a message. 
    /// </summary>
    let actorOf2 (fn : Actor<'Message> -> 'Message -> #Effect) (mailbox : Actor<'Message>) : Behavior<'Message> = 
        let rec loop() = 
            actor {
                let! msg = mailbox.Receive()
                return fn mailbox msg
            }
        loop()

    /// <summary>
    /// Returns an actor effect causing no changes in message handling pipeline.
    /// </summary>
    let inline ignored (_: 'Any) : Effect = ActorEffect.Ignore :> Effect

    /// <summary>
    /// Returns an actor effect causing messages to become unhandled.
    /// </summary>
    let inline unhandled (_: 'Any) : Effect = ActorEffect.Unhandled :> Effect

    /// <summary>
    /// Returns an actor effect causing actor to stop.
    /// </summary>
    let inline stop (_: 'Any) : Effect = ActorEffect.Stop :> Effect