namespace Akkling.Hocon

[<AutoOpen>]
module Serializers =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    type Serializers =
        /// akka-cluster
        static member akka_cluster = "akka-cluster"
        
        /// akka-cluster-client
        static member akka_cluster_client = "akka-cluster-client"
        
        /// akka-cluster-metrics
        static member akka_cluster_metrics = "akka-cluster-metrics"

        /// akka-containers
        static member akka_containers = "akka-containers"

        /// akka-misc
        static member akka_misc = "akka-misc"

        /// akka-persistence-message
        static member akka_persistence_message = "akka-persistence-message"
        
        /// akka-persistence-snapshot
        static member akka_persistence_snapshot = "akka-persistence-snapshot"

        /// akka-stream-ref
        static member akka_stream_ref = "akka-stream-ref"
        
        /// akka-system-msg
        static member akka_system_msg = "akka-system-msg"

        /// bytes
        static member bytes = "bytes"
        
        /// daemon-create
        static member daemon_create = "daemon-create"
        
        /// hyperion
        static member hyperion = "hyperion"

        /// json
        static member json = "json"
        
        /// primative
        static member primitive = "primitive"

        /// proto
        static member proto = "proto"

    [<AbstractClass>]
    type SerializersBuilder<'T when 'T :> MarkerClasses.IField> () =
        inherit BaseBuilder<'T>()
        
        [<CustomOperation("akka_cluster");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaCluster (state: string list, value: string) =
            quotedField Serializers.akka_cluster value::state
        member this.akka_cluster value =
            this.AkkaCluster([], value)
            |> this.Run
        
        [<CustomOperation("akka_cluster_client");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaClusterClient (state: string list, value: string) =
            quotedField Serializers.akka_cluster_client value::state
        member this.akka_cluster_client value =
            this.AkkaClusterClient([], value)
            |> this.Run
        
        [<CustomOperation("akka_cluster_metrics");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaClusterMetrics (state: string list, value: string) =
            quotedField Serializers.akka_cluster_metrics value::state
        member this.akka_cluster_metrics value =
            this.AkkaClusterClient([], value)
            |> this.Run
        
        [<CustomOperation("akka_containers");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaContainers (state: string list, value: string) =
            quotedField Serializers.akka_containers value::state
        member this.akka_containers value =
            this.AkkaContainers([], value)
            |> this.Run
        
        [<CustomOperation("akka_misc");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaMisc (state: string list, value: string) =
            quotedField Serializers.akka_misc value::state
        member this.akka_misc value =
            this.AkkaMisc([], value)
            |> this.Run
        
        [<CustomOperation("akka_persistence_message");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaPersistenceMessage (state: string list, value: string) =
            quotedField Serializers.akka_persistence_message value::state
        member this.akka_persistence_message value =
            this.AkkaPersistenceMessage([], value)
            |> this.Run
        
        [<CustomOperation("akka_persistence_snapshot");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaPersistenceSnapshot (state: string list, value: string) =
            quotedField Serializers.akka_persistence_snapshot value::state
        member this.akka_persistence_snapshot value =
            this.AkkaPersistenceSnapshot([], value)
            |> this.Run
            
        [<CustomOperation("akka_stream_ref");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaStreamRef (state: string list, value: string) =
            quotedField Serializers.akka_stream_ref value::state
        member this.akka_stream_ref value =
            this.AkkaStreamRef([], value)
            |> this.Run
            
        [<CustomOperation("akka_system_msg");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AkkaSystemMessage (state: string list, value: string) =
            quotedField Serializers.akka_system_msg value::state
        member this.akka_system_msg value =
            this.AkkaSystemMessage([], value)
            |> this.Run
            
        [<CustomOperation("bytes");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Bytes (state: string list, value: string) =
            quotedField Serializers.bytes value::state
        member this.bytes value =
            this.Bytes([], value)
            |> this.Run
            
        [<CustomOperation("daemon_create");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DaemonCreate (state: string list, value: string) =
            quotedField Serializers.daemon_create value::state
        member this.daemon_create value =
            this.DaemonCreate([], value)
            |> this.Run
            
        [<CustomOperation("hyperion");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Hyperion (state: string list, value: string) =
            quotedField Serializers.hyperion value::state
        member this.hyperion value =
            this.Hyperion([], value)
            |> this.Run
            
        [<CustomOperation("json");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Json (state: string list, value: string) =
            quotedField Serializers.json value::state
        member this.json value =
            this.Json([], value)
            |> this.Run
            
        [<CustomOperation("primitive");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Primitive (state: string list, value: string) =
            quotedField Serializers.primitive value::state
        member this.primitive value =
            this.Primitive([], value)
            |> this.Run
            
        [<CustomOperation("proto");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Proto (state: string list, value: string) =
            quotedField Serializers.proto value::state
        member this.proto value =
            this.Proto([], value)
            |> this.Run

    let serializers = 
        { new SerializersBuilder<Akka.Actor.Field>() with
            member _.Run (state: string list) =
                objExpr "serializers" 3 state
                |> Akka.Actor.Field }

    let serialization_settings = 
        { new BaseBuilder<Akka.Actor.Field>() with
            member _.Run (state: string list) =
                objExpr "serialization-settings" 3 state
                |> Akka.Actor.Field }
