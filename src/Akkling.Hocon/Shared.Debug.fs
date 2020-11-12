namespace Akkling.Hocon

[<AutoOpen>]
module Debug =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    module Debug =
        [<RequireQualifiedAccess>]
        type Target =
            | Any
            | Actor
            | Cluster
            | Materializer
        
        [<RequireQualifiedAccess>]
        module Target =
            let merge (target1: Target) (target2: Target) =
                match target1, target2 with
                | Target.Any, _
                | _, Target.Any -> Some target1
                | _, _ when target1 = target2 -> Some target1
                | _ -> None

        [<RequireQualifiedAccess>]
        module State =
            let create = State.create Target.Any
    
    [<AbstractClass>]
    type DebugBuilder<'T when 'T :> IField> () =
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        abstract Run : State<Debug.Target> -> Akka.Shared.Field<'T>
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: string) = Debug.State.create [x]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (a: unit) = Debug.State.create []
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine (state1: State<Debug.Target>, state2: State<Debug.Target>) : State<Debug.Target> =
            let fields = state1.Fields @ state2.Fields

            match Debug.Target.merge state1.Target state2.Target with
            | Some target -> { Target = target; Fields = fields }
            | None -> 
                failwithf "Debug fields mismatch found was given:%s%A"
                    System.Environment.NewLine fields
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero () = Debug.State.create []
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f()
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.For (state: State<Debug.Target>, f: unit -> State<Debug.Target>) = this.Combine(state, f())

        /// Enable DEBUG logging of all AutoReceiveMessages (Kill, PoisonPill et.c.)
        [<CustomOperation("autoreceive");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoReceive (state: State<Debug.Target>, value: bool) = 
            switchField "autoreceive" value
            |> State.addWith state Debug.Target.Actor
        /// Enable DEBUG logging of all AutoReceiveMessages (Kill, PoisonPill et.c.)
        member this.autoreceive value =
            this.AutoReceive(State.create Debug.Target.Actor [], value)
            |> this.Run
        
        /// Enable DEBUG logging of subscription changes on the eventStream
        [<CustomOperation("event_stream");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EventStream (state: State<Debug.Target>, value: bool) = 
            switchField "event-stream" value
            |> State.addWith state Debug.Target.Actor
        /// Enable DEBUG logging of subscription changes on the eventStream
        member this.event_stream value =
            this.EventStream(State.create Debug.Target.Actor [], value)
            |> this.Run
            
        /// Enable DEBUG logging of all LoggingFSMs for events, transitions and timers
        [<CustomOperation("fsm");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Fsm (state: State<Debug.Target>, value: bool) = 
            switchField "fsm" value
            |> State.addWith state Debug.Target.Actor
        /// Enable DEBUG logging of all LoggingFSMs for events, transitions and timers
        member this.fsm value =
            this.Fsm(State.create Debug.Target.Actor [], value)
            |> this.Run
            
        /// Enable DEBUG logging of actor lifecycle changes
        [<CustomOperation("lifecycle");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Lifecycle (state: State<Debug.Target>, value: bool) = 
            switchField "lifecycle" value
            |> State.addWith state Debug.Target.Actor
        /// Enable DEBUG logging of actor lifecycle changes
        member this.lifecycle value =
            this.Lifecycle(State.create Debug.Target.Actor [], value)
            |> this.Run
            
        /// Enable function of Actor.loggable(), which is to log any received message
        /// at DEBUG level, see the “Testing Actor Systems” section of the Akka
        /// Documentation at http://akka.io/docs
        [<CustomOperation("receive");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Receive (state: State<Debug.Target>, value: bool) = 
            switchField "receive" value
            |> State.addWith state Debug.Target.Actor
        /// Enable function of Actor.loggable(), which is to log any received message
        /// at DEBUG level, see the “Testing Actor Systems” section of the Akka
        /// Documentation at http://akka.io/docs
        member this.receive value =
            this.Receive(State.create Debug.Target.Actor [], value)
            |> this.Run

        /// Enable WARN logging of misconfigured routers
        [<CustomOperation("router_misconfiguration");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RouterMisconfiguration (state: State<Debug.Target>, value: bool) = 
            switchField "router-misconfiguration" value
            |> State.addWith state Debug.Target.Actor
        /// Enable WARN logging of misconfigured routers
        member this.router_misconfiguration value =
            this.RouterMisconfiguration(State.create Debug.Target.Actor [], value)
            |> this.Run

        /// Enable DEBUG logging of unhandled messages
        [<CustomOperation("unhandled");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Unhandled (state: State<Debug.Target>, value: bool) = 
            switchField "unhandled" value
            |> State.addWith state Debug.Target.Actor
        /// Enable DEBUG logging of unhandled messages
        member this.unhandled value =
            this.Unhandled(State.create Debug.Target.Actor [], value)
            |> this.Run

        // Akka.Cluster

        /// Log heartbeat events (very verbose, useful mostly when debugging heartbeating issues)
        [<CustomOperation("verbose_heartbeat_logging");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.VerboseHeartbeatLogging (state: State<Debug.Target>, value: bool) = 
            switchField "verbose-heartbeat-logging" value
            |> State.addWith state Debug.Target.Cluster
        /// Log heartbeat events (very verbose, useful mostly when debugging heartbeating issues)
        member this.verbose_heartbeat_logging value =
            this.VerboseHeartbeatLogging(State.create Debug.Target.Cluster [], value)
            |> this.Run
        
        /// Log gossip merge events (very verbose, useful when debugging convergence issues)
        [<CustomOperation("verbose_receive_gossip_logging");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.VerboseReceiveGossipLogging (state: State<Debug.Target>, value: bool) = 
            switchField "verbose-receive-gossip-logging" value
            |> State.addWith state Debug.Target.Cluster
        /// Log gossip merge events (very verbose, useful when debugging convergence issues)
        member this.verbose_receive_gossip_logging value =
            this.VerboseReceiveGossipLogging(State.create Debug.Target.Cluster [], value)
            |> this.Run

        // Akka.Streams.Materializer

        /// Enables the fuzzing mode which increases the chance of race conditions
        /// by aggressively reordering events and making certain operations more
        /// concurrent than usual.
        ///
        /// This setting is for testing purposes, NEVER enable this in a production
        /// environment!
        ///
        /// To get the best results, try combining this setting with a throughput
        /// of 1 on the corresponding dispatchers.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("fuzzing_mode");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.FuzzingMode (state: State<Debug.Target>, value: bool) = 
            switchField "fuzzing-mode" value
            |> State.addWith state Debug.Target.Materializer
        /// Enables the fuzzing mode which increases the chance of race conditions
        /// by aggressively reordering events and making certain operations more
        /// concurrent than usual.
        ///
        /// This setting is for testing purposes, NEVER enable this in a production
        /// environment!
        ///
        /// To get the best results, try combining this setting with a throughput
        /// of 1 on the corresponding dispatchers.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.FuzzingMode value =
            this.VerboseReceiveGossipLogging(State.create Debug.Target.Materializer [], value)
            |> this.Run
    
    let debug =
        { new DebugBuilder<Akka.Shared.Debug.Field>() with
            member _.Run (state: State<Debug.Target>) : Akka.Shared.Field<Akka.Shared.Debug.Field> =
                fun indent ->
                    objExpr "debug" indent state.Fields
                    |> Akka.Shared.Debug.Field }
