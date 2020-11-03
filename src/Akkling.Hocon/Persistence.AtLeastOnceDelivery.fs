namespace Akkling.Hocon

[<AutoOpen>]
module AtLeastOnceDelivery =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    [<AbstractClass>]
    type AtLeastOnceDeliveryBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
            
        /// Interval between re-delivery attempts.
        [<CustomOperation("redeliver_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RedeliverInterval (state: string list, value: 'Duration) =
            durationField "redeliver-interval" value::state
        /// Interval between re-delivery attempts.
        member inline this.redeliver_interval (value: 'Duration) =
            this.RedeliverInterval([], value)
            |> this.Run
        
        /// Maximum number of unconfirmed messages that will be sent in one
        /// re-delivery burst.
        [<CustomOperation("redelivery_burst_limit");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RedeliveryBurstLimit (state: string list, value: int) =
            positiveField "redelivery-burst-limit" value::state
        /// Maximum number of unconfirmed messages that will be sent in one
        /// re-delivery burst.
        member this.redelivery_burst_limit value =
            this.RedeliveryBurstLimit([], value)
            |> this.Run
        
        /// After this number of delivery attempts a
        /// `ReliableRedelivery.UnconfirmedWarning`, message will be sent to the actor.
        [<CustomOperation("warn_after_number_of_unconfirmed_attempts");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.WarnAfterNumberOfUnconfirmedAttempts (state: string list, value: int) =
            positiveField "warn-after-number-of-unconfirmed-attempts" value::state
        /// After this number of delivery attempts a
        /// `ReliableRedelivery.UnconfirmedWarning`, message will be sent to the actor.
        member this.warn_after_number_of_unconfirmed_attempts value =
            this.WarnAfterNumberOfUnconfirmedAttempts([], value)
            |> this.Run
        
        /// Maximum number of unconfirmed messages that an actor with
        /// AtLeastOnceDelivery is allowed to hold in memory.
        [<CustomOperation("max_unconfirmed_messages");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxUnconfirmedMessages (state: string list, value: int) =
            positiveField "max-unconfirmed-messages" value::state
        /// Maximum number of unconfirmed messages that an actor with
        /// AtLeastOnceDelivery is allowed to hold in memory.
        member this.max_unconfirmed_messages value =
            this.MaxUnconfirmedMessages([], value)
            |> this.Run
    
    /// Reliable delivery settings.
    let at_least_once_delivery =
        { new AtLeastOnceDeliveryBuilder<Akka.Persistence.Field>() with
            member _.Run (state: string list) = 
                objExpr "at-least-once-delivery" 3 state
                |> Akka.Persistence.Field }
