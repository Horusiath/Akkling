//-----------------------------------------------------------------------
// <copyright file="PersistentView.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling.Persistence

open System
open Akka.Actor
open Akka.Configuration
open Akka.Persistence
open Akkling
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.QuotationEvaluation

[<Sealed>]
type AtLeastOnceDeliverySemantic (semantic: Akka.Persistence.AtLeastOnceDeliverySemantic) =
    /// Full delivery state of the at-least-once delivery, including unconfirmed messages. 
    /// It can be saved as part of the persistence snapshot. 
    member __.DeliverySnapshot 
        with get() = semantic.GetDeliverySnapshot()
        and set(s) = semantic.SetDeliverySnapshot(s)
    /// Maximum number of unconfirmed messages, that this actor is allowed to hold in the memory. 
    /// If this number is exceeded, Delivery will not accept more messages and it will return false.
    /// The default value can be configure with the 'akka.persistence.at-least-once-delivery.max-unconfirmed-messages' configuration key. 
    member __.MaxUnconfirmedMessages = semantic.MaxUnconfirmedMessages
    /// Interval between redelivery attempts. 
    /// The default value can be configure with the 'akka.persistence.at-least-once-delivery.redeliver-interval' configuration key. 
    member __.RedeliverInterval = semantic.RedeliverInterval
    /// Maximum number of unconfirmed messages that will be sent at each redelivery burst 
    /// (burst frequency is half of the redelivery interval). If there's a lot of unconfirmed messages 
    /// (e.g. if the destination is not available for a long time), this helps prevent an overwhelming amount of messages to be sent at once.  
    /// The default value can be configure with the 'akka.persistence.at-least-once-delivery.redelivery-burst-limit' configuration key.
    member __.RedeliveryBurstLimit = semantic.RedeliveryBurstLimit
     /// Number of messages, that have not been confirmed yet.
    member __.UnconfirmedCount = semantic.UnconfirmedCount
    /// After this number of delivery attempts a Akka.Persistence.UnconfirmedWarning message will be sent to self. 
    /// The count is reset after restart. The default value can be configure with the 
    /// 'akka.persistence.at-least-once-delivery.warn-after-number-of-unconfirmed-attempts' configuration key.
    member __.WarnAfterNumberOfUnconfirmedAttempts = semantic.WarnAfterNumberOfUnconfirmedAttempts
    /// Call this method when a message has been confirmed by the destination, or to abort re-sending.
    member __.Confirm(deliveryId: int64) = semantic.ConfirmDelivery(deliveryId)
    member __.Deliver<'Message>(destination: ActorPath, deliveryMapper: int64 -> 'Message, ?isRecovering:bool) : bool =
        try
            let recovering = defaultArg isRecovering false
            semantic.Deliver(destination, System.Func<_,_>(fun x -> upcast deliveryMapper x), recovering)
            true
        with
        | :? MaxUnconfirmedMessagesExceededException -> false
    /// Partial behavior responsible for handling the at-least-once-delivery semantics messages.
    member __.Receive : Receive<obj, 'Context> =
        fun (ctx: 'Context) (msg: obj) ->
            match msg with
            | LifecycleEvent e ->
                match e with
                | PreRestart(error, msg) -> semantic.Cancel()
                | PostStop -> semantic.Cancel()
                | _ -> ()
                upcast Unhandled
            | :? PersistentLifecycleEvent as pe ->
                match pe with
                | ReplaySucceed -> semantic.OnReplaySuccess()
                | _ -> ()
                upcast Unhandled
            | other ->
                if semantic.AroundReceive(Unchecked.defaultof<Receive>, other)
                then upcast Ignore
                else upcast Unhandled        
    interface IDisposable with
        member __.Dispose() = semantic.Cancel()

[<RequireQualifiedAccess>]
module AtLeastOnceDelivery =

    /// Creates an at-least-once delivery settings from provided configuration.
    let inline parseConfig (conf: Config) = PersistenceSettings.AtLeastOnceDeliverySettings(conf)
    
    /// Creates an at-least-once delivery semantics object, that can be embedded into actor's behavior.
    let create (settings: PersistenceSettings.AtLeastOnceDeliverySettings) (ctx: #Actor<_>) : AtLeastOnceDeliverySemantic = 
        let semantic = Akka.Persistence.AtLeastOnceDeliverySemantic(ctx.UntypedContext, settings)
        new AtLeastOnceDeliverySemantic(semantic)

    /// Creates an at-least-once delivery semantics object with default settings.
    let createDefault (ctx: #Actor<_>) = 
        let settings = PersistenceSettings.AtLeastOnceDeliverySettings (ctx.System.Settings.Config.GetConfig "akka.persistence")
        create settings ctx