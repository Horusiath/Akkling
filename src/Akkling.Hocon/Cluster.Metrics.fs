namespace Akkling.Hocon

open System

[<AutoOpen>]
module Metrics =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    /// Supervision strategy.
    [<AbstractClass>]
    type ConfigurationBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Log restart attempts.
        [<CustomOperation("loggingEnabled"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LoggingEnabled(state: string list, value: bool) =
            switchField "loggingEnabled" value :: state

        /// Log restart attempts.
        member this.loggingEnabled value =
            this.LoggingEnabled([], value) |> this.Run

        /// Child actor restart-on-failure window.
        [<CustomOperation("within_time_range"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WithinTimeRange(state: string list, value: 'Duration) =
            durationField "withinTimeRange" value :: state

        /// Child actor restart-on-failure window.
        member inline this.within_time_range(value: 'Duration) =
            this.WithinTimeRange([], value) |> this.Run

        /// Maximum number of restart attempts before child actor is stopped.
        [<CustomOperation("max_nr_of_retries"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxNrOfRetries(state: string list, value: int) =
            positiveField "maxNrOfRetries" value :: state

        /// Maximum number of restart attempts before child actor is stopped.
        member this.max_nr_of_retries value =
            this.MaxNrOfRetries([], value) |> this.Run

    /// Supervision strategy.
    let configuration =
        { new ConfigurationBuilder<Akka.Cluster.Strategy.Field>() with
            member _.Run(state: string list) =
                objExpr "configuration" 6 state |> Akka.Cluster.Strategy.Field }

    /// Supervision strategy.
    [<AbstractClass>]
    type StrategyBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Cluster.Strategy.Field) = [ x.ToString() ]

        /// FQCN of class providing `akka.actor.SupervisorStrategy`.
        /// Must have a constructor with signature `<init>(com.typesafe.config.Config)`.
        /// Default metrics strategy provider is a configurable extension of `OneForOneStrategy`.
        [<CustomOperation("provider"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Provider(state: string list, x: Type) =
            quotedField "provider" (fqcn x) :: state

        /// FQCN of class providing `akka.actor.SupervisorStrategy`.
        /// Must have a constructor with signature `<init>(com.typesafe.config.Config)`.
        /// Default metrics strategy provider is a configurable extension of `OneForOneStrategy`.
        member this.provider value = this.Provider([], value) |> this.Run

    /// Supervision strategy.
    let strategy =
        { new StrategyBuilder<Akka.Cluster.Supervisor.Field>() with
            member _.Run(state: string list) =
                objExpr "strategy" 5 state |> Akka.Cluster.Supervisor.Field }

    /// Metrics supervisor actor.
    [<AbstractClass>]
    type SupervisorBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Cluster.Supervisor.Field) = [ x.ToString() ]

        /// Actor name of the mediator actor,
        ///
        /// Default: /system/distributedPubSubMediator
        [<CustomOperation("name"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Name(state: string list, x: string) = field "name" x :: state

        /// Actor name of the mediator actor,
        ///
        /// Default: /system/distributedPubSubMediator
        member this.name value = this.Name([], value) |> this.Run

    /// Metrics supervisor actor.
    let supervisor =
        { new SupervisorBuilder<Akka.Cluster.Metrics.Field>() with
            member _.Run(state: string list) =
                objExpr "supervisor" 4 state |> Akka.Cluster.Metrics.Field }

    /// Cluster metrics extension.
    ///
    /// Provides periodic statistics collection and publication throughout the cluster.
    [<AbstractClass>]
    type MetricsBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Cluster.Metrics.Field) = [ x.ToString() ]

        /// The id of the dispatcher to use for this actor.
        /// If undefined or empty the dispatcher specified in code
        /// (Props.withDispatcher) is used, or default-dispatcher if not
        /// specified at all.
        [<CustomOperation("dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Dispatcher(state: string list, value: string) = quotedField "dispatcher" value :: state

        /// The id of the dispatcher to use for this actor.
        /// If undefined or empty the dispatcher specified in code
        /// (Props.withDispatcher) is used, or default-dispatcher if not
        /// specified at all.
        member this.dispatcher value = this.Dispatcher([], value) |> this.Run

        /// How often keep-alive heartbeat messages should be sent to each connection.
        [<CustomOperation("periodic_tasks_initial_delay"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PeriodicTasksInitialDelay(state: string list, value: 'Duration) =
            durationField "periodic-tasks-initial-delay" value :: state

        /// How often keep-alive heartbeat messages should be sent to each connection.
        member inline this.periodic_tasks_initial_delay(value: 'Duration) =
            this.PeriodicTasksInitialDelay([], value) |> this.Run

    /// Cluster metrics extension.
    ///
    /// Provides periodic statistics collection and publication throughout the cluster.
    let metrics =
        { new MetricsBuilder<Akka.Cluster.Field>() with
            member _.Run(state: string list) =
                objExpr "metrics" 3 state |> Akka.Cluster.Field }
