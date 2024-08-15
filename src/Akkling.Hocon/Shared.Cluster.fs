namespace Akkling.Hocon

[<AutoOpen>]
module Cluster =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    module Cluster =
        [<RequireQualifiedAccess>]
        type Target =
            | Any
            | Cluster
            | DeploymentDefault

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
    type ClusterBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string) =
        let pathObjExpr = pathObjExpr mkField path

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        abstract Run: State<Cluster.Target> -> Akka.Shared.Field<'T>

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: string) = Cluster.State.create [ x ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Cluster.Field) =
            State.create Cluster.Target.Cluster [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Scheduler.Field) =
            State.create Cluster.Target.Cluster [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Shared.Debug.Field>) =
            State.create Cluster.Target.Cluster [ (x 3).ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(a: unit) = Cluster.State.create []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine(state1: State<Cluster.Target>, state2: State<Cluster.Target>) : State<Cluster.Target> =
            let fields = state1.Fields @ state2.Fields

            match Cluster.Target.merge state1.Target state2.Target with
            | Some target -> { Target = target; Fields = fields }
            | None -> failwithf "Cluster fields mismatch found was given:%s%A" System.Environment.NewLine fields

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero() = Cluster.State.create []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f ()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.For(state: State<Cluster.Target>, f: unit -> State<Cluster.Target>) = this.Combine(state, f ())

        // Akka.Cluster

        /// Initial contact points of the cluster.
        /// The nodes to join automatically at startup.
        ///
        /// Comma separated full URIs defined by a string on the form of
        /// "akka.tcp://system@hostname:port"
        ///
        /// Leave as empty if the node is supposed to be joined manually.
        [<CustomOperation("seed_nodes"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SeedNodes(state: State<Cluster.Target>, xs: string list) =
            xs |> List.iter (System.Uri >> ignore)
            quotedListField "seed-nodes" xs |> State.addWith state Cluster.Target.Cluster

        /// Initial contact points of the cluster.
        /// The nodes to join automatically at startup.
        ///
        /// Comma separated full URIs defined by a string on the form of
        /// "akka.tcp://system@hostname:port"
        ///
        /// Leave as empty if the node is supposed to be joined manually.
        member this.seed_nodes value =
            this.SeedNodes(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// How long to wait for one of the seed nodes to reply to initial join request.
        ///
        /// When this is the first seed node and there is no positive reply from the other
        /// seed nodes within this timeout it will join itself to bootstrap the cluster.
        ///
        /// When this is not the first seed node the join attempts will be performed with
        /// this interval.
        [<CustomOperation("seed_node_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SeedNodeTimeout(state: State<Cluster.Target>, value: 'Duration) =
            durationField "seed-node-timeout" value
            |> State.addWith state Cluster.Target.Cluster

        /// How long to wait for one of the seed nodes to reply to initial join request.
        ///
        /// When this is the first seed node and there is no positive reply from the other
        /// seed nodes within this timeout it will join itself to bootstrap the cluster.
        ///
        /// When this is not the first seed node the join attempts will be performed with
        /// this interval.
        member inline this.seed_node_timeout(value: 'Duration) =
            this.SeedNodeTimeout(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// If a join request fails it will be retried after this period.
        ///
        /// Disable join retry by specifying "off".
        [<CustomOperation("retry_unsuccessful_join_after"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RetryUnsuccessfulJoinAfter(state: State<Cluster.Target>, value: 'Duration) =
            durationField "retry-unsuccessful-join-after" value
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RetryUnsuccessfulJoinAfter(state: State<Cluster.Target>, value: bool) =
            if value then
                state
            else
                switchField "retry-unsuccessful-join-after" value
                |> State.addWith state Cluster.Target.Cluster

        /// If a join request fails it will be retried after this period.
        ///
        /// Disable join retry by specifying "off".
        member inline this.retry_unsuccessful_join_after(value: 'Duration) =
            this.RetryUnsuccessfulJoinAfter(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// If a join request fails it will be retried after this period.
        ///
        /// Disable join retry by specifying "off".
        member this.retry_unsuccessful_join_after(value: bool) =
            this.RetryUnsuccessfulJoinAfter(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// The joining of given seed nodes will by default be retried indefinitely until
        /// a successful join. That process can be aborted if unsuccessful by defining this
        /// timeout. When aborted it will run CoordinatedShutdown, which by default will
        /// terminate the ActorSystem. CoordinatedShutdown can also be configured to exit
        /// the JVM. It is useful to define this timeout if the seed-nodes are assembled
        /// dynamically and a restart with new seed-nodes should be tried after unsuccessful
        /// attempts.
        [<CustomOperation("shutdown_after_unsuccessful_join_seed_nodes"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ShutdownAfterUnsuccessfulJoinSeedNodes(state: State<Cluster.Target>, value: 'Duration) =
            durationField "shutdown-after-unsuccessful-join-seed-nodes" value
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ShutdownAfterUnsuccessfulJoinSeedNodes(state: State<Cluster.Target>, value: bool) =
            if value then
                state
            else
                switchField "shutdown-after-unsuccessful-join-seed-nodes" value
                |> State.addWith state Cluster.Target.Cluster

        /// The joining of given seed nodes will by default be retried indefinitely until
        /// a successful join. That process can be aborted if unsuccessful by defining this
        /// timeout. When aborted it will run CoordinatedShutdown, which by default will
        /// terminate the ActorSystem. CoordinatedShutdown can also be configured to exit
        /// the JVM. It is useful to define this timeout if the seed-nodes are assembled
        /// dynamically and a restart with new seed-nodes should be tried after unsuccessful
        /// attempts.
        member inline this.shutdown_after_unsuccessful_join_seed_nodes(value: 'Duration) =
            this.ShutdownAfterUnsuccessfulJoinSeedNodes(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// The joining of given seed nodes will by default be retried indefinitely until
        /// a successful join. That process can be aborted if unsuccessful by defining this
        /// timeout. When aborted it will run CoordinatedShutdown, which by default will
        /// terminate the ActorSystem. CoordinatedShutdown can also be configured to exit
        /// the JVM. It is useful to define this timeout if the seed-nodes are assembled
        /// dynamically and a restart with new seed-nodes should be tried after unsuccessful
        /// attempts.
        member this.shutdown_after_unsuccessful_join_seed_nodes(value: bool) =
            this.ShutdownAfterUnsuccessfulJoinSeedNodes(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Should the 'leader' in the cluster be allowed to automatically mark
        /// unreachable nodes as DOWN after a configured time of unreachability?
        /// Using auto-down implies that two separate clusters will automatically be
        /// formed in case of network partition.
        ///
        /// Disable with "off" or specify a duration to enable auto-down.
        ///
        /// If a downing-provider-class is configured this setting is ignored.
        [<CustomOperation("auto_down_unreachable_after"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.AutoDownUnreachableAfter(state: State<Cluster.Target>, value: 'Duration) =
            durationField "auto-down-unreachable-after" value
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoDownUnreachableAfter(state: State<Cluster.Target>, value: bool) =
            if value then
                state
            else
                switchField "auto-down-unreachable-after" value
                |> State.addWith state Cluster.Target.Cluster

        /// Should the 'leader' in the cluster be allowed to automatically mark
        /// unreachable nodes as DOWN after a configured time of unreachability?
        /// Using auto-down implies that two separate clusters will automatically be
        /// formed in case of network partition.
        ///
        /// Disable with "off" or specify a duration to enable auto-down.
        ///
        /// If a downing-provider-class is configured this setting is ignored.
        member inline this.auto_down_unreachable_after(value: 'Duration) =
            this.AutoDownUnreachableAfter(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Should the 'leader' in the cluster be allowed to automatically mark
        /// unreachable nodes as DOWN after a configured time of unreachability?
        /// Using auto-down implies that two separate clusters will automatically be
        /// formed in case of network partition.
        ///
        /// Disable with "off" or specify a duration to enable auto-down.
        ///
        /// If a downing-provider-class is configured this setting is ignored.
        member this.auto_down_unreachable_after(value: bool) =
            this.AutoDownUnreachableAfter(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Time margin after which shards or singletons that belonged to a downed/removed
        /// partition are created in surviving partition. The purpose of this margin is that
        /// in case of a network partition the persistent actors in the non-surviving partitions
        /// must be stopped before corresponding persistent actors are started somewhere else.
        /// This is useful if you implement downing strategies that handle network partitions,
        /// e.g. by keeping the larger side of the partition and shutting down the smaller side.
        /// It will not add any extra safety for auto-down-unreachable-after, since that is not
        /// handling network partitions.
        ///
        /// Disable with "off" or specify a duration to enable.
        [<CustomOperation("down_removal_margin"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DownRemovalMargin(state: State<Cluster.Target>, value: 'Duration) =
            durationField "down-removal-margin" value
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DownRemovalMargin(state: State<Cluster.Target>, value: bool) =
            if value then
                state
            else
                switchField "down-removal-margin" value
                |> State.addWith state Cluster.Target.Cluster

        /// Time margin after which shards or singletons that belonged to a downed/removed
        /// partition are created in surviving partition. The purpose of this margin is that
        /// in case of a network partition the persistent actors in the non-surviving partitions
        /// must be stopped before corresponding persistent actors are started somewhere else.
        /// This is useful if you implement downing strategies that handle network partitions,
        /// e.g. by keeping the larger side of the partition and shutting down the smaller side.
        /// It will not add any extra safety for auto-down-unreachable-after, since that is not
        /// handling network partitions.
        ///
        /// Disable with "off" or specify a duration to enable.
        member inline this.down_removal_margin(value: 'Duration) =
            this.DownRemovalMargin(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Time margin after which shards or singletons that belonged to a downed/removed
        /// partition are created in surviving partition. The purpose of this margin is that
        /// in case of a network partition the persistent actors in the non-surviving partitions
        /// must be stopped before corresponding persistent actors are started somewhere else.
        /// This is useful if you implement downing strategies that handle network partitions,
        /// e.g. by keeping the larger side of the partition and shutting down the smaller side.
        /// It will not add any extra safety for auto-down-unreachable-after, since that is not
        /// handling network partitions.
        ///
        /// Disable with "off" or specify a duration to enable.
        member this.down_removal_margin(value: bool) =
            this.DownRemovalMargin(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Pluggable support for downing of nodes in the cluster.
        ///
        /// If this setting is left empty behaviour will depend on 'auto-down-unreachable' in the following ways:
        ///
        /// * if it is 'off' the `NoDowning` provider is used and no automatic downing will be performed
        ///
        /// * if it is set to a duration the `AutoDowning` provider is with the configured downing duration
        ///
        /// If specified the value must be the fully qualified class name of a subclass of
        /// `akka.cluster.DowningProvider` having a public one argument constructor accepting an `ActorSystem`
        [<CustomOperation("downing_provider_class"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DowningProviderClass(state: State<Cluster.Target>, x: string) =
            quotedField "downing-provider-class" x
            |> State.addWith state Cluster.Target.Cluster

        /// Pluggable support for downing of nodes in the cluster.
        ///
        /// If this setting is left empty behaviour will depend on 'auto-down-unreachable' in the following ways:
        ///
        /// * if it is 'off' the `NoDowning` provider is used and no automatic downing will be performed
        ///
        /// * if it is set to a duration the `AutoDowning` provider is with the configured downing duration
        ///
        /// If specified the value must be the fully qualified class name of a subclass of
        /// `akka.cluster.DowningProvider` having a public one argument constructor accepting an `ActorSystem`
        member this.downing_provider_class value =
            this.DowningProviderClass(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// If this is set to "off", the leader will not move 'Joining' members to 'Up' during a network
        /// split. This feature allows the leader to accept 'Joining' members to be 'WeaklyUp'
        /// so they become part of the cluster even during a network split. The leader will
        /// move `Joining` members to 'WeaklyUp' after 3 rounds of 'leader-actions-interval'
        /// without convergence.
        ///
        /// The leader will move 'WeaklyUp' members to 'Up' status once convergence has been reached.
        [<CustomOperation("allow_weakly_up_members"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AllowWeaklyUpMembers(state: State<Cluster.Target>, value: bool) =
            switchField "allow-weakly-up-members" value
            |> State.addWith state Cluster.Target.Cluster

        /// If this is set to "off", the leader will not move 'Joining' members to 'Up' during a network
        /// split. This feature allows the leader to accept 'Joining' members to be 'WeaklyUp'
        /// so they become part of the cluster even during a network split. The leader will
        /// move `Joining` members to 'WeaklyUp' after 3 rounds of 'leader-actions-interval'
        /// without convergence.
        ///
        /// The leader will move 'WeaklyUp' members to 'Up' status once convergence has been reached.
        member this.allow_weakly_up_members value =
            this.AllowWeaklyUpMembers(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// The roles of this member. List of strings, e.g. roles = ["A", "B"].
        ///
        /// The roles are part of the membership information and can be used by
        /// routers or other services to distribute work to certain member types,
        /// e.g. front-end and back-end nodes.
        [<CustomOperation("roles"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Roles(state: State<Cluster.Target>, xs: string list) =
            listField "roles" xs |> State.addWith state Cluster.Target.Cluster

        /// The roles of this member. List of strings, e.g. roles = ["A", "B"].
        ///
        /// The roles are part of the membership information and can be used by
        /// routers or other services to distribute work to certain member types,
        /// e.g. front-end and back-end nodes.
        member this.roles value =
            this.Roles(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        [<CustomOperation("app_version"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppVersion(state: State<Cluster.Target>, major: int, minor: int, patch: int) =
            sprintf "version = %i.%i.%i" major minor patch
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppVersion(state: State<Cluster.Target>, major: int, minor: int, patch: int, qualifier: string) =
            sprintf "version = %i.%i.%i-%s" major minor patch qualifier
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppVersion(state: State<Cluster.Target>, major: int, minor: int) =
            sprintf "version = %i.%i" major minor
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppVersion(state: State<Cluster.Target>, major: int, minor: int, qualifier: string) =
            sprintf "version = %i.%i-%s" major minor qualifier
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppVersion(state: State<Cluster.Target>, major: int) =
            sprintf "version = %i" major |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppVersion(state: State<Cluster.Target>, major: int, qualifier: string) =
            sprintf "version = %i-%s" major qualifier
            |> State.addWith state Cluster.Target.Cluster

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        member this.app_version(major: int, minor: int, patch: int) =
            this.AppVersion(State.create Cluster.Target.Cluster [], major, minor, patch)
            |> this.Run

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        member this.app_version(major: int, minor: int, patch: int, qualifier: string) =
            this.AppVersion(State.create Cluster.Target.Cluster [], major, minor, patch, qualifier)
            |> this.Run

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        member this.app_version(major: int, minor: int) =
            this.AppVersion(State.create Cluster.Target.Cluster [], major, minor)
            |> this.Run

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        member this.app_version(major: int, minor: int, qualifier: string) =
            this.AppVersion(State.create Cluster.Target.Cluster [], major, minor, qualifier)
            |> this.Run

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        member this.app_version(major: int) =
            this.AppVersion(State.create Cluster.Target.Cluster [], major) |> this.Run

        /// Application version of the deployment. Used by rolling update features
        /// to distinguish between old and new nodes. The typical convention is to use
        /// 3 digit version numbers `major.minor.patch`, but 1 or two digits are also
        /// supported.
        ///
        /// If no `.` is used it is interpreted as a single digit version number or as
        /// plain alphanumeric if it couldn't be parsed as a number.
        ///
        /// It may also have a qualifier at the end for 2 or 3 digit version numbers such
        /// as "1.2-RC1".
        /// For 1 digit with qualifier, 1-RC1, it is interpreted as plain alphanumeric.
        member this.app_version(major: int, qualifier: string) =
            this.AppVersion(State.create Cluster.Target.Cluster [], major, qualifier)
            |> this.Run

        /// Run the coordinated shutdown from phase 'cluster-shutdown' when the cluster
        /// is shutdown for other reasons than when leaving, e.g. when downing. This
        /// will terminate the ActorSystem when the cluster extension is shutdown.
        [<CustomOperation("run_coordinated_shutdown_when_down"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RunCoordinatedShutdownWhenDown(state: State<Cluster.Target>, value: bool) =
            switchField "run-coordinated-shutdown-when-down" value
            |> State.addWith state Cluster.Target.Cluster

        /// Run the coordinated shutdown from phase 'cluster-shutdown' when the cluster
        /// is shutdown for other reasons than when leaving, e.g. when downing. This
        /// will terminate the ActorSystem when the cluster extension is shutdown.
        member this.run_coordinated_shutdown_when_down value =
            this.RunCoordinatedShutdownWhenDown(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Minimum required number of members before the leader changes member status
        /// of 'Joining' members to 'Up'. Typically used together with
        /// 'Cluster.registerOnMemberUp' to defer some action, such as starting actors,
        /// until the cluster has reached a certain size.
        [<CustomOperation("min_nr_of_members"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MinNrOfMembers(state: State<Cluster.Target>, value: int) =
            positiveField "min-nr-of-members" value
            |> State.addWith state Cluster.Target.Cluster

        /// Minimum required number of members before the leader changes member status
        /// of 'Joining' members to 'Up'. Typically used together with
        /// 'Cluster.registerOnMemberUp' to defer some action, such as starting actors,
        /// until the cluster has reached a certain size.
        member this.min_nr_of_members value =
            this.MinNrOfMembers(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// Enable/disable info level logging of cluster events
        [<CustomOperation("log_info"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogInfo(state: State<Cluster.Target>, value: bool) =
            switchField "log-info" value |> State.addWith state Cluster.Target.Cluster

        /// Enable/disable info level logging of cluster events
        member this.log_info value =
            this.LogInfo(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// Enable/disable verbose info-level logging of cluster events
        /// for temporary troubleshooting. Defaults to 'off'.
        [<CustomOperation("log_info_verbose"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogInfoVerbose(state: State<Cluster.Target>, value: bool) =
            switchField "log-info-verbose" value
            |> State.addWith state Cluster.Target.Cluster

        /// Enable/disable verbose info-level logging of cluster events
        /// for temporary troubleshooting. Defaults to 'off'.
        member this.log_info_verbose value =
            this.LogInfoVerbose(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// How long should the node wait before starting the periodic tasks
        /// maintenance tasks?
        [<CustomOperation("periodic_tasks_initial_delay"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PeriodicTasksInitialDelay(state: State<Cluster.Target>, value: 'Duration) =
            durationField "periodic-tasks-initial-delay" value
            |> State.addWith state Cluster.Target.Cluster

        /// How long should the node wait before starting the periodic tasks
        /// maintenance tasks?
        member inline this.periodic_tasks_initial_delay(value: 'Duration) =
            this.PeriodicTasksInitialDelay(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// How often should the node send out gossip information?
        [<CustomOperation("gossip_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.GossipInterval(state: State<Cluster.Target>, value: 'Duration) =
            durationField "gossip-interval" value
            |> State.addWith state Cluster.Target.Cluster

        /// How often should the node send out gossip information?
        member inline this.gossip_interval(value: 'Duration) =
            this.GossipInterval(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// discard incoming gossip messages if not handled within this duration
        [<CustomOperation("gossip_time_to_live"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.GossipTimeToLive(state: State<Cluster.Target>, value: 'Duration) =
            durationField "gossip-time-to-live" value
            |> State.addWith state Cluster.Target.Cluster

        /// discard incoming gossip messages if not handled within this duration
        member inline this.gossip_time_to_live(value: 'Duration) =
            this.GossipTimeToLive(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// How often should the leader perform maintenance tasks?
        [<CustomOperation("leader_actions_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.LeaderActionsInterval(state: State<Cluster.Target>, value: 'Duration) =
            durationField "leader-actions-interval" value
            |> State.addWith state Cluster.Target.Cluster

        /// How often should the leader perform maintenance tasks?
        member inline this.leader_actions_interval(value: 'Duration) =
            this.LeaderActionsInterval(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// How often should the node move nodes, marked as unreachable by the failure
        /// detector, out of the membership ring?
        [<CustomOperation("unreachable_nodes_reaper_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.UnreachableNodesReaperInterval(state: State<Cluster.Target>, value: 'Duration) =
            durationField "unreachable-nodes-reaper-interval" value
            |> State.addWith state Cluster.Target.Cluster

        /// How often should the node move nodes, marked as unreachable by the failure
        /// detector, out of the membership ring?
        member inline this.unreachable_nodes_reaper_interval(value: 'Duration) =
            this.UnreachableNodesReaperInterval(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// How often the current internal stats should be published.
        /// A value of 0s can be used to always publish the stats, when it happens.
        ///
        /// Disable with "off".
        [<CustomOperation("publish_stats_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PublishStatsInterval(state: State<Cluster.Target>, value: 'Duration) =
            durationField "publish-stats-interval" value
            |> State.addWith state Cluster.Target.Cluster

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.PublishStatsInterval(state: State<Cluster.Target>, value: bool) =
            if value then
                this.PublishStatsInterval(state, 0<s>)
            else
                switchField "publish-stats-interval" value
                |> State.addWith state Cluster.Target.Cluster

        /// How often the current internal stats should be published.
        /// A value of 0s can be used to always publish the stats, when it happens.
        ///
        /// Disable with "off".
        member inline this.publish_stats_interval(value: 'Duration) =
            this.PublishStatsInterval(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// How often the current internal stats should be published.
        /// A value of 0s can be used to always publish the stats, when it happens.
        ///
        /// Disable with "off".
        member this.publish_stats_interval(value: bool) =
            this.PublishStatsInterval(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// The id of the dispatcher to use for cluster actors.
        ///
        /// If not specified, the internal dispatcher is used.
        ///
        /// If specified you need to define the settings of the actual dispatcher.
        [<CustomOperation("use_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcher(state: State<Cluster.Target>, x: string) =
            quotedField "use-dispatcher" x |> State.addWith state Cluster.Target.Cluster

        /// The id of the dispatcher to use for cluster actors.
        ///
        /// If not specified, the internal dispatcher is used.
        ///
        /// If specified you need to define the settings of the actual dispatcher.
        member this.use_dispatcher value =
            this.UseDispatcher(State.create Cluster.Target.Cluster [], value) |> this.Run

        /// Gossip to random node with newer or older state information, if any with
        /// this probability. Otherwise Gossip to any random live node.
        ///
        /// Probability value is between 0.0 and 1.0. 0.0 means never, 1.0 means always.
        [<CustomOperation("gossip_different_view_probability"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.GossipDifferentViewProbability(state: State<Cluster.Target>, value: float) =
            let fieldName = "gossip-different-view-probability"

            if value > 1. || value < 0. then
                failwithf "Field %s must have a value between 0.0 and 1.0. You provided: %f" fieldName value
            else
                positiveFieldf fieldName value
            |> State.addWith state Cluster.Target.Cluster

        /// Gossip to random node with newer or older state information, if any with
        /// this probability. Otherwise Gossip to any random live node.
        ///
        /// Probability value is between 0.0 and 1.0. 0.0 means never, 1.0 means always.
        member this.gossip_different_view_probability value =
            this.GossipDifferentViewProbability(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        /// Reduced the above probability when the number of nodes in the cluster
        /// greater than this value.
        [<CustomOperation("reduce_gossip_different_view_probability"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ReduceGossipDifferentViewProbability(state: State<Cluster.Target>, value: int) =
            positiveField "reduce-gossip-different-view-probability" value
            |> State.addWith state Cluster.Target.Cluster

        /// Reduced the above probability when the number of nodes in the cluster
        /// greater than this value.
        member this.reduce_gossip_different_view_probability value =
            this.ReduceGossipDifferentViewProbability(State.create Cluster.Target.Cluster [], value)
            |> this.Run

        // Akka.Actor.Deployment.Default.Cluster

        /// Enable cluster aware router that deploys to nodes in the cluster
        [<CustomOperation("enabled"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Enabled(state: State<Cluster.Target>, value: bool) =
            switchField "enabled" value
            |> State.addWith state Cluster.Target.DeploymentDefault

        /// Enable cluster aware router that deploys to nodes in the cluster
        member this.enabled value =
            this.Enabled(State.create Cluster.Target.DeploymentDefault [], value)
            |> this.Run

        /// Maximum number of routees that will be deployed on each cluster
        /// member node.
        ///
        /// Note that max-total-nr-of-instances defines total number of routees, but
        /// number of routees per node will not be exceeded, i.e. if you
        /// define max-total-nr-of-instances = 50 and max-nr-of-instances-per-node = 2
        /// it will deploy 2 routees per new member in the cluster, up to
        /// 25 members.
        [<CustomOperation("max_nr_of_instances_per_node"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxNrOfInstancesPerNode(state: State<Cluster.Target>, value: int) =
            positiveField "max-nr-of-instances-per-node" value
            |> State.addWith state Cluster.Target.DeploymentDefault

        /// Maximum number of routees that will be deployed on each cluster
        /// member node.
        ///
        /// Note that max-total-nr-of-instances defines total number of routees, but
        /// number of routees per node will not be exceeded, i.e. if you
        /// define max-total-nr-of-instances = 50 and max-nr-of-instances-per-node = 2
        /// it will deploy 2 routees per new member in the cluster, up to
        /// 25 members.
        member this.max_nr_of_instances_per_node value =
            this.MaxNrOfInstancesPerNode(State.create Cluster.Target.DeploymentDefault [], value)
            |> this.Run

        /// Maximum number of routees that will be deployed, in total
        /// on all nodes. See also description of max-nr-of-instances-per-node.
        ///
        /// For backwards compatibility reasons, nr-of-instances
        /// has the same purpose as max-total-nr-of-instances for cluster
        /// aware routers and nr-of-instances (if defined by user) takes
        /// precedence over max-total-nr-of-instances.
        [<CustomOperation("max_total_nr_of_instances"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxTotalNrOfInstances(state: State<Cluster.Target>, value: int) =
            positiveField "max-total-nr-of-instances" value
            |> State.addWith state Cluster.Target.DeploymentDefault

        /// Maximum number of routees that will be deployed, in total
        /// on all nodes. See also description of max-nr-of-instances-per-node.
        ///
        /// For backwards compatibility reasons, nr-of-instances
        /// has the same purpose as max-total-nr-of-instances for cluster
        /// aware routers and nr-of-instances (if defined by user) takes
        /// precedence over max-total-nr-of-instances.
        member this.max_total_nr_of_instances value =
            this.MaxTotalNrOfInstances(State.create Cluster.Target.DeploymentDefault [], value)
            |> this.Run

        /// Defines if routees are allowed to be located on the same node as
        /// the head router actor, or only on remote nodes.
        ///
        /// Useful for master-worker scenario where all routees are remote.
        [<CustomOperation("allow_local_routees"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AllowLocalRoutees(state: State<Cluster.Target>, value: bool) =
            switchField "allow-local-routees" value
            |> State.addWith state Cluster.Target.DeploymentDefault

        /// Defines if routees are allowed to be located on the same node as
        /// the head router actor, or only on remote nodes.
        ///
        /// Useful for master-worker scenario where all routees are remote.
        member this.allow_local_routees value =
            this.AllowLocalRoutees(State.create Cluster.Target.DeploymentDefault [], value)
            |> this.Run

        /// Use members with specified role, or all members if undefined or empty.
        [<CustomOperation("use_role")>]
        member _.UseRole(state: State<Cluster.Target>, value: string) =
            quotedField "use-role" value
            |> State.addWith state Cluster.Target.DeploymentDefault

        /// Use members with specified role, or all members if undefined or empty.
        member this.use_role value =
            this.UseRole(State.create Cluster.Target.DeploymentDefault [], value)
            |> this.Run

        // Pathing

        member _.debug =
            { new DebugBuilder<'T>() with
                member _.Run(state: State<Debug.Target>) : Akka.Shared.Field<'T> =
                    fun _ -> pathObjExpr 5 "debug" state.Fields }

        /// Cluster metrics extension.
        ///
        /// Provides periodic statistics collection and publication throughout the cluster.
        member _.metrics =
            { new MetricsBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "metrics" state }

        /// Settings for the DistributedPubSub extension
        member _.pub_sub =
            { new PubSubBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "pub-sub" state }

        /// Settings for the ClusterClient
        member _.client =
            { new ClientBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "client" state }

        member _.role =
            { new RoleBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "role" state }

        /// Settings for the Phi accrual failure detector (http://ddg.jaist.ac.jp/pub/HDY+04.pdf
        /// [Hayashibara et al]) used by the cluster subsystem to detect unreachable
        /// members.
        member _.failure_detector =
            { new FailureDetectorBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "failure-detector" state }

        member _.split_brain_resolver =
            { new SplitBrainResolverBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr 3 "split-brain-resolver" state }

        member _.singleton_proxy =
            { new SingletonProxyBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "singleton-proxy" state }

        member _.singleton =
            { new SingletonBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 3 "singleton" state }

    let cluster =
        let path = "cluster"

        { new ClusterBuilder<Akka.Shared.Cluster.Field>(Akka.Shared.Cluster.Field, path) with
            member _.Run(state: State<Cluster.Target>) : Akka.Shared.Field<Akka.Shared.Cluster.Field> =
                fun indent -> objExpr "cluster" indent state.Fields |> Akka.Shared.Cluster.Field }
