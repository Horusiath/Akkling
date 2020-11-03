namespace Akkling.Hocon

[<AutoOpen>]
module PubSub =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    /// Settings for the DistributedPubSub extension
    [<AbstractClass>]
    type PubSubBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        /// Actor name of the mediator actor, 
        ///
        /// Default: /system/distributedPubSubMediator
        [<CustomOperation("name");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Name (state: string list, x: string) = 
            field "name" x::state
        /// Actor name of the mediator actor, 
        ///
        /// Default: /system/distributedPubSubMediator
        member this.name value =
            this.Name([], value)
            |> this.Run
        
        /// Start the mediator on members tagged with this role.
        /// All members are used if undefined or empty.
        [<CustomOperation("role");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role (state: string list, x: string) = 
            field "role" x::state
        /// Start the mediator on members tagged with this role.
        /// All members are used if undefined or empty.
        member this.role value =
            this.Role([], value)
            |> this.Run
        
        /// The routing logic to use for 'Send'
        /// Possible values: random, round-robin, broadcast
        [<CustomOperation("routing_logic");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RoutingLogic (state: string list, x: Hocon.Router.Logic) = 
            field "routing-logic" (x.Text)::state
        /// The routing logic to use for 'Send'
        /// Possible values: random, round-robin, broadcast
        member this.routing_logic value =
            this.RoutingLogic([], value)
            |> this.Run
        
        /// How often keep-alive heartbeat messages should be sent to each connection.
        [<CustomOperation("gossip_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.GossipInterval (state: string list, value: 'Duration) =
            durationField "gossip-interval" value::state
        /// How often keep-alive heartbeat messages should be sent to each connection.
        member inline this.gossip_interval (value: 'Duration) =
            this.GossipInterval([], value)
            |> this.Run
        
        /// Removed entries are pruned after this duration
        [<CustomOperation("removed_time_to_live");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RemovedTimeToLive (state: string list, value: 'Duration) =
            durationField "removed-time-to-live" value::state
        /// Removed entries are pruned after this duration
        member inline this.removed_time_to_live (value: 'Duration) =
            this.RemovedTimeToLive([], value)
            |> this.Run
        
        /// Maximum number of elements to transfer in one message when synchronizing the registries.
        /// Next chunk will be transferred in next round of gossip.
        [<CustomOperation("max_delta_elements");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxDeltaElements (state: string list, value: int) =
            positiveField "max-delta-elements" value::state
        /// Maximum number of elements to transfer in one message when synchronizing the registries.
        /// Next chunk will be transferred in next round of gossip.
        member this.max_delta_elements value =
            this.MaxDeltaElements([], value)
            |> this.Run
        
        /// The id of the dispatcher to use for DistributedPubSubMediator actors. 
        /// If not specified default dispatcher is used.
        /// If specified you need to define the settings of the actual dispatcher.
        [<CustomOperation("use_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcher (state: string list, x: string) = 
            quotedField "use-dispatcher" x::state
        /// The id of the dispatcher to use for DistributedPubSubMediator actors. 
        /// If not specified default dispatcher is used.
        /// If specified you need to define the settings of the actual dispatcher.
        member this.use_dispatcher value =
            this.UseDispatcher([], value)
            |> this.Run
        
    /// Settings for the DistributedPubSub extension
    let pub_sub =
        { new PubSubBuilder<Akka.Cluster.Field>() with
            member _.Run (state: string list) = 
                objExpr "pub-sub" 3 state
                |> Akka.Cluster.Field }
