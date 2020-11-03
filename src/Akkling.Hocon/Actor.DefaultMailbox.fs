namespace Akkling.Hocon

[<AutoOpen>]
module DefaultMailbox =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type DefaultMailboxBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()        

        /// FQCN of the MailboxType. The Class of the FQCN must have a public
        /// constructor with
        /// (akka.actor.ActorSystem.Settings, com.typesafe.config.Config) parameters.
        [<CustomOperation("mailbox_type");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MailboxType (state: string list, x: string) =
            quotedField "mailbox-type" x::state
        /// FQCN of the MailboxType. The Class of the FQCN must have a public
        /// constructor with
        /// (akka.actor.ActorSystem.Settings, com.typesafe.config.Config) parameters.
        member this.mailbox_type value =
            this.MailboxType([], value)
            |> this.Run
            
        /// If the mailbox is bounded then it uses this setting to determine its
        /// capacity. The provided value must be positive.
        ///
        /// NOTICE:
        /// Up to version 2.1 the mailbox type was determined based on this setting;
        /// this is no longer the case, the type must explicitly be a bounded mailbox.
        [<CustomOperation("mailbox_capacity");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MailboxCapacity (state: string list, value: int) =
            positiveField "mailbox-capacity" value::state
        /// If the mailbox is bounded then it uses this setting to determine its
        /// capacity. The provided value must be positive.
        ///
        /// NOTICE:
        /// Up to version 2.1 the mailbox type was determined based on this setting;
        /// this is no longer the case, the type must explicitly be a bounded mailbox.
        member this.mailbox_capacity value =
            this.MailboxCapacity([], value)
            |> this.Run
            
        /// If the mailbox is bounded then this is the timeout for enqueueing
        /// in case the mailbox is full. Negative values signify infinite
        /// timeout, which should be avoided as it bears the risk of dead-lock.
        [<CustomOperation("mailbox_push_timeout_time");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MailboxPushTimeoutTime (state: string list, value: 'Duration) =
            durationField "mailbox-push-timeout-time" value::state
        /// If the mailbox is bounded then this is the timeout for enqueueing
        /// in case the mailbox is full. Negative values signify infinite
        /// timeout, which should be avoided as it bears the risk of dead-lock.
        member inline this.mailbox_push_timeout_time (value: 'Duration) =
            this.MailboxPushTimeoutTime([], value)
            |> this.Run
            
        /// For Actor with Stash: The default capacity of the stash.
        /// If negative (or zero) then an unbounded stash is used (default)
        /// If positive then a bounded stash is used and the capacity is set using
        /// the property
        [<CustomOperation("stash_capacity");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StashCapacity (state: string list, value: int) =
            numField "stash-capacity" value::state
        /// For Actor with Stash: The default capacity of the stash.
        /// If negative (or zero) then an unbounded stash is used (default)
        /// If positive then a bounded stash is used and the capacity is set using
        /// the property
        member this.stash_capacity value =
            this.StashCapacity([], value)
            |> this.Run

    let default_mailbox = 
        { new DefaultMailboxBuilder<Akka.Actor.Field>() with
            member _.Run (state: string list) =
                objExpr "default-mailbox" 3 state
                |> Akka.Actor.Field }

    let custom_mailbox (name: string) = 
        { new DefaultMailboxBuilder<Akka.Actor.Field>() with
            member _.Run (state: string list) =
                objExpr name 3 state
                |> Akka.Actor.Field }
    