namespace Akkling.Hocon

[<AutoOpen>]
module Deployment =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type ResizerBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<CustomOperation("enabled"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Enabled(state: string list, value: bool) = switchField "enabled" value :: state

        member this.enabled value = this.Enabled([], value) |> this.Run

        /// The fewest number of routees the router should ever have.
        [<CustomOperation("lower_bound"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LowerBound(state: string list, value: int) =
            positiveField "lower-bound" value :: state

        /// The fewest number of routees the router should ever have.
        member this.lower_bound value = this.LowerBound([], value) |> this.Run

        /// The most number of routees the router should ever have.
        /// Must be greater than or equal to lower-bound.
        [<CustomOperation("upper_bound"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UpperBound(state: string list, value: int) =
            positiveField "upper-bound" value :: state

        /// The most number of routees the router should ever have.
        /// Must be greater than or equal to lower-bound.
        member this.upper_bound value = this.UpperBound([], value) |> this.Run

        /// Threshold used to evaluate if a routee is considered to be busy
        /// (under pressure).
        ///
        /// Implementation depends on this value (default is 1).
        ///
        /// 0:   number of routees currently processing a message.
        ///
        /// 1:   number of routees currently processing a message has
        ///      some messages in mailbox.
        ///
        /// > 1: number of routees with at least the configured pressure-threshold
        ///      messages in their mailbox. Note that estimating mailbox size of
        ///      default UnboundedMailbox is O(N) operation.
        [<CustomOperation("pressure_threshold"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.PressureThreshold(state: string list, value: int) =
            positiveField "pressure-threshold" value :: state

        /// Threshold used to evaluate if a routee is considered to be busy
        /// (under pressure).
        ///
        /// Implementation depends on this value (default is 1).
        ///
        /// 0:   number of routees currently processing a message.
        ///
        /// 1:   number of routees currently processing a message has
        ///      some messages in mailbox.
        ///
        /// > 1: number of routees with at least the configured pressure-threshold
        ///      messages in their mailbox. Note that estimating mailbox size of
        ///      default UnboundedMailbox is O(N) operation.
        member this.pressure_threshold value =
            this.PressureThreshold([], value) |> this.Run

        /// Percentage to increase capacity whenever all routees are busy.
        /// For example, 0.2 would increase 20% (rounded up), i.e. if current
        /// capacity is 6 it will request an increase of 2 more routees.
        [<CustomOperation("rampup_rate"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RampupRate(state: string list, value) =
            positiveFieldf "rampup-rate" value :: state

        /// Percentage to increase capacity whenever all routees are busy.
        /// For example, 0.2 would increase 20% (rounded up), i.e. if current
        /// capacity is 6 it will request an increase of 2 more routees.
        member inline this.rampup_rate value = this.RampupRate([], value) |> this.Run

        /// Minimum fraction of busy routees before backing off.
        /// For example, if this is 0.3, then we'll remove some routees only when
        /// less than 30% of routees are busy, i.e. if current capacity is 10 and
        /// 3 are busy then the capacity is unchanged, but if 2 or less are busy
        /// the capacity is decreased.
        /// Use 0.0 or negative to avoid removal of routees.
        [<CustomOperation("backoff_threshold"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BackoffThreshold(state: string list, value) =
            numFieldf "backoff-threshold" value :: state

        /// Minimum fraction of busy routees before backing off.
        /// For example, if this is 0.3, then we'll remove some routees only when
        /// less than 30% of routees are busy, i.e. if current capacity is 10 and
        /// 3 are busy then the capacity is unchanged, but if 2 or less are busy
        /// the capacity is decreased.
        /// Use 0.0 or negative to avoid removal of routees.
        member inline this.backoff_threshold value =
            this.BackoffThreshold([], value) |> this.Run

        /// Percentage to increase capacity whenever all routees are busy.
        /// For example, 0.2 would increase 20% (rounded up), i.e. if current
        /// capacity is 6 it will request an increase of 2 more routees.
        [<CustomOperation("backoff_rate"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BackoffRate(state: string list, value) =
            positiveFieldf "backoff-rate" value :: state

        /// Percentage to increase capacity whenever all routees are busy.
        /// For example, 0.2 would increase 20% (rounded up), i.e. if current
        /// capacity is 6 it will request an increase of 2 more routees.
        member inline this.backoff_rate value = this.BackoffRate([], value) |> this.Run

        /// Number of messages between resize operation.
        /// Use 1 to resize before each message.
        [<CustomOperation("messages_per_resize"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MessagesPerResize(state: string list, value: int) =
            positiveField "messages-per-resize" value :: state

        /// Number of messages between resize operation.
        /// Use 1 to resize before each message.
        member this.messages_per_resize value =
            this.MessagesPerResize([], value) |> this.Run

    /// Routers with dynamically resizable number of routees; this feature is
    /// enabled by including (parts of) this section in the deployment
    let resizer =
        { new ResizerBuilder<Akka.Actor.DeploymentDefault.Field>() with
            member _.Run(state: string list) =
                objExpr "resizer" 5 state |> Akka.Actor.DeploymentDefault.Field }

    [<AbstractClass>]
    type RouteesBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Alternatively to giving nr-of-instances you can specify the full
        /// paths of those actors which should be routed to. This setting takes
        /// precedence over nr-of-instances
        [<CustomOperation("paths"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Paths(state: string list, value: string list) = quotedListField "paths" value :: state

        /// Alternatively to giving nr-of-instances you can specify the full
        /// paths of those actors which should be routed to. This setting takes
        /// precedence over nr-of-instances
        member this.paths value = this.Paths([], value) |> this.Run

    let routees =
        { new RouteesBuilder<Akka.Actor.DeploymentDefault.Field>() with
            member _.Run(state: string list) =
                objExpr "routees" 5 state |> Akka.Actor.DeploymentDefault.Field }

    [<AbstractClass>]
    type DeployeeBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()

        let pathObjExpr = pathObjExpr mkField path indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Actor.DeploymentDefault.Field) = [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Shared.Cluster.Field>) = [ (x 4).ToString() ]

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

        /// The id of the mailbox to use for this actor.
        /// If undefined or empty the default mailbox of the configured dispatcher
        /// is used or if there is no mailbox configuration the mailbox specified
        /// in code (Props.withMailbox) is used.
        /// If there is a mailbox defined in the configured dispatcher then that
        /// overrides this setting.
        [<CustomOperation("mailbox"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Mailbox(state: string list, value: string) = quotedField "mailbox" value :: state

        /// The id of the mailbox to use for this actor.
        /// If undefined or empty the default mailbox of the configured dispatcher
        /// is used or if there is no mailbox configuration the mailbox specified
        /// in code (Props.withMailbox) is used.
        /// If there is a mailbox defined in the configured dispatcher then that
        /// overrides this setting.
        member this.mailbox value = this.Mailbox([], value) |> this.Run

        /// Routing (load-balance) scheme to use.
        ///
        /// - available:
        /// "from-code", "round-robin", "random", "smallest-mailbox",
        /// "scatter-gather", "broadcast"
        ///
        /// - or:
        /// Fully qualified class name of the router class.
        /// The class must extend akka.routing.CustomRouterConfig and
        /// have a public constructor with com.typesafe.config.Config
        /// and optional akka.actor.DynamicAccess parameter.
        ///
        /// - default is "from-code";
        ///
        /// Whether or not an actor is transformed to a Router is decided in code
        /// only (Props.withRouter). The type of router can be overridden in the
        /// configuration; specifying "from-code" means that the values specified
        /// in the code shall be used.
        ///
        /// In case of routing, the actors to be routed to can be specified
        /// in several ways:
        ///
        /// - nr-of-instances: will create that many children
        ///
        /// - routees.paths: will route messages to these paths using ActorSelection,
        ///   i.e. will not create children
        ///
        /// - resizer: dynamically resizable number of routees as specified in
        /// resizer
        [<CustomOperation("router"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Router(state: string list, value: Hocon.Router.LoadBalance) =
            quotedField "router" value.Text :: state

        /// Routing (load-balance) scheme to use.
        ///
        /// - available:
        /// "from-code", "round-robin", "random", "smallest-mailbox",
        /// "scatter-gather", "broadcast"
        ///
        /// - or:
        /// Fully qualified class name of the router class.
        /// The class must extend akka.routing.CustomRouterConfig and
        /// have a public constructor with com.typesafe.config.Config
        /// and optional akka.actor.DynamicAccess parameter.
        ///
        /// - default is "from-code";
        ///
        /// Whether or not an actor is transformed to a Router is decided in code
        /// only (Props.withRouter). The type of router can be overridden in the
        /// configuration; specifying "from-code" means that the values specified
        /// in the code shall be used.
        ///
        /// In case of routing, the actors to be routed to can be specified
        /// in several ways:
        ///
        /// - nr-of-instances: will create that many children
        ///
        /// - routees.paths: will route messages to these paths using ActorSelection,
        ///   i.e. will not create children
        ///
        /// - resizer: dynamically resizable number of routees as specified in
        /// resizer
        member this.router value = this.Router([], value) |> this.Run

        /// Number of children to create in case of a router;
        /// this setting is ignored if routees.paths is given
        [<CustomOperation("nr_of_instances"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.NumberOfInstances(state: string list, value: int) =
            positiveField "nr-of-instances" value :: state

        /// Number of children to create in case of a router;
        /// this setting is ignored if routees.paths is given
        member this.nr_of_instances value =
            this.NumberOfInstances([], value) |> this.Run

        /// Within is the timeout used for routers containing future calls
        [<CustomOperation("within"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Within(state: string list, value: 'Duration) = durationField "within" value :: state

        /// Within is the timeout used for routers containing future calls
        member inline this.within(value: 'Duration) = this.Within([], value) |> this.Run

        /// Number of virtual nodes per node for consistent-hashing router
        [<CustomOperation("virtual_nodes_factor"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.VirtualNodesFactor(state: string list, value: int) =
            positiveField "virtual-nodes-factor" value :: state

        /// Number of virtual nodes per node for consistent-hashing router
        member this.virtual_nodes_factor value =
            this.VirtualNodesFactor([], value) |> this.Run

        /// MetricsSelector to use
        ///
        /// - available: "mix", "heap", "cpu", "load"
        ///
        /// - or: Fully qualified class name of the MetricsSelector class.
        ///       The class must extend akka.cluster.routing.MetricsSelector
        ///       and have a public constructor with com.typesafe.config.Config
        ///       parameter.
        ///
        /// - default is "mix"
        [<CustomOperation("metrics_selector"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MetricsSelector(state: string list, value: Hocon.MetricSelection) =
            field "metrics-selector" value.Text :: state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MetricsSelector(state: string list, value: string) =
            quotedField "metrics-selector" value :: state

        /// MetricsSelector to use
        ///
        /// - available: "mix", "heap", "cpu", "load"
        ///
        /// - or: Fully qualified class name of the MetricsSelector class.
        ///       The class must extend akka.cluster.routing.MetricsSelector
        ///       and have a public constructor with com.typesafe.config.Config
        ///       parameter.
        ///
        /// - default is "mix"
        member this.metrics_selector(value: Hocon.MetricSelection) =
            this.MetricsSelector([], value) |> this.Run

        /// MetricsSelector to use
        ///
        /// - available: "mix", "heap", "cpu", "load"
        ///
        /// - or: Fully qualified class name of the MetricsSelector class.
        ///       The class must extend akka.cluster.routing.MetricsSelector
        ///       and have a public constructor with com.typesafe.config.Config
        ///       parameter.
        ///
        /// - default is "mix"
        member this.metrics_selector(value: string) =
            this.MetricsSelector([], value) |> this.Run

        // Pathing

        member _.cluster =
            { new ClusterBuilder<'T>(mkField, path) with
                member _.Run(state: SharedInternal.State<Cluster.Target>) : Akka.Shared.Field<'T> =
                    fun _ -> pathObjExpr "cluster" state.Fields }

        /// Routers with dynamically resizable number of routees; this feature is
        /// enabled by including (parts of) this section in the deployment
        member _.resizer =
            { new ResizerBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr "resizer" state }

        member _.routees =
            { new RouteesBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr "routees" state }

    /// Deployment actor configuration
    let deploy (name: string) =
        let indent = 4

        { new DeployeeBuilder<Akka.Actor.Deployment.Field>(Akka.Actor.Deployment.Field, name, indent) with
            member _.Run(state: string list) =
                objExpr name indent state |> Akka.Actor.Deployment.Field }

    /// Deployment id pattern - on the format: /parent/child etc.
    let default' = deploy "default"

    [<AbstractClass>]
    type DeploymentBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()

        let pathObjExpr = pathObjExpr mkField path indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Actor.Deployment.Field) = [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Actor.Deployment.Field>) = [ (x indent).ToString() ]

        // Pathing

        /// Deployment actor configuration
        member _.deploy(name: string) =
            let path = sprintf "%s.%s" path name

            { new DeployeeBuilder<'T>(mkField, path, indent) with
                member _.Run(state: string list) = pathObjExpr name state }

        /// Deployment id pattern - on the format: /parent/child etc.
        member this.default' = this.deploy "default"

    let deployment =
        let path = "deployment"
        let indent = 3

        { new DeploymentBuilder<Akka.Actor.Field>(Akka.Actor.Field, path, indent) with
            member _.Run(state: string list) =
                objExpr path indent state |> Akka.Actor.Field }
