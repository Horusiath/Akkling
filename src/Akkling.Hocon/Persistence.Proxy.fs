namespace Akkling.Hocon

[<AutoOpen>]
module Proxy =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<AbstractClass>]
    type ProxyBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string, indent: int) =
        inherit PluginBuilder<'T>(mkField, path, indent)

        /// Set this to on in the configuration of the ActorSystem
        /// that will host the target journal
        [<CustomOperation("start_target_journal"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StartTargetJournal(state: State<Plugin.Target>, value: bool) =
            switchField "start-target-journal" value
            |> State.addWith state Plugin.Target.Journal

        /// Set this to on in the configuration of the ActorSystem
        /// that will host the target journal
        member this.start_target_journal value =
            this.StartTargetJournal(State.create Plugin.Target.Journal [], value)
            |> this.Run

        /// The journal plugin config path to use for the target journal
        [<CustomOperation("target_journal_plugin"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TargetJournalPlugin(state: State<Plugin.Target>, x: string) =
            quotedField "target-journal-plugin" x
            |> State.addWith state Plugin.Target.Journal

        /// The journal plugin config path to use for the target journal
        member this.target_journal_plugin value =
            this.TargetJournalPlugin(State.create Plugin.Target.Journal [], value)
            |> this.Run

        /// The address of the proxy to connect to from other nodes.
        ///
        /// Optional setting.
        [<CustomOperation("target_journal_address"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TargetJournalAddress(state: State<Plugin.Target>, x: string) =
            System.Uri(x) |> ignore

            quotedField "target-journal-address" x
            |> State.addWith state Plugin.Target.Journal

        /// The address of the proxy to connect to from other nodes.
        ///
        /// Optional setting.
        member this.target_journal_address value =
            this.TargetJournalAddress(State.create Plugin.Target.Journal [], value)
            |> this.Run

        /// Set this to on in the configuration of the ActorSystem
        /// that will host the target snapshot-store
        [<CustomOperation("start_target_snapshot_store"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StartTargetSnapshotStore(state: State<Plugin.Target>, value: bool) =
            switchField "start-target-snapshot-store" value
            |> State.addWith state Plugin.Target.SnapshotStore

        /// Set this to on in the configuration of the ActorSystem
        /// that will host the target snapshot-store
        member this.start_target_snapshot_store value =
            this.StartTargetSnapshotStore(State.create Plugin.Target.SnapshotStore [], value)
            |> this.Run

        /// The journal plugin config path to use for the target snapshot-store
        [<CustomOperation("target_snapshot_store_plugin"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TargetSnapshotStorePlugin(state: State<Plugin.Target>, x: string) =
            quotedField "target-snapshot-store-plugin" x
            |> State.addWith state Plugin.Target.SnapshotStore

        /// The journal plugin config path to use for the target snapshot-store
        member this.target_snapshot_store_plugin value =
            this.TargetSnapshotStorePlugin(State.create Plugin.Target.SnapshotStore [], value)
            |> this.Run

        /// The address of the proxy to connect to from other nodes.
        ///
        /// Optional setting.
        [<CustomOperation("target_snapshot_store_address"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TargetSnapshotStoreAddress(state: State<Plugin.Target>, x: string) =
            System.Uri(x) |> ignore

            quotedField "target-snapshot-store-address" x
            |> State.addWith state Plugin.Target.SnapshotStore

        /// The address of the proxy to connect to from other nodes.
        ///
        /// Optional setting.
        member this.target_snapshot_store_address value =
            this.TargetSnapshotStoreAddress(State.create Plugin.Target.SnapshotStore [], value)
            |> this.Run

        /// Initialization timeout of target lookup
        [<CustomOperation("init_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.InitTimeout(state: State<Plugin.Target>, value: 'Duration) =
            durationField "init-timeout" value |> State.add state

        /// Initialization timeout of target lookup
        member inline this.init_timeout(value: 'Duration) =
            this.InitTimeout(State.create Plugin.Target.Any [], value) |> this.Run

    let proxy =
        let path = "proxy"
        let indent = 4

        { new ProxyBuilder<Akka.Persistence.Plugin.Field>(Akka.Persistence.Plugin.Field, path, indent) with
            member _.Run(state: State<Plugin.Target>) =
                objExpr path indent state.Fields |> Akka.Persistence.Plugin.Field }
