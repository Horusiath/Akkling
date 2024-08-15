namespace Akkling.Hocon

[<AutoOpen>]
module Router =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type TypeMappingBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<CustomOperation("from_code"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.FromCode(state: string list, x: string) = quotedField "from-code" x :: state

        member this.from_code value = this.FromCode([], value) |> this.Run

        [<CustomOperation("round_robin_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RoundRobinPool(state: string list, x: string) =
            quotedField "round-robin-pool" x :: state

        member this.round_robin_pool value =
            this.RoundRobinPool([], value) |> this.Run

        [<CustomOperation("round_robin_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RoundRobinGroup(state: string list, x: string) =
            quotedField "round-robin-group" x :: state

        member this.round_robin_group value =
            this.RoundRobinGroup([], value) |> this.Run

        [<CustomOperation("random_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RandomPool(state: string list, x: string) = quotedField "random-pool" x :: state

        member this.random_pool value = this.RandomPool([], value) |> this.Run

        [<CustomOperation("random_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RandomGroup(state: string list, x: string) = quotedField "random-group" x :: state

        member this.random_group value = this.RandomGroup([], value) |> this.Run

        [<CustomOperation("smallest_mailbox_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SmallestMailboxPool(state: string list, x: string) =
            quotedField "smallest-mailbox-pool" x :: state

        member this.smallest_mailbox_pool value =
            this.SmallestMailboxPool([], value) |> this.Run

        [<CustomOperation("broadcast_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BroadcastPool(state: string list, x: string) = quotedField "broadcast-pool" x :: state

        member this.broadcast_pool value =
            this.BroadcastPool([], value) |> this.Run

        [<CustomOperation("broadcast_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BroadcastGroup(state: string list, x: string) =
            quotedField "broadcast-group" x :: state

        member this.broadcast_group value =
            this.BroadcastGroup([], value) |> this.Run

        [<CustomOperation("scatter_gather_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ScatterGatherPool(state: string list, x: string) =
            quotedField "scatter-gather-pool" x :: state

        member this.scatter_gather_pool value =
            this.ScatterGatherPool([], value) |> this.Run

        [<CustomOperation("scatter_gather_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ScatterGatherGroup(state: string list, x: string) =
            quotedField "scatter-gather-group" x :: state

        member this.scatter_gather_group value =
            this.ScatterGatherGroup([], value) |> this.Run

        [<CustomOperation("consistent_hashing_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ConsistentHashingPool(state: string list, x: string) =
            quotedField "consistent-hashing-pool" x :: state

        member this.consistent_hashing_pool value =
            this.ConsistentHashingPool([], value) |> this.Run

        [<CustomOperation("consistent_hashing_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ConsistentHashingGroup(state: string list, x: string) =
            quotedField "consistent-hashing-group" x :: state

        member this.consistent_hashing_group value =
            this.ConsistentHashingGroup([], value) |> this.Run

        [<CustomOperation("tail_chopping_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TailChoppingPool(state: string list, x: string) =
            quotedField "tail-chopping-pool" x :: state

        member this.tail_chopping_pool value =
            this.TailChoppingPool([], value) |> this.Run

        [<CustomOperation("tail_chopping_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TailChoppingGroup(state: string list, x: string) =
            quotedField "tail-chopping-group" x :: state

        member this.tail_chopping_group value =
            this.TailChoppingGroup([], value) |> this.Run

        [<CustomOperation("cluster_metrics_adaptive_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ClusterMetricsAdaptivePool(state: string list, x: string) =
            quotedField "cluster-metrics-adaptive-pool" x :: state

        member this.cluster_metrics_adaptive_pool value =
            this.ClusterMetricsAdaptivePool([], value) |> this.Run

        [<CustomOperation("cluster_metrics_adaptive_group"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ClusterMetricsAdaptiveGroup(state: string list, x: string) =
            quotedField "cluster-metrics-adaptive-group" x :: state

        member this.cluster_metrics_adaptive_group value =
            this.ClusterMetricsAdaptiveGroup([], value) |> this.Run

    let type_mapping =
        { new TypeMappingBuilder<Akka.Actor.Router.Field>() with
            member _.Run(state: string list) =
                objExpr "type-mapping" 4 state |> Akka.Actor.Router.Field }

    [<AbstractClass>]
    type RouterBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()

        let pathObjExpr = pathObjExpr mkField path indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Actor.Router.Field) = [ x.ToString() ]

        // Pathing

        member _.type_mapping =
            { new TypeMappingBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr "type-mapping" state }

    let router =
        let path = "router"
        let indent = 3

        { new RouterBuilder<Akka.Actor.Field>(Akka.Actor.Field, path, indent) with
            member _.Run(state: string list) =
                objExpr path indent state |> Akka.Actor.Field }
