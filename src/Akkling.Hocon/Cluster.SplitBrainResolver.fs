namespace Akkling.Hocon

[<AutoOpen>]
module SplitBrainResolver =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type KeepMajorityBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// If the 'role' is defined the decision is based only on members with that 'role'
        [<CustomOperation("role"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role(state: string list, x: string) = field "role" x :: state

        /// If the 'role' is defined the decision is based only on members with that 'role'
        member this.role value = this.Role([], value) |> this.Run

    let keep_majority =
        { new KeepMajorityBuilder<Akka.Cluster.SplitBrainResolver.Field>() with
            member _.Run(state: string list) =
                objExpr "keep-majority" 4 state |> Akka.Cluster.SplitBrainResolver.Field }

    [<AbstractClass>]
    type KeepOldestBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Enable downing of the oldest node when it is partitioned from all other nodes
        [<CustomOperation("down_if_alone"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DownIfAlone(state: string list, value: bool) =
            switchField "down-if-alone" value :: state

        /// Enable downing of the oldest node when it is partitioned from all other nodes
        member this.down_if_alone value = this.DownIfAlone([], value) |> this.Run

        /// if the 'role' is defined the decision is based only on members with that 'role',
        /// i.e. using the oldest member (singleton) within the nodes with that role
        [<CustomOperation("role"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role(state: string list, x: string) = field "role" x :: state

        /// if the 'role' is defined the decision is based only on members with that 'role',
        /// i.e. using the oldest member (singleton) within the nodes with that role
        member this.role value = this.Role([], value) |> this.Run

    let keep_oldest =
        { new KeepOldestBuilder<Akka.Cluster.SplitBrainResolver.Field>() with
            member _.Run(state: string list) =
                objExpr "keep-majority" 4 state |> Akka.Cluster.SplitBrainResolver.Field }

    [<AbstractClass>]
    type KeepRefereeBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Referee address on the form of "akka.tcp://system@hostname:port"
        [<CustomOperation("address"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Address(state: string list, x: string) =
            if isNull x || x <> "" then
                System.Uri(x) |> ignore

            quotedField "address" x :: state

        /// Referee address on the form of "akka.tcp://system@hostname:port"
        member this.address value = this.Address([], value) |> this.Run

        [<CustomOperation("down_all_if_less_than_nodes"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DownAllIfLessThanNodes(state: string list, value: int) =
            positiveField "down-all-if-less-than-nodes" value :: state

        member this.down_all_if_less_than_nodes value =
            this.DownAllIfLessThanNodes([], value) |> this.Run

    let keep_referee =
        { new KeepRefereeBuilder<Akka.Cluster.SplitBrainResolver.Field>() with
            member _.Run(state: string list) =
                objExpr "keep-referee" 4 state |> Akka.Cluster.SplitBrainResolver.Field }

    [<AbstractClass>]
    type StaticQuorumBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Minimum number of nodes that the cluster must have
        [<CustomOperation("quorum_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.QuorumSize(state: string list, value: int) =
            positiveField "quorum-size" value :: state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.QuorumSize(state: string list, value: int option) =
            match value with
            | Some i -> this.QuorumSize(state, i)
            | None -> field "quorum-size" "undefined" :: state

        /// Minimum number of nodes that the cluster must have
        member this.quorum_size(value: int) = this.QuorumSize([], value) |> this.Run
        /// Minimum number of nodes that the cluster must have
        member this.quorum_size(value: int option) = this.QuorumSize([], value) |> this.Run

        /// If the 'role' is defined the decision is based only on members with that 'role'
        [<CustomOperation("role"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role(state: string list, x: string) = field "role" x :: state

        /// If the 'role' is defined the decision is based only on members with that 'role'
        member this.role value = this.Role([], value) |> this.Run

    let static_quorum =
        { new StaticQuorumBuilder<Akka.Cluster.SplitBrainResolver.Field>() with
            member _.Run(state: string list) =
                objExpr "static-quorum" 4 state |> Akka.Cluster.SplitBrainResolver.Field }

    [<AbstractClass>]
    type SplitBrainResolverBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        member _.Yield(x: Akka.Cluster.SplitBrainResolver.Field) = [ x.ToString() ]

        /// Enable one of the available strategies (see descriptions below):
        /// static-quorum, keep-majority, keep-oldest, keep-referee
        [<CustomOperation("active_strategy"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ActiveStrategy(state: string list, value: bool) =
            switchField "active-strategy" value :: state

        /// Enable one of the available strategies (see descriptions below):
        /// static-quorum, keep-majority, keep-oldest, keep-referee
        member this.active_strategy value =
            this.ActiveStrategy([], value) |> this.Run

        /// Decision is taken by the strategy when there has been no membership or
        /// reachability changes for this duration, i.e. the cluster state is stable.
        [<CustomOperation("stable_after"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.StableAfter(state: string list, value: 'Duration) =
            durationField "stable-after" value :: state

        /// Decision is taken by the strategy when there has been no membership or
        /// reachability changes for this duration, i.e. the cluster state is stable.
        member inline this.stable_after(value: 'Duration) = this.StableAfter([], value) |> this.Run

    let split_brain_resolver =
        { new SplitBrainResolverBuilder<Akka.Cluster.Field>() with
            member _.Run(state: string list) =
                objExpr "split-brain-resolver" 3 state |> Akka.Cluster.Field }
