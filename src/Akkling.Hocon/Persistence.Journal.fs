namespace Akkling.Hocon

[<AutoOpen>]
module Journal =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<AbstractClass>]
    type JournalBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Persistence.Plugin.Field) = [x.ToString()]
            
        /// Absolute path to the journal plugin configuration entry used by
        /// persistent actor or view by default.
        ///
        /// Persistent actor or view can override `journalPluginId` method
        /// in order to rely on a different journal plugin.
        [<CustomOperation("plugin");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Plugin (state: string list, x: string) =
            quotedField "plugin" x::state
        /// Absolute path to the journal plugin configuration entry used by
        /// persistent actor or view by default.
        ///
        /// Persistent actor or view can override `journalPluginId` method
        /// in order to rely on a different journal plugin.
        member this.plugin value =
            this.Plugin([], value)
            |> this.Run
        
        /// List of journal plugins to start automatically.
        /// 
        /// Use "" for the default journal plugin.
        [<CustomOperation("auto_start_journals");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoStartJournals (state: string list, xs: string list) =
            quotedListField "auto-start-journals" xs::state
        /// List of journal plugins to start automatically.
        /// 
        /// Use "" for the default journal plugin.
        member this.auto_start_journals value =
            this.AutoStartJournals([], value)
            |> this.Run
        
        // Pathing

        member _.plugin_custom name =
            let path = sprintf "%s.%s" path name

            { new PluginBuilder<'T>(mkField, path, indent) with
                member _.Run (state: State<Plugin.Target>) = pathObjExpr name state.Fields }

        member _.inmem = plugin_custom "inmem"
        
        member _.proxy =
            let path = sprintf "%s.proxy" path

            { new ProxyBuilder<'T>(mkField, path, indent) with
                member _.Run (state: State<Plugin.Target>) = pathObjExpr "proxy" state.Fields }

    let journal =
        let path = "journal"
        let indent = 3

        { new JournalBuilder<Akka.Persistence.Field>(Akka.Persistence.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.Persistence.Field }
