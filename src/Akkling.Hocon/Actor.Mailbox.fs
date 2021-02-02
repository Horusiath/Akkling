namespace Akkling.Hocon

open System

[<AutoOpen>]
module Mailbox =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type MailboxBasedBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()

        /// FQCN of the MailboxType. The Class of the FQCN must have a public
        /// constructor with
        /// (akka.actor.ActorSystem.Settings, com.typesafe.config.Config) parameters.
        [<CustomOperation("mailbox_type");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MailboxType (state: string list, x: Type) =
            quotedField "mailbox-type" (fqcn x)::state
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
            
    let private createMailbox name =
        { new MailboxBasedBuilder<Akka.Actor.Mailbox.Field>() with
            member _.Run (state: string list) =
                objExpr name 4 state
                |> Akka.Actor.Mailbox.Field }

    let bounded_deque_based = createMailbox "bounded-deque-based"
    let bounded_queue_based = createMailbox "bounded-queue-based"

    /// The LoggerMailbox will drain all messages in the mailbox
    /// when the system is shutdown and deliver them to the StandardOutLogger.
    ///
    /// Do not change this unless you know what you are doing.
    let logger_queue = createMailbox "logger-queue"
    let unbounded_deque_based = createMailbox "unbounded-deque-based"
    let unbounded_queue_based = createMailbox "unbounded-queue-based"
    
    [<AbstractClass>]
    type MailboxBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.Mailbox.Field) = [x.ToString()]

        [<CustomOperation("requirements");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Requirements (state: string list, xs: (string * string) list) =
            stringyObjExpr "requirements" 4 xs::state
        member this.requirements value =
            this.Requirements([], value)
            |> this.Run

        // Pathing

        member private _.createMailbox name =
            { new MailboxBasedBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr name state }

        member this.bounded_deque_based = this.createMailbox "bounded-deque-based"
        member this.bounded_queue_based = this.createMailbox "bounded-queue-based"

        /// The LoggerMailbox will drain all messages in the mailbox
        /// when the system is shutdown and deliver them to the StandardOutLogger.
        ///
        /// Do not change this unless you know what you are doing.
        member this.logger_queue = this.createMailbox "logger-queue"
        member this.unbounded_deque_based = this.createMailbox "unbounded-deque-based"
        member this.unbounded_queue_based = this.createMailbox "unbounded-queue-based"

    let mailbox =
        let path = "mailbox"
        let indent = 3

        { new MailboxBuilder<Akka.Actor.Field>(Akka.Actor.Field, path, indent) with
            member _.Run (state: string list) =
                objExpr path indent state
                |> Akka.Actor.Field }
