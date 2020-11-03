namespace Akkling.Hocon

[<AutoOpen>]
module SnapshotStore =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel
    
    [<AbstractClass>]
    type LocalBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
            
        /// Fully qualified class name providing journal plugin api implementation.
        /// It is mandatory to specify this property.
        ///
        /// The class must have a constructor without parameters or constructor with
        /// one `Akka.Configuration.Config` parameter.
        [<CustomOperation("class'");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Class (state: string list, x: string) =
            quotedField "class" x::state
        /// Fully qualified class name providing journal plugin api implementation.
        /// It is mandatory to specify this property.
        ///
        /// The class must have a constructor without parameters or constructor with
        /// one `Akka.Configuration.Config` parameter.
        member this.class' value =
            this.Class([], value)
            |> this.Run
        
        /// Dispatcher for the plugin actor.
        [<CustomOperation("plugin_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.PluginDispatcher (state: string list, x: string) =
            quotedField "plugin-dispatcher" x::state
        /// Dispatcher for the plugin actor.
        member this.plugin_dispatcher value =
            this.PluginDispatcher([], value)
            |> this.Run
        
        /// Default serializer used as manifest serializer when applicable and 
        /// payload serializer when no specific binding overrides are specified
        [<CustomOperation("serializer");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Serializer (state: string list, x: string) =
            quotedField "serializer" x::state
        /// Default serializer used as manifest serializer when applicable and 
        /// payload serializer when no specific binding overrides are specified
        member this.serializer value =
            this.Serializer([], value)
            |> this.Run
        
        /// If there is more time in between individual events gotten from the Journal
        /// recovery than this the recovery will fail.
        ///
        /// Note that it also affect reading the snapshot before replaying events on
        /// top of it, even though iti is configured for the journal.
        [<CustomOperation("recovery_event_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RecoveryEventTimeout (state: string list, value: 'Duration) =
            durationField "recovery-event-timeout" value::state
        /// If there is more time in between individual events gotten from the Journal
        /// recovery than this the recovery will fail.
        ///
        /// Note that it also affect reading the snapshot before replaying events on
        /// top of it, even though iti is configured for the journal.
        member inline this.recovery_event_timeout (value: 'Duration) =
            this.RecoveryEventTimeout([], value)
            |> this.Run
        
        /// Dispatcher for streaming snapshot IO.
        [<CustomOperation("stream_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StreamDispatcher (state: string list, x: string) =
            quotedField "stream-dispatcher" x::state
        /// Dispatcher for streaming snapshot IO.
        member this.stream_dispatcher value =
            this.StreamDispatcher([], value)
            |> this.Run
        
        /// Storage location of snapshot files.
        [<CustomOperation("dir");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Dir (state: string list, x: string) =
            quotedField "dir" x::state
        /// Storage location of snapshot files.
        member this.dir value =
            this.Dir([], value)
            |> this.Run
        
        /// Number load attempts when recovering from the latest snapshot fails
        /// yet older snapshot files are available. Each recovery attempt will try
        /// to recover using an older than previously failed-on snapshot file
        /// (if any are present). If all attempts fail the recovery will fail and
        /// the persistent actor will be stopped.
        [<CustomOperation("max_load_attempts");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxLoadAttempts (state: string list, value: int) =
            positiveField "max-load-attempts" value::state
        /// Number load attempts when recovering from the latest snapshot fails
        /// yet older snapshot files are available. Each recovery attempt will try
        /// to recover using an older than previously failed-on snapshot file
        /// (if any are present). If all attempts fail the recovery will fail and
        /// the persistent actor will be stopped.
        member this.max_load_attempts value =
            this.MaxLoadAttempts([], value)
            |> this.Run

    let local =
        { new LocalBuilder<Akka.Persistence.SnapshotStore.Field>() with
            member _.Run (state: string list) = 
                objExpr "local" 4 state
                |> Akka.Persistence.SnapshotStore.Field }

    [<AbstractClass>]
    type NoSnapshotStoreBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
            
        [<CustomOperation("class'");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Class (state: string list, x: string) =
            quotedField "class" x::state
        member this.class' value =
            this.Class([], value)
            |> this.Run
    
    /// Used as default-snapshot store if no plugin configured
    let no_snapshot_store =
        { new NoSnapshotStoreBuilder<Akka.Persistence.Field>() with
            member _.Run (state: string list) = 
                objExpr "no-snapshot-store" 3 state
                |> Akka.Persistence.Field }
    
    [<AbstractClass>]
    type SnapshotStoreBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Persistence.Plugin.Field) = [x.ToString()]
            
        /// Absolute path to the snapshot plugin configuration entry used by
        /// persistent actor or view by default.
        ///
        /// Persistent actor or view can override `snapshotPluginId` method
        /// in order to rely on a different snapshot plugin.
        ///
        /// It is not mandatory to specify a snapshot store plugin.
        ///
        /// If you don't use snapshots you don't have to configure it.
        ///
        /// Note that Cluster Sharding is using snapshots, so if you
        /// use Cluster Sharding you need to define a snapshot store plugin.
        [<CustomOperation("plugin");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Plugin (state: string list, x: string) =
            quotedField "plugin" x::state
        /// Absolute path to the snapshot plugin configuration entry used by
        /// persistent actor or view by default.
        ///
        /// Persistent actor or view can override `snapshotPluginId` method
        /// in order to rely on a different snapshot plugin.
        ///
        /// It is not mandatory to specify a snapshot store plugin.
        ///
        /// If you don't use snapshots you don't have to configure it.
        ///
        /// Note that Cluster Sharding is using snapshots, so if you
        /// use Cluster Sharding you need to define a snapshot store plugin.
        member this.plugin value =
            this.Plugin([], value)
            |> this.Run
        
        /// List of journal plugins to start automatically.
        /// 
        /// Use "" for the default snapshot store.
        [<CustomOperation("auto_start_snapshot_stores");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoStartSnapshotStores (state: string list, xs: string list) =
            quotedListField "auto-start-snapshot-stores" xs::state
        /// List of journal plugins to start automatically.
        /// 
        /// Use "" for the default snapshot store.
        member this.auto_start_snapshot_stores value =
            this.AutoStartSnapshotStores([], value)
            |> this.Run

        // Pathing
        
        member _.local =
            { new LocalBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "local" state }

        member _.plugin_custom name =
            let path = sprintf "%s.%s" path name

            { new PluginBuilder<'T>(mkField, path, indent) with
                member _.Run (state: State<Plugin.Target>) = pathObjExpr name state.Fields }

        member _.inmem = plugin_custom "inmem"
        
        member _.proxy =
            let path = sprintf "%s.proxy" path

            { new ProxyBuilder<'T>(mkField, path, indent) with
                member _.Run (state: State<Plugin.Target>) = pathObjExpr "proxy" state.Fields }

    let snapshot_store =
        let path = "snapshot-store"
        let indent = 3

        { new SnapshotStoreBuilder<Akka.Persistence.Field>(Akka.Persistence.Field, path, indent) with
            member _.Run (state: string list) =
                objExpr path indent state
                |> Akka.Persistence.Field }
