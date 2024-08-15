namespace Akkling.Hocon

[<AutoOpen>]
module View =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type ViewBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Automated incremental view update.
        [<CustomOperation("auto_update"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoUpdate(state: string list, value: bool) =
            switchField "auto-update" value :: state

        /// Automated incremental view update.
        member this.auto_update value = this.AutoUpdate([], value) |> this.Run

        /// Interval between incremental updates.
        [<CustomOperation("auto_update_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.AutoUpdateInterval(state: string list, value: 'Duration) =
            durationField "auto-update-interval" value :: state

        /// Interval between incremental updates.
        member inline this.auto_update_interval(value: 'Duration) =
            this.AutoUpdateInterval([], value) |> this.Run

        /// Maximum number of messages to replay per incremental view update.
        ///
        /// Set to -1 for no upper limit.
        [<CustomOperation("auto_update_replay_max"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoUpdateReplayMax(state: string list, value: int) =
            numField "auto-update-replay-max" value :: state

        /// Maximum number of messages to replay per incremental view update.
        ///
        /// Set to -1 for no upper limit.
        member this.auto_update_replay_max value =
            this.AutoUpdateReplayMax([], value) |> this.Run

    /// Persistent view settings.
    let view =
        { new ViewBuilder<Akka.Persistence.Field>() with
            member _.Run(state: string list) =
                objExpr "view" 3 state |> Akka.Persistence.Field }
