namespace Akkling.Hocon

[<AutoOpen>]
module Persistence =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    [<AbstractClass>]
    type PersistenceBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        let indent = 2
        let pathObjExpr = pathObjExpr Akka.Field "actor" indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.Dispatcher.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Persistence.Field) = [x.ToString()]
        
        /// When starting many persistent actors at the same time the journal
        /// and its data store is protected from being overloaded by limiting number
        /// of recoveries that can be in progress at the same time. When
        /// exceeding the limit the actors will wait until other recoveries have
        /// been completed.  
        [<CustomOperation("max_concurrent_recoveries");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxConcurrentRecoveries (state: string list, value: int) =
            positiveField "max-concurrent-recoveries" value::state
        /// When starting many persistent actors at the same time the journal
        /// and its data store is protected from being overloaded by limiting number
        /// of recoveries that can be in progress at the same time. When
        /// exceeding the limit the actors will wait until other recoveries have
        /// been completed.  
        member this.max_concurrent_recoveries value =
            this.MaxConcurrentRecoveries([], value)
            |> this.Run
        
        /// Fully qualified class name providing a default internal stash overflow strategy.
        /// It needs to be a subclass of Akka.Persistence.StashOverflowStrategyConfigurator
        /// The default strategy throws StashOverflowException
        [<CustomOperation("internal_stash_overflow_strategy");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.InternalStashOverflowStrategy (state: string list, x: string) =
            quotedField "internal-stash-overflow-strategy" x::state
        /// Fully qualified class name providing a default internal stash overflow strategy.
        /// It needs to be a subclass of Akka.Persistence.StashOverflowStrategyConfigurator
        /// The default strategy throws StashOverflowException
        member this.internal_stash_overflow_strategy value =
            this.InternalStashOverflowStrategy([], value)
            |> this.Run
        
        // Pathing
        
        /// Reliable delivery settings.
        member _.fsm =
            { new FSMBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "fsm" state }

        /// Reliable delivery settings.
        member _.at_least_once_delivery =
            { new AtLeastOnceDeliveryBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "at-least-once-delivery" state }

        /// Persistent view settings.
        member _.view =
            { new ViewBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "view" state }

        /// Fallback settings for journal plugin configurations.
        ///
        /// These settings are used if they are not defined in plugin config section.
        member _.journal_plugin_fallback = 
            { new JournalPluginFallbackBuilder<Akka.Field>(Akka.Field, "persistence.jounal-plugin-fallback", indent) with
                member _.Run (state: string list) = pathObjExpr "jounal-plugin-fallback" state }

        /// Fallback settings for snapshot store plugin configurations
        ///
        /// These settings are used if they are not defined in plugin config section.
        member _.snapshot_store_plguin_fallback = 
            { new PluginFallbackBuilder<Akka.Field>(Akka.Field, "persistence.snapshot-store-plugin-fallback", indent) with
                member _.Run (state: string list) = pathObjExpr "snapshot-store-plugin-fallback" state }

        member _.journal =
            { new JournalBuilder<Akka.Field>(Akka.Field, "persistence.journal", indent) with
                member _.Run (state: string list) = pathObjExpr "journal" state }

        /// Used as default-snapshot store if no plugin configured
        member _.no_snapshot_store =
            { new NoSnapshotStoreBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "no-snapshot-store" state }
        
        member _.snapshot_store =
            { new SnapshotStoreBuilder<Akka.Field>(Akka.Field, "persistence.snapshot-store", indent) with
                member _.Run (state: string list) = pathObjExpr "snapshot-store" state }

    let persistence =
        { new PersistenceBuilder<Akka.Field>() with
            member _.Run (state: string list) = 
                objExpr "persistence" 2 state
                |> Akka.Field }
