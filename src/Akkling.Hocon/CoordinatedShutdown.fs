namespace Akkling.Hocon

[<AutoOpen>]
module CoordinatedShutdown =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    [<AbstractClass>]
    type PhaseBuilder<'T when 'T :> IField> (name: string) =
        inherit BaseBuilder<'T>()

        member internal _.Name = name

        [<CustomOperation("depends_on");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DependsOn (state: string list, values: string list) =
            listField "depends-on" values::state
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DependsOn (state: string list, values: PhaseBuilder<_> list) =
            listField "depends-on" (values |> List.map (fun o -> o.Name))::state
        member this.depends_on (value: string list) =
            this.DependsOn([], value)
            |> this.Run
        member this.depends_on (value: PhaseBuilder<_> list) =
            this.DependsOn([], value)
            |> this.Run
        
        [<CustomOperation("timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Timeout (state: string list, value: 'Duration) =
            durationField "timeout" value::state
        member inline this.timeout (value: 'Duration) =
            this.Timeout([], value)
            |> this.Run

    let private createPhaseBuilder name =
        { new PhaseBuilder<Akka.CoordinatedShutdown.Phases.Field>(name) with
            member _.Run (state: string list) = 
                objExpr name 4 state
                |> Akka.CoordinatedShutdown.Phases.Field }

    /// The first pre-defined phase that applications can add tasks to.
    /// Note that more phases can be be added in the application's
    /// configuration by overriding this phase with an additional 
    /// depends-on.
    let before_service_unbind = createPhaseBuilder "before-service-unbind"

    /// Stop accepting new incoming requests in for example HTTP.
    let service_unbind = createPhaseBuilder "service-unbind"

    /// Wait for requests that are in progress to be completed.
    let service_requests_done = createPhaseBuilder "service-requests-done"

    /// Final shutdown of service endpoints.
    let service_stop = createPhaseBuilder "service-stop"

    /// Phase for custom application tasks that are to be run
    /// after service shutdown and before cluster shutdown.
    let before_cluster_shutdown = createPhaseBuilder "before-cluster-shutdown"

    /// Graceful shutdown of the Cluster Sharding regions.
    let cluster_sharding_shutdown_region = createPhaseBuilder "cluster-sharding-shutdown-region"

    /// Emit the leave command for the node that is shutting down.
    let cluster_leave = createPhaseBuilder "cluster-leave"

    /// Shutdown cluster singletons
    let cluster_exiting = createPhaseBuilder "cluster-exiting"

    /// Wait until exiting has been completed
    let cluster_exiting_done = createPhaseBuilder "cluster-exiting-done"

    /// Shutdown the cluster extension
    let cluster_shutdown = createPhaseBuilder "cluster-shutdown"

    /// Phase for custom application tasks that are to be run
    /// after cluster shutdown and before ActorSystem termination.
    let before_actor_system_terminate = createPhaseBuilder "before-actor-system-terminate"

    /// Last phase. See terminate-actor-system and exit-jvm above.
    /// Don't add phases that depends on this phase because the 
    /// dispatcher and scheduler of the ActorSystem have been shutdown. 
    let actor_system_terminate = createPhaseBuilder "actor-system-terminate"
    
    [<AbstractClass>]
    type PhasesBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.CoordinatedShutdown.Phases.Field) = [x.ToString()]

        // Pathing

        member private _.createPhaseBuilder name =
            { new PhaseBuilder<'T>(name) with
                member _.Run (state: string list) = pathObjExpr name state }

        /// The first pre-defined phase that applications can add tasks to.
        /// Note that more phases can be be added in the application's
        /// configuration by overriding this phase with an additional 
        /// depends-on.
        member this.before_service_unbind = this.createPhaseBuilder "before-service-unbind"

        /// Stop accepting new incoming requests in for example HTTP.
        member this.service_unbind = this.createPhaseBuilder "service-unbind"

        /// Wait for requests that are in progress to be completed.
        member this.service_requests_done = this.createPhaseBuilder "service-requests-done"

        /// Final shutdown of service endpoints.
        member this.service_stop = this.createPhaseBuilder "service-stop"

        /// Phase for custom application tasks that are to be run
        /// after service shutdown and before cluster shutdown.
        member this.before_cluster_shutdown = this.createPhaseBuilder "before-cluster-shutdown"

        /// Graceful shutdown of the Cluster Sharding regions.
        member this.cluster_sharding_shutdown_region = this.createPhaseBuilder "cluster-sharding-shutdown-region"

        /// Emit the leave command for the node that is shutting down.
        member this.cluster_leave = this.createPhaseBuilder "cluster-leave"

        /// Shutdown cluster singletons
        member this.cluster_exiting = this.createPhaseBuilder "cluster-exiting"

        /// Wait until exiting has been completed
        member this.cluster_exiting_done = this.createPhaseBuilder "cluster-exiting-done"

        /// Shutdown the cluster extension
        member this.cluster_shutdown = this.createPhaseBuilder "cluster-shutdown"

        /// Phase for custom application tasks that are to be run
        /// after cluster shutdown and before ActorSystem termination.
        member this.before_actor_system_terminate = this.createPhaseBuilder "before-actor-system-terminate"

        /// Last phase. See terminate-actor-system and exit-jvm above.
        /// Don't add phases that depends on this phase because the 
        /// dispatcher and scheduler of the ActorSystem have been shutdown. 
        member this.actor_system_terminate = this.createPhaseBuilder "actor-system-terminate"

    /// CoordinatedShutdown will run the tasks that are added to these
    /// phases. 
    ///
    /// The phases can be ordered as a DAG by defining the 
    /// dependencies between the phases.  
    /// 
    /// Each phase is defined as a named config section with the
    /// following optional properties:
    /// 
    /// - timeout=15s: Override the default-phase-timeout for this phase.
    /// 
    /// - recover=off: If the phase fails the shutdown is aborted
    ///                and depending phases will not be executed.
    /// 
    /// depends-on=[]: Run the phase after the given phases
    let phases =
        let path = "phases"
        let indent = 3

        { new PhasesBuilder<Akka.CoordinatedShutdown.Field>(Akka.CoordinatedShutdown.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.CoordinatedShutdown.Field }

    [<AbstractClass>]
    type CoordinatedShutdownBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        let indent = 2
        let pathObjExpr = pathObjExpr Akka.Field "coordinated-shutdown" indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.CoordinatedShutdown.Field) = [x.ToString()]
        
        /// The timeout that will be used for a phase if not specified with 
        /// 'timeout' in the phase
        [<CustomOperation("default_phase_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DefaultPhaseTimeout (state: string list, value: 'Duration) =
            durationField "default-phase-timeout" value::state
        /// The timeout that will be used for a phase if not specified with 
        /// 'timeout' in the phase
        member inline this.default_phase_timeout (value: 'Duration) =
            this.DefaultPhaseTimeout([], value)
            |> this.Run
            
        /// Terminate the ActorSystem in the last phase actor-system-terminate.
        [<CustomOperation("terminate_actor_system");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TerminateActorSystem (state: string list, value: bool) =
            switchField "terminate-actor-system" value::state
        member this.terminate_actor_system value =
            this.TerminateActorSystem([], value)
            |> this.Run
    
        /// Exit the CLR(Environment.Exit(0)) in the last phase actor-system-terminate
        /// if this is set to 'on'. It is done after termination of the 
        /// ActorSystem if terminate-actor-system=on, otherwise it is done 
        /// immediately when the last phase is reached.  
        [<CustomOperation("exit_clr");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ExitClr (state: string list, value: bool) =
            switchField "exit-clr" value::state
        /// Exit the CLR(Environment.Exit(0)) in the last phase actor-system-terminate
        /// if this is set to 'on'. It is done after termination of the 
        /// ActorSystem if terminate-actor-system=on, otherwise it is done 
        /// immediately when the last phase is reached.  
        member this.exit_clr value =
            this.ExitClr([], value)
            |> this.Run
        
        /// When shutting down the scheduler, there will typically be a thread which
        /// needs to be stopped, and this timeout determines how long to wait for
        /// that to happen. In case of timeout the shutdown of the actor system will
        /// proceed without running possibly still enqueued tasks.
        [<CustomOperation("run_by_clr_shutdown_hook");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RunByClrShutdownHook (state: string list, value: bool) =
            switchField "exit-clr" value::state
        /// When shutting down the scheduler, there will typically be a thread which
        /// needs to be stopped, and this timeout determines how long to wait for
        /// that to happen. In case of timeout the shutdown of the actor system will
        /// proceed without running possibly still enqueued tasks.
        member this.run_by_clr_shutdown_hook value =
            this.RunByClrShutdownHook([], value)
            |> this.Run

        /// Run the coordinated shutdown when ActorSystem.terminate is called.
        /// Enabling this and disabling terminate-actor-system is not a supported
        /// combination (will throw ConfigurationException at startup).
        [<CustomOperation("run_by_actor_system_terminate");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RunByActorSystemTerminate (state: string list, value: bool) =
            switchField "run-by-actor-system-terminate" value::state
        /// Run the coordinated shutdown when ActorSystem.terminate is called.
        /// Enabling this and disabling terminate-actor-system is not a supported
        /// combination (will throw ConfigurationException at startup).
        member this.run_by_actor_system_terminate value =
            this.RunByActorSystemTerminate([], value)
            |> this.Run
            
        // Pathing

        /// CoordinatedShutdown will run the tasks that are added to these
        /// phases. 
        ///
        /// The phases can be ordered as a DAG by defining the 
        /// dependencies between the phases.  
        /// 
        /// Each phase is defined as a named config section with the
        /// following optional properties:
        /// 
        /// - timeout=15s: Override the default-phase-timeout for this phase.
        /// 
        /// - recover=off: If the phase fails the shutdown is aborted
        ///                and depending phases will not be executed.
        /// 
        /// depends-on=[]: Run the phase after the given phases
        member _.phases =
            { new PhasesBuilder<Akka.Field>(Akka.Field, "coordinated-shutdown.phases", indent) with
                member _.Run (state: string list) = pathObjExpr "phases" state }

    /// CoordinatedShutdown is an extension that will perform registered
    /// tasks in the order that is defined by the phases. It is started
    /// by calling CoordinatedShutdown(system).run(). This can be triggered
    /// by different things, for example:
    ///
    /// - JVM shutdown hook will by default run CoordinatedShutdown
    ///
    /// - Cluster node will automatically run CoordinatedShutdown when it
    ///   sees itself as Exiting
    ///
    /// - A management console or other application specific command can 
    ///   run CoordinatedShutdown
    let coordinated_shutdown =
        { new CoordinatedShutdownBuilder<Akka.Field>() with
            member _.Run (state: string list) = 
                objExpr "coordinated-shutdown" 2 state
                |> Akka.Field }
