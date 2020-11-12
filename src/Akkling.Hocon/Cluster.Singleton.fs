namespace Akkling.Hocon

[<AutoOpen>]
module ClusterSingleton =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    [<AbstractClass>]
    type SingletonProxyBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()

        member _.Yield (x: Akka.Cluster.SplitBrainResolver.Field) = [x.ToString()]
        
        /// The actor name of the singleton actor that is started by the ClusterSingletonManager
        [<CustomOperation("singleton_name");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SingletonName (state: string list, x: string) =
            quotedField "singleton-name" x::state
        /// The actor name of the singleton actor that is started by the ClusterSingletonManager
        member this.singleton_name value =
            this.SingletonName([], value)
            |> this.Run
        
        /// The role of the cluster nodes where the singleton can be deployed. 
        /// If the role is not specified then any node will do.
        [<CustomOperation("role");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role (state: string list, x: string) =
            field "role" x::state
        /// The role of the cluster nodes where the singleton can be deployed. 
        /// If the role is not specified then any node will do.
        member this.role value =
            this.Role([], value)
            |> this.Run
        
        /// Interval at which the proxy will try to resolve the singleton instance.
        [<CustomOperation("singleton_identification_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SingletonIdentificationInterval (state: string list, value: 'Duration) =
            durationField "singleton-identification-interval" value::state
        /// Interval at which the proxy will try to resolve the singleton instance.
        member inline this.singleton_identification_interval (value: 'Duration) =
            this.SingletonIdentificationInterval([], value)
            |> this.Run
        
        /// If the location of the singleton is unknown the proxy will buffer this
        /// number of messages and deliver them when the singleton is identified. 
        /// When the buffer is full old messages will be dropped when new messages are
        /// sent via the proxy.
        ///
        /// Use 0 to disable buffering, i.e. messages will be dropped immediately if
        /// the location of the singleton is unknown.
        ///
        /// Maximum allowed buffer size is 10000.
        [<CustomOperation("buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BufferSize (state: string list, value: int) =
            let fieldName = "buffer-size"
            
            if value > 10000 then 
                failwithf "Field %s must have a value between 0 and 10000. You provided: %i" 
                    fieldName value
            else positiveField fieldName value::state
        /// If the location of the singleton is unknown the proxy will buffer this
        /// number of messages and deliver them when the singleton is identified. 
        /// When the buffer is full old messages will be dropped when new messages are
        /// sent via the proxy.
        ///
        /// Use 0 to disable buffering, i.e. messages will be dropped immediately if
        /// the location of the singleton is unknown.
        ///
        /// Maximum allowed buffer size is 10000.
        member this.buffer_size value =
            this.BufferSize([], value)
            |> this.Run
            
    let singleton_proxy =
        { new SingletonProxyBuilder<Akka.Cluster.Field>() with
            member _.Run (state: string list) = 
                objExpr "singleton-proxy" 3 state
                |> Akka.Cluster.Field }
    
    [<AbstractClass>]
    type SingletonBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()

        /// The actor name of the child singleton actor.
        [<CustomOperation("singleton_name");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SingletonName (state: string list, x: string) =
            quotedField "singleton-name" x::state
        /// The actor name of the child singleton actor.
        member this.singleton_name value =
            this.SingletonName([], value)
            |> this.Run
        
        /// Singleton among the nodes tagged with specified role.
        ///
        /// If the role is not specified it's a singleton among all nodes in the cluster.
        [<CustomOperation("role");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role (state: string list, x: string) =
            field "role" x::state
        /// Singleton among the nodes tagged with specified role.
        ///
        /// If the role is not specified it's a singleton among all nodes in the cluster.
        member this.role value =
            this.Role([], value)
            |> this.Run
        
        /// When a node is becoming oldest it sends hand-over request to previous oldest, 
        /// that might be leaving the cluster. This is retried with this interval until 
        /// the previous oldest confirms that the hand over has started or the previous 
        /// oldest member is removed from the cluster (+ akka.cluster.down-removal-margin).
        [<CustomOperation("hand_over_retry_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.HandOverRetryInterval (state: string list, value: 'Duration) =
            durationField "hand-over-retry-interval" value::state
        /// When a node is becoming oldest it sends hand-over request to previous oldest, 
        /// that might be leaving the cluster. This is retried with this interval until 
        /// the previous oldest confirms that the hand over has started or the previous 
        /// oldest member is removed from the cluster (+ akka.cluster.down-removal-margin).
        member inline this.hand_over_retry_interval (value: 'Duration) =
            this.HandOverRetryInterval([], value)
            |> this.Run
        
        /// The number of retries are derived from hand-over-retry-interval and
        /// akka.cluster.down-removal-margin (or ClusterSingletonManagerSettings.RemovalMargin),
        /// but it will never be less than this property.
        [<CustomOperation("min_number_of_hand_over_retries");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MinNumberOfHandOverRetries (state: string list, value: int) =
            positiveField "min-number-of-hand-over-retries" value::state
        /// The number of retries are derived from hand-over-retry-interval and
        /// akka.cluster.down-removal-margin (or ClusterSingletonManagerSettings.RemovalMargin),
        /// but it will never be less than this property.
        member this.min_number_of_hand_over_retries value =
            this.MinNumberOfHandOverRetries([], value)
            |> this.Run
            
    let singleton =
        { new SingletonBuilder<Akka.Cluster.Field>() with
            member _.Run (state: string list) = 
                objExpr "singleton" 3 state
                |> Akka.Cluster.Field }
