﻿//-----------------------------------------------------------------------
// <copyright file="Spawning.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Props

open System
open Akka.Actor
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

let internal exprSerializer = Nessos.FsPickler.FsPickler.CreateBinarySerializer()

/// <summary>
/// Typed props are descriptors of how particular actor should be instantiated.
/// </summary>
type Props<'Message> = 
    { 
      /// <summary>
      /// INTERNAL API.
      /// </summary>
      ActorType: Type

      /// <summary>
      /// INTERNAL API.
      /// </summary>
      Receiver: obj

      /// <summary>
      /// Config key to dispatcher responsible for managing current actor's threading.
      /// </summary>
      Dispatcher: string option

      /// <summary>
      /// Config key to mailbox type used by current actor.
      /// </summary>
      Mailbox: string option

      /// <summary>
      /// Deploy settings of the current actor.
      /// </summary>
      Deploy: Deploy option

      /// <summary>
      /// Router settings in case when current actor is used in router configuration.
      /// </summary>
      Router: Akka.Routing.RouterConfig option

      /// <summary>
      /// Custom supervision strategy used by current actor.
      /// </summary>
      SupervisionStrategy: SupervisorStrategy option } 

    member this.ToProps () : Akka.Actor.Props = this.ToProps true
    member internal this.ToProps (withReceiver: bool) : Akka.Actor.Props = 
        let mutable p = if withReceiver then Props.Create(this.ActorType, [| this.Receiver |]) else Props.Create(this.ActorType)
        p <- match this.Dispatcher with
                | Some dispatcher -> p.WithDispatcher dispatcher
                | _ -> p
        p <- match this.Mailbox with
                | Some mailbox -> p.WithMailbox mailbox
                | _ -> p
        p <- match this.Deploy with
                | Some deploy -> p.WithDeploy deploy
                | _ -> p
        p <- match this.Router with
                | Some router -> p.WithRouter router
                | _ -> p
        p <- match this.SupervisionStrategy with
                | Some supervisionStrategy -> p.WithSupervisorStrategy supervisionStrategy
                | _ -> p
        p

    static member Create<'Actor, 'Context, 'Message when 'Actor :> ActorBase>(receive: 'Context -> Behavior<'Message>) : Props<'Message> = 
        { ActorType = typeof<'Actor>
          Receiver = receive
          Dispatcher = None
          Mailbox = None
          Deploy = None
          Router = None
          SupervisionStrategy = None }

    static member Create<'Actor, 'Context, 'Message when 'Actor :> ActorBase>(expr: Expr<('Context -> Behavior<'Message>)>) : Props<'Message> = 
        { ActorType = typeof<'Actor>
          Receiver = expr
          Dispatcher = None
          Mailbox = None
          Deploy = None
          Router = None
          SupervisionStrategy = None }

    static member From(props: Props) : Props<'Message> =
        { ActorType = props.Type
          Receiver = props.Arguments.[0]
          Deploy = Some props.Deploy
          Dispatcher = if props.Dispatcher = Deploy.NoDispatcherGiven then None else Some props.Dispatcher
          Mailbox = if props.Mailbox = Deploy.NoMailboxGiven then None else Some props.Mailbox
          Router =  if props.RouterConfig = Akka.Routing.RouterConfig.NoRouter then None else Some props.RouterConfig
          SupervisionStrategy = if props.SupervisorStrategy = null then None else Some props.SupervisorStrategy
        }

    static member internal From(props: Props, receiver: obj) : Props<'Message> =
        { ActorType = props.Type
          Receiver = receiver
          Deploy = Some props.Deploy
          Dispatcher = if props.Dispatcher = Deploy.NoDispatcherGiven then None else Some props.Dispatcher
          Mailbox = if props.Mailbox = Deploy.NoMailboxGiven then None else Some props.Mailbox
          Router =  if props.RouterConfig = Akka.Routing.RouterConfig.NoRouter then None else Some props.RouterConfig
          SupervisionStrategy = if props.SupervisorStrategy = null then None else Some props.SupervisorStrategy
        }

    interface Akka.Util.ISurrogated with
        member this.ToSurrogate _ =
            let props = this.ToProps false
            let surrogate: PropsSurrogate<'Message> = { Wrapped = props; ReceiverBytes = exprSerializer.Pickle(this.Receiver) } 
            surrogate :> Akka.Util.ISurrogate

and PropsSurrogate<'Message> = 
    { Wrapped: Props; ReceiverBytes: byte array }
    interface Akka.Util.ISurrogate with
        member this.FromSurrogate _ = Props<'Message>.From (this.Wrapped, exprSerializer.UnPickle (this.ReceiverBytes)) :> Akka.Util.ISurrogated

/// <summary>
/// Creates a props describing a way to incarnate actor with behavior described by <paramref name="receive"/> function.
/// </summary>
let inline props (receive: Actor<'Message>->Behavior<'Message>) : Props<'Message> = 
    Props<'Message>.Create<FunActor<'Message>, Actor<'Message>, 'Message>(receive)

/// <summary>
/// Creates a props describing a way to incarnate actor with behavior described by <paramref name="expr"/> expression.
/// </summary>
let inline propse (expr: Expr<(Actor<'Message> -> Behavior<'Message>)>) : Props<'Message> =
    Props<'Message>.Create<FunActor<'Message>, Actor<'Message>, 'Message>(expr)