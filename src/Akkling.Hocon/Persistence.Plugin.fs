namespace Akkling.Hocon

[<AutoOpen>]
module Plugin =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<AbstractClass>]
    type CircuitBreakerBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// AtLeastOnceDelivery is allowed to hold in memory.
        [<CustomOperation("max_failures"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxFailures(state: string list, value: int) =
            positiveField "max-failures" value :: state

        /// AtLeastOnceDelivery is allowed to hold in memory.
        member this.max_failures value = this.MaxFailures([], value) |> this.Run

        /// Interval between incremental updates.
        [<CustomOperation("call_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.CallTimeout(state: string list, value: 'Duration) =
            durationField "call-timeout" value :: state

        /// Interval between incremental updates.
        member inline this.call_timeout(value: 'Duration) = this.CallTimeout([], value) |> this.Run

        /// Interval between incremental updates.
        [<CustomOperation("reset_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ResetTimeout(state: string list, value: 'Duration) =
            durationField "reset_timeout" value :: state

        /// Interval between incremental updates.
        member inline this.reset_timeout(value: 'Duration) =
            this.ResetTimeout([], value) |> this.Run

    let circuit_breaker =
        { new CircuitBreakerBuilder<Akka.Persistence.CircuitBreaker.Field>() with
            member _.Run(state: string list) =
                objExpr "jounal-plugin-fallback" 4 state
                |> Akka.Persistence.CircuitBreaker.Field }

    [<AbstractClass>]
    type ReplayFilterBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// What the filter should do when detecting invalid events.
        /// Supported values:
        ///
        /// `repair-by-discard-old` : discard events from old writers,
        ///                           warning is logged
        ///
        /// `fail` : fail the replay, error is logged
        ///
        /// `warn` : log warning but emit events untouched
        ///
        /// `off` : disable this feature completely
        [<CustomOperation("mode"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Mode(state: string list, value: Hocon.ReplayFilter.Mode) = field "mode" value.Text :: state

        /// What the filter should do when detecting invalid events.
        /// Supported values:
        ///
        /// `repair-by-discard-old` : discard events from old writers,
        ///                           warning is logged
        ///
        /// `fail` : fail the replay, error is logged
        ///
        /// `warn` : log warning but emit events untouched
        ///
        /// `off` : disable this feature completely
        member this.mode value = this.Mode([], value) |> this.Run

        /// It uses a look ahead buffer for analyzing the events.
        ///
        /// This defines the size (in number of events) of the buffer.
        [<CustomOperation("window_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.WindowSize(state: string list, value: int) =
            positiveField "window-size" value :: state

        /// It uses a look ahead buffer for analyzing the events.
        ///
        /// This defines the size (in number of events) of the buffer.
        member this.window_size value = this.WindowSize([], value) |> this.Run

        /// How many old writerUuid to remember
        [<CustomOperation("max_old_writers"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxOldWriters(state: string list, value: int) =
            positiveField "max-old-writers" value :: state

        /// How many old writerUuid to remember
        member this.max_old_writers value =
            this.MaxOldWriters([], value) |> this.Run

        /// Set this to `on` to enable detailed debug logging of each
        /// replayed event.
        [<CustomOperation("debug"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Debug(state: string list, value: bool) = switchField "debug" value :: state

        /// Set this to `on` to enable detailed debug logging of each
        /// replayed event.
        member this.debug value = this.Debug([], value) |> this.Run

    /// The replay filter can detect a corrupt event stream by inspecting
    /// sequence numbers and writerUuid when replaying events.
    let replay_filter =
        { new ReplayFilterBuilder<Akka.Persistence.ReplayFilter.Field>() with
            member _.Run(state: string list) =
                objExpr "replay-filter" 4 state |> Akka.Persistence.ReplayFilter.Field }

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    module Plugin =
        [<RequireQualifiedAccess>]
        type Target =
            | Any
            | Journal
            | SnapshotStore

        [<RequireQualifiedAccess>]
        module Target =
            let merge (target1: Target) (target2: Target) =
                match target1, target2 with
                | Target.Any, _
                | _, Target.Any
                | _, _ when target1 = target2 -> Some target1
                | _ -> None

        [<RequireQualifiedAccess>]
        module State =
            let create = State.create Target.Any

    [<AbstractClass>]
    type PluginBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string, indent: int) =
        let pathObjExpr = pathObjExpr mkField path indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        abstract Run: State<Plugin.Target> -> 'T

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: string) = Plugin.State.create [ x ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Persistence.CircuitBreaker.Field) = Plugin.State.create [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Persistence.ReplayFilter.Field) =
            State.create Plugin.Target.SnapshotStore [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(a: unit) = Plugin.State.create []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine(state1: State<Plugin.Target>, state2: State<Plugin.Target>) : State<Plugin.Target> =
            let fields = state1.Fields @ state2.Fields

            match Plugin.Target.merge state1.Target state2.Target with
            | Some target -> { Target = target; Fields = fields }
            | None ->
                failwithf
                    "Plugin only supports targeting a journal or snapshot, but was given fields for both.%s%A"
                    System.Environment.NewLine
                    fields

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero() = Plugin.State.create []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f ()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.For(state: State<Plugin.Target>, f: unit -> State<Plugin.Target>) = this.Combine(state, f ())

        /// Fully qualified class name providing journal plugin api implementation.
        /// It is mandatory to specify this property.
        ///
        /// The class must have a constructor without parameters or constructor with
        /// one `Akka.Configuration.Config` parameter.
        [<CustomOperation("class'"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Class(state: State<Plugin.Target>, x: string) =
            quotedField "class" x |> State.add state

        /// Fully qualified class name providing journal plugin api implementation.
        /// It is mandatory to specify this property.
        ///
        /// The class must have a constructor without parameters or constructor with
        /// one `Akka.Configuration.Config` parameter.
        member this.class' value =
            this.Class(State.create Plugin.Target.Any [], value) |> this.Run

        /// Dispatcher for the plugin actor.
        [<CustomOperation("plugin_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.PluginDispatcher(state: State<Plugin.Target>, x: string) =
            quotedField "plugin-dispatcher" x |> State.add state

        /// Dispatcher for the plugin actor.
        member this.plugin_dispatcher value =
            this.PluginDispatcher(State.create Plugin.Target.Any [], value) |> this.Run

        /// Dispatcher for message replay.
        [<CustomOperation("replay_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ReplayDispatcher(state: State<Plugin.Target>, x: string) =
            quotedField "replay-dispatcher" x |> State.addWith state Plugin.Target.Journal

        /// Dispatcher for message replay.
        member this.replay_dispatcher value =
            this.ReplayDispatcher(State.create Plugin.Target.Journal [], value) |> this.Run

        /// Default serializer used as manifest serializer when applicable and
        /// payload serializer when no specific binding overrides are specified
        [<CustomOperation("serializer"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Serializer(state: State<Plugin.Target>, x: string) =
            quotedField "serializer" x |> State.add state

        /// Default serializer used as manifest serializer when applicable and
        /// payload serializer when no specific binding overrides are specified
        member this.serializer value =
            this.Serializer(State.create Plugin.Target.Any [], value) |> this.Run

        /// If there is more time in between individual events gotten from the Journal
        /// recovery than this the recovery will fail.
        ///
        /// Note that it also affect reading the snapshot before replaying events on
        /// top of it, even though iti is configured for the journal.
        [<CustomOperation("recovery_event_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RecoveryEventTimeout(state: State<Plugin.Target>, value: 'Duration) =
            durationField "recovery-event-timeout" value |> State.add state

        /// If there is more time in between individual events gotten from the Journal
        /// recovery than this the recovery will fail.
        ///
        /// Note that it also affect reading the snapshot before replaying events on
        /// top of it, even though iti is configured for the journal.
        member inline this.recovery_event_timeout(value: 'Duration) =
            this.RecoveryEventTimeout(State.create Plugin.Target.Any [], value) |> this.Run

        // Pathing

        member _.circuit_breaker =
            { new CircuitBreakerBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr "jounal-plugin-fallback" state }

        /// The replay filter can detect a corrupt event stream by inspecting
        /// sequence numbers and writerUuid when replaying events.
        member _.replay_filter =
            { new ReplayFilterBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr "replay-filter" state }

    let plugin_custom name =
        let indent = 4

        { new PluginBuilder<Akka.Persistence.Plugin.Field>(Akka.Persistence.Plugin.Field, name, indent) with
            member _.Run(state: State<Plugin.Target>) =
                objExpr name 4 state.Fields |> Akka.Persistence.Plugin.Field }

    let inmem = plugin_custom "inmem"
