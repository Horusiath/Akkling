namespace Akkling.Hocon

[<AutoOpen>]
module FSM =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type FSMBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// PersistentFSM saves snapshots after this number of persistent
        /// events. Snapshots are used to reduce recovery times.
        ///
        /// When you disable this feature, specify snapshot-after = off.
        ///
        /// To enable the feature, specify a number like snapshot-after = 1000
        /// which means a snapshot is taken after persisting every 1000 events.
        [<CustomOperation("snapshot_after"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SnapshotAfter(state: string list, value: int) =
            positiveField "snapshot-after" value :: state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SnapshotAfter(state: string list, value: bool) =
            if value then
                state
            else
                switchField "snapshot-after" value :: state

        /// PersistentFSM saves snapshots after this number of persistent
        /// events. Snapshots are used to reduce recovery times.
        ///
        /// When you disable this feature, specify snapshot-after = off.
        ///
        /// To enable the feature, specify a number like snapshot-after = 1000
        /// which means a snapshot is taken after persisting every 1000 events.
        member this.snapshot_after(value: int) =
            this.SnapshotAfter([], value) |> this.Run

        /// PersistentFSM saves snapshots after this number of persistent
        /// events. Snapshots are used to reduce recovery times.
        ///
        /// When you disable this feature, specify snapshot-after = off.
        ///
        /// To enable the feature, specify a number like snapshot-after = 1000
        /// which means a snapshot is taken after persisting every 1000 events.
        member this.snapshot_after(value: bool) =
            this.SnapshotAfter([], value) |> this.Run

    /// Reliable delivery settings.
    let fsm =
        { new FSMBuilder<Akka.Persistence.Field>() with
            member _.Run(state: string list) =
                objExpr "fsm" 3 state |> Akka.Persistence.Field }
