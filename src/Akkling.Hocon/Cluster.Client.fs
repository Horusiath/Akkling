namespace Akkling.Hocon

[<AutoOpen>]
module Client =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type ReceptionistBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        /// Actor name of the ClusterReceptionist actor, /system/receptionist
        [<CustomOperation("name");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Name (state: string list, x: string) =
            field "name" x::state
        /// Actor name of the ClusterReceptionist actor, /system/receptionist
        member this.name value =
            this.Name([], value)
            |> this.Run
    
        /// Start the receptionist on members tagged with this role.
        /// All members are used if undefined or empty.
        [<CustomOperation("role");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Role (state: string list, x: string) =
            field "role" x::state
        /// Start the receptionist on members tagged with this role.
        /// All members are used if undefined or empty.
        member this.role value =
            this.Role([], value)
            |> this.Run
        
        /// The receptionist will send this number of contact points to the client
        [<CustomOperation("number_of_contacts");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.NumberOfContacts (state: string list, value: int) =
            positiveField "number-of-contacts" value::state
        /// The receptionist will send this number of contact points to the client
        member this.number_of_contacts value =
            this.NumberOfContacts([], value)
            |> this.Run
        
        /// The actor that tunnel response messages to the client will be stopped
        /// after this time of inactivity.
        [<CustomOperation("response_tunnel_receive_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ResponseTunnelReceiveTimeout (state: string list, value: 'Duration) =
            durationField "response-tunnel-receive-timeout" value::state
        /// The actor that tunnel response messages to the client will be stopped
        /// after this time of inactivity.
        member inline this.response_tunnel_receive_timeout (value: 'Duration) =
            this.ResponseTunnelReceiveTimeout([], value)
            |> this.Run
        
        /// The id of the dispatcher to use for ClusterReceptionist actors. 
        /// If not specified, the internal dispatcher is used.
        /// If specified you need to define the settings of the actual dispatcher.
        [<CustomOperation("use_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcher (state: string list, x: string) =
            quotedField "use-dispatcher" x::state
        /// The id of the dispatcher to use for ClusterReceptionist actors. 
        /// If not specified, the internal dispatcher is used.
        /// If specified you need to define the settings of the actual dispatcher.
        member this.use_dispatcher value =
            this.UseDispatcher([], value)
            |> this.Run
        
        /// How often failure detection heartbeat messages should be received for
        /// each ClusterClient
        [<CustomOperation("heartbeat_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.HeartbeatInterval (state: string list, value: 'Duration) =
            durationField "heartbeat-interval" value::state
        /// How often failure detection heartbeat messages should be received for
        /// each ClusterClient
        member inline this.heartbeat_interval (value: 'Duration) =
            this.HeartbeatInterval([], value)
            |> this.Run
        
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        /// The ClusterReceptionist is using the akka.remote.DeadlineFailureDetector, which
        /// will trigger if there are no heartbeats within the duration
        /// heartbeat-interval + acceptable-heartbeat-pause, i.e. 15 seconds with
        /// the default settings.
        [<CustomOperation("acceptable_heartbeat_pause");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.AcceptableHeartbeatPause (state: string list, value: 'Duration) =
            durationField "acceptable-heartbeat-pause" value::state
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        /// The ClusterReceptionist is using the akka.remote.DeadlineFailureDetector, which
        /// will trigger if there are no heartbeats within the duration
        /// heartbeat-interval + acceptable-heartbeat-pause, i.e. 15 seconds with
        /// the default settings.
        member inline this.acceptable_heartbeat_pause (value: 'Duration) =
            this.AcceptableHeartbeatPause([], value)
            |> this.Run
        
        /// Failure detection checking interval for checking all ClusterClients
        [<CustomOperation("failure_detection_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FailureDetectionInterval (state: string list, value: 'Duration) =
            durationField "failure-detection-interval" value::state
        /// Failure detection checking interval for checking all ClusterClients
        member inline this.failure_detection_interval (value: 'Duration) =
            this.FailureDetectionInterval([], value)
            |> this.Run
        
    /// Settings for the ClusterClientReceptionist extension
    let receptionist =
        { new ReceptionistBuilder<Akka.Cluster.Client.Field>() with
            member _.Run (state: string list) = 
                objExpr "receptionist" 4 state
                |> Akka.Cluster.Client.Field }

    [<AbstractClass>]
    type ClientBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Cluster.Client.Field) = [x.ToString()]

        /// Actor paths of the ClusterReceptionist actors on the servers (cluster nodes)
        /// that the client will try to contact initially. It is mandatory to specify
        /// at least one initial contact. 
        ///
        /// Comma separated full actor paths defined by a string on the form of
        /// "akka.tcp://system@hostname:port/system/receptionist"
        [<CustomOperation("initial_contacts");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.InitialContacts (state: string list, xs: string list) =
            xs |> List.map System.Uri |> ignore
            quotedListField "initial-contacts" xs::state
        /// Actor paths of the ClusterReceptionist actors on the servers (cluster nodes)
        /// that the client will try to contact initially. It is mandatory to specify
        /// at least one initial contact. 
        ///
        /// Comma separated full actor paths defined by a string on the form of
        /// "akka.tcp://system@hostname:port/system/receptionist"
        member this.initial_contacts value =
            this.InitialContacts([], value)
            |> this.Run
        
        /// Interval at which the client retries to establish contact with one of 
        /// ClusterReceptionist on the servers (cluster nodes)
        [<CustomOperation("establishing_get_contacts_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.EstablishingGetContactsInterval (state: string list, value: 'Duration) =
            durationField "establishing-get-contacts-interval" value::state
        /// Interval at which the client retries to establish contact with one of 
        /// ClusterReceptionist on the servers (cluster nodes)
        member inline this.establishing_get_contacts_interval (value: 'Duration) =
            this.EstablishingGetContactsInterval([], value)
            |> this.Run
        
        /// Interval at which the client will ask the ClusterReceptionist for
        /// new contact points to be used for next reconnect.
        [<CustomOperation("refresh_contacts_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RefreshContactsInterval (state: string list, value: 'Duration) =
            durationField "refresh-contacts-interval" value::state
        /// Interval at which the client will ask the ClusterReceptionist for
        /// new contact points to be used for next reconnect.
        member inline this.refresh_contacts_interval (value: 'Duration) =
            this.RefreshContactsInterval([], value)
            |> this.Run
        
        /// How often failure detection heartbeat messages should be sent
        [<CustomOperation("heartbeat_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.HeartbeatInterval (state: string list, value: 'Duration) =
            durationField "heartbeat-interval" value::state
        /// How often failure detection heartbeat messages should be sent
        member inline this.heartbeat_interval (value: 'Duration) =
            this.HeartbeatInterval([], value)
            |> this.Run
        
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        ///
        /// The ClusterClient is using the akka.remote.DeadlineFailureDetector, which
        /// will trigger if there are no heartbeats within the duration 
        /// heartbeat-interval + acceptable-heartbeat-pause, i.e. 15 seconds with
        /// the default settings.
        [<CustomOperation("acceptable_heartbeat_pause");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.AcceptableHeartbeatPause (state: string list, value: 'Duration) =
            durationField "acceptable-heartbeat-pause" value::state
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        ///
        /// The ClusterClient is using the akka.remote.DeadlineFailureDetector, which
        /// will trigger if there are no heartbeats within the duration 
        /// heartbeat-interval + acceptable-heartbeat-pause, i.e. 15 seconds with
        /// the default settings.
        member inline this.acceptable_heartbeat_pause (value: 'Duration) =
            this.AcceptableHeartbeatPause([], value)
            |> this.Run
        
        /// If connection to the receptionist is not established the client will buffer
        /// this number of messages and deliver them the connection is established.
        ///
        /// When the buffer is full old messages will be dropped when new messages are sent
        /// via the client. Use 0 to disable buffering, i.e. messages will be dropped
        /// immediately if the location of the singleton is unknown.
        ///
        /// Maximum allowed buffer size is 10000.
        [<CustomOperation("buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BufferSize (state: string list, value: int) =
            if value < 10000 then
                positiveField "buffer-size" value::state
            else failwithf "buffer-size must be value less than 10000, was given: %i" value
        /// If connection to the receptionist is not established the client will buffer
        /// this number of messages and deliver them the connection is established.
        ///
        /// When the buffer is full old messages will be dropped when new messages are sent
        /// via the client. Use 0 to disable buffering, i.e. messages will be dropped
        /// immediately if the location of the singleton is unknown.
        ///
        /// Maximum allowed buffer size is 10000.
        member this.buffer_size value =
            this.BufferSize([], value)
            |> this.Run
        
        /// If connection to the receiptionist is lost and the client has not been
        /// able to acquire a new connection for this long the client will stop itself.
        /// This duration makes it possible to watch the cluster client and react on a more permanent
        /// loss of connection with the cluster, for example by accessing some kind of
        /// service registry for an updated set of initial contacts to start a new cluster client with.
        ///
        /// If this is not wanted it can be set to "off" to disable the timeout and retry
        /// forever.
        [<CustomOperation("reconnect_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ReconnectTimeout (state: string list, value: bool) = 
            switchField "reconnect-timeout" value::state
        /// If connection to the receiptionist is lost and the client has not been
        /// able to acquire a new connection for this long the client will stop itself.
        /// This duration makes it possible to watch the cluster client and react on a more permanent
        /// loss of connection with the cluster, for example by accessing some kind of
        /// service registry for an updated set of initial contacts to start a new cluster client with.
        ///
        /// If this is not wanted it can be set to "off" to disable the timeout and retry
        /// forever.
        member this.reconnect_timeout value =
            this.ReconnectTimeout([], value)
            |> this.Run
        
    /// Settings for the ClusterClient
    let client =
        { new ClientBuilder<Akka.Cluster.Field>() with
            member _.Run (state: string list) = 
                objExpr "client" 3 state
                |> Akka.Cluster.Field }
