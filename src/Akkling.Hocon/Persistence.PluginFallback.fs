namespace Akkling.Hocon

[<AutoOpen>]
module PluginFallback =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel
    
    [<AbstractClass>]
    type PluginFallbackBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Persistence.CircuitBreaker.Field) = [x.ToString()]
            
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
    
        // Pathing

        member _.circuit_breaker =
            { new CircuitBreakerBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "jounal-plugin-fallback" state }
        
        member _.proxy =
            let path = sprintf "%s.proxy" path

            { new ProxyBuilder<'T>(mkField, path, indent) with
                member _.Run (state: State<Plugin.Target>) = pathObjExpr "proxy" state.Fields }

    /// Fallback settings for snapshot store plugin configurations
    ///
    /// These settings are used if they are not defined in plugin config section.
    let snapshot_store_plguin_fallback =
        let path = "snapshot-store-plugin-fallback"
        let indent = 3

        { new PluginFallbackBuilder<Akka.Persistence.Field>(Akka.Persistence.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.Persistence.Field }

    [<AbstractClass>]
    type JournalPluginFallbackBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit PluginFallbackBuilder<'T>(mkField, path, indent)
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Persistence.ReplayFilter.Field) = [x.ToString()]
            
        /// Dispatcher for message replay.
        [<CustomOperation("replay_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ReplayDispatcher (state: string list, x: string) =
            quotedField "replay-dispatcher" x::state
        /// Dispatcher for message replay.
        member this.replay_dispatcher value =
            this.ReplayDispatcher([], value)
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

        // Pathing
        
        /// The replay filter can detect a corrupt event stream by inspecting
        /// sequence numbers and writerUuid when replaying events.
        member _.replay_filter =
            { new ReplayFilterBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "replay-filter" state }

    /// Fallback settings for journal plugin configurations.
    ///
    /// These settings are used if they are not defined in plugin config section.
    let journal_plugin_fallback = 
        let path = "jounal-plugin-fallback"
        let indent = 3

        { new JournalPluginFallbackBuilder<Akka.Persistence.Field>(Akka.Persistence.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.Persistence.Field }
