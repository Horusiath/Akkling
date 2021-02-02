namespace Akkling.Hocon

open System
open Akka.Actor
open Akka.Actor

[<AutoOpen>]
module Actor =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    [<AbstractClass>]
    type ActorBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        let indent = 2
        let pathObjExpr = pathObjExpr Akka.Field "actor" indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Shared.Field<Akka.Shared.Debug.Field>) = [(x 3).ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Shared.Field<Akka.Actor.Field>) = [(x indent).ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.Dispatcher.Field) = [x.ToString()]

        /// Default timeout for IActorRef.Ask.
        [<CustomOperation("ask_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.AskTimeout (state: string list, value: 'Duration) =
            durationField "ask-timeout" value::state
        member _.AskTimeout (state: string list, value: Hocon.Infinite) =
            field "ask-timeout" (value.ToString())::state

        /// Default timeout for IActorRef.Ask.
        member inline this.ask_timeout (value: 'Duration) =
            this.AskTimeout([], value)
            |> this.Run
        /// Default timeout for IActorRef.Ask.
        member this.ask_timeout (value: Hocon.Infinite) =
            this.AskTimeout([], value)
            |> this.Run

        /// Timeout for ActorSystem.actorOf
        [<CustomOperation("creation_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.CreationTimeout (state: string list, value: 'Duration) =
            durationField "creation-timeout" value::state
        /// Timeout for ActorSystem.actorOf
        member inline this.creation_timeout (value: 'Duration) =
            this.CreationTimeout([], value)
            |> this.Run

        /// FQCN of the ActorRefProvider to be used; the below is the built-in default,
        /// another one is akka.remote.RemoteActorRefProvider in the akka-remote bundle.
        [<CustomOperation("guardian_supervisor_strategy");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.GuardianSupervisorStrategy (state: string list, x: Type) =
            if not (typeof<SupervisorStrategyConfigurator>.IsAssignableFrom x) then
                failwithf "guardian_supervisor_strategy class must inherit from '%s'" (fqcn typeof<SupervisorStrategyConfigurator>)
            quotedField "guardian-supervisor-strategy" (fqcn x)::state
        /// FQCN of the ActorRefProvider to be used; the below is the built-in default,
        /// another one is akka.remote.RemoteActorRefProvider in the akka-remote bundle.
        member this.guardian_supervisor_strategy value =
            this.GuardianSupervisorStrategy([], value)
            |> this.Run

        /// FQCN of the ActorRefProvider to be used; the below is the built-in default,
        /// another one is akka.remote.RemoteActorRefProvider in the akka-remote bundle.
        [<CustomOperation("provider");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Provider (state: string list, x: Type) =
            if not (typeof<IActorRefProvider>.IsAssignableFrom x) then
                failwithf "actor.provider type must implement '%s'" (fqcn typeof<IActorRefProvider>)
            quotedField "provider" (fqcn x)::state
        /// FQCN of the ActorRefProvider to be used; the below is the built-in default,
        /// another one is akka.remote.RemoteActorRefProvider in the akka-remote bundle.
        member this.provider value =
            this.Provider([], value)
            |> this.Run

        /// Frequency with which stopping actors are prodded in case they had to be
        /// removed from their parents.
        [<CustomOperation("reaper_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ReaperInterval (state: string list, value: int) =
            positiveField "reaper-interval" value::state
        /// Frequency with which stopping actors are prodded in case they had to be
        /// removed from their parents.
        member this.reaper_interval value =
            this.ReaperInterval([], value)
            |> this.Run
            
        /// Serializes and deserializes creators (in Props) to ensure that they can be
        /// sent over the network, this is only intended for testing. Purely local deployments
        /// as marked with deploy.scope == LocalScope are exempt from verification.
        [<CustomOperation("serialize_messages");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SerializeMessages (state: string list, value: bool) = 
            switchField "serialize-messages" value::state
        /// Serializes and deserializes creators (in Props) to ensure that they can be
        /// sent over the network, this is only intended for testing. Purely local deployments
        /// as marked with deploy.scope == LocalScope are exempt from verification.
        member this.serialize_messages value =
            this.SerializeMessages([], value)
            |> this.Run

        /// Serializes and deserializes (non-primitive) messages to ensure immutability,
        /// this is only intended for testing.
        [<CustomOperation("serialize_creators");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SerializeCreators (state: string list, value: bool) = 
            switchField "serialize-creators" value::state
        /// Serializes and deserializes (non-primitive) messages to ensure immutability,
        /// this is only intended for testing.
        member this.serialize_creators value =
            this.SerializeCreators([], value)
            |> this.Run

        /// Class to Serializer binding. You only need to specify the name of an
        /// interface or abstract base class of the messages. In case of ambiguity it
        /// is using the most specific configured class, or giving a warning and
        /// choosing the “first” one.
        /// 
        /// To disable one of the default serializers, assign its class to "none", like
        /// "java.io.Serializable" = none
        [<CustomOperation("serialization_bindings");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SerializationBindings (state: string list, xs: (System.Type * string) list) =
            stringyObjExpr "serialization-bindings" 4 (xs |> List.map (fun (t,v) -> t.FullName,v))::state
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SerializationBindings (state: string list, xs: (string * string) list) =
            stringyObjExpr "serialization-bindings" 4 xs::state
        /// Class to Serializer binding. You only need to specify the name of an
        /// interface or abstract base class of the messages. In case of ambiguity it
        /// is using the most specific configured class, or giving a warning and
        /// choosing the “first” one.
        /// 
        /// To disable one of the default serializers, assign its class to "none", like
        /// "java.io.Serializable" = none
        member this.serialization_bindings (xs: (string * string) list) =
            this.SerializationBindings([], xs)
            |> this.Run
        /// Class to Serializer binding. You only need to specify the name of an
        /// interface or abstract base class of the messages. In case of ambiguity it
        /// is using the most specific configured class, or giving a warning and
        /// choosing the “first” one.
        /// 
        /// To disable one of the default serializers, assign its class to "none", like
        /// "java.io.Serializable" = none
        member this.serialization_bindings (xs: (System.Type * string) list) =
            this.SerializationBindings([], xs)
            |> this.Run

        /// Configuration namespace of serialization identifiers.
        /// Each serializer implementation must have an entry in the following format:
        /// `akka.actor.serialization-identifiers."FQCN" = ID`
        /// where `FQCN` is fully qualified class name of the serializer implementation
        /// and `ID` is globally unique serializer identifier number.
        ///
        /// Identifier values from 0 to 40 are reserved for Akka internal usage.
        [<CustomOperation("serialization_identifiers");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SerializationIdentifiers (state: string list, xs: (Type * int) list) =
            stringyObjExpr "serialization-identifiers" 4 (xs |> List.map (fun (t,v) -> fqcn t,string v))::state
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SerializationIdentifiers (state: string list, xs: (string * string) list) =
            stringyObjExpr "serialization-identifiers" 4 xs::state
        /// Configuration namespace of serialization identifiers.
        /// Each serializer implementation must have an entry in the following format:
        /// `akka.actor.serialization-identifiers."FQCN" = ID`
        /// where `FQCN` is fully qualified class name of the serializer implementation
        /// and `ID` is globally unique serializer identifier number.
        ///
        /// Identifier values from 0 to 40 are reserved for Akka internal usage.
        member this.serialization_identifiers (xs: (Type * int) list) =
            this.SerializationIdentifiers([], xs)
            |> this.Run
        /// Configuration namespace of serialization identifiers.
        /// Each serializer implementation must have an entry in the following format:
        /// `akka.actor.serialization-identifiers."FQCN" = ID`
        /// where `FQCN` is fully qualified class name of the serializer implementation
        /// and `ID` is globally unique serializer identifier number.
        ///
        /// Identifier values from 0 to 40 are reserved for Akka internal usage.
        member this.serialization_identifiers (xs: (string * string) list) =
            this.SerializationIdentifiers([], xs)
            |> this.Run

        /// Timeout for send operations to top-level actors which are in the process
        /// of being started. This is only relevant if using a bounded mailbox or the
        /// CallingThreadDispatcher for a top-level actor.
        [<CustomOperation("unstarted_push_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.UnstartedPushTimeout (state: string list, value: 'Duration) =
            durationField "unstarted-push-timeout" value::state
        /// Timeout for send operations to top-level actors which are in the process
        /// of being started. This is only relevant if using a bounded mailbox or the
        /// CallingThreadDispatcher for a top-level actor.
        member inline this.unstarted_push_timeout (value: 'Duration) =
            this.UnstartedPushTimeout([], value)
            |> this.Run

        // Pathing

        member _.typed =
            { new TypedBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "typed" state }

        member _.deployment = 
            { new DeploymentBuilder<Akka.Field>(Akka.Field, "actor.deployment", indent) with
                member _.Run (state: string list) = pathObjExpr "deployment" state }

        member _.router = 
            { new RouterBuilder<Akka.Field>(Akka.Field, "actor.router", indent) with
                member _.Run (state: string list) = pathObjExpr "router" state }

        member _.custom_dispatcher (name: string) =
            let path = sprintf "actor.%s" name

            { new DispatcherBuilder<Akka.Field>(name, Akka.Field, path, indent) with
                member _.Run (state: string list) = pathObjExpr name state }

        member this.backoff_remote_dispatcher = this.custom_dispatcher "backoff-remote-dispatcher"
        member this.calling_thread_dispatcher = this.custom_dispatcher "calling-thread-dispatcher"
        member this.default_dispatcher = this.custom_dispatcher "default-dispatcher"
        member this.dispatcher = this.custom_dispatcher "dispatcher"
        member this.default_blocking_io_dispatcher = this.custom_dispatcher "default-blocking-io-dispatcher"
        member this.default_fork_join_dispatcher = this.custom_dispatcher "default-fork-join-dispatcher"
        member this.default_plugin_dispatcher = this.custom_dispatcher "default-plugin-dispatcher"
        member this.default_remote_dispatcher = this.custom_dispatcher "default-remote-dispatcher"
        member this.default_replay_dispatcher = this.custom_dispatcher "default-replay-dispatcher"
        member this.default_stream_dispatcher = this.custom_dispatcher "default-stream-dispatcher"

        /// Default separate internal dispatcher to run Akka internal tasks and actors on
        /// protecting them against starvation because of accidental blocking in user actors (which run on the
        /// default dispatcher
        member this.internal_dispatcher = this.custom_dispatcher "internal-dispatcher"

        /// Used for GUI applications
        member this.synchronized_dispatcher = this.custom_dispatcher "synchronized-dispatcher"
        member this.task_dispatcher = this.custom_dispatcher "task-dispatcher"

        member _.default_mailbox = 
            { new DefaultMailboxBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "default-mailbox" state }

        member _.custom_mailbox (name: string) = 
            { new DefaultMailboxBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr name state }

        member _.mailbox = 
            { new MailboxBuilder<Akka.Field>(Akka.Field, "actor.mailbox", indent) with
                member _.Run (state: string list) = pathObjExpr "mailbox" state }

        member _.serializers = 
            { new SerializersBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "serializers" state }

        member _.serialization_settings = 
            { new BaseBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "serialization-settings" state }

        member _.debug =            
            { new DebugBuilder<Akka.Field>() with
                member _.Run (state: SharedInternal.State<Debug.Target>) : Akka.Shared.Field<Akka.Field> =
                    fun _ -> pathObjExpr "debug" state.Fields }

    let actor = 
        { new ActorBuilder<Akka.Field>() with
            member _.Run (state: string list) = 
                objExpr "actor" 2 state
                |> Akka.Field }
