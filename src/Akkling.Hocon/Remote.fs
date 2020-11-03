namespace Akkling.Hocon

[<AutoOpen>]
module Remote =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    [<AbstractClass>]
    type RemoteBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        let indent = 2
        let pathObjExpr = pathObjExpr Akka.Field "remote" indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.Dispatcher.Field) = [x.ToString()]

        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters setting in the configuration of a
        /// transport. The available adapters should be configured in this
        /// section by providing a name, and the fully qualified name of
        /// their corresponding implementation. The class given here
        /// must implement Akka.Remote.Transport.TransportAdapterProvider
        /// and have public constructor without parameters.
        [<CustomOperation("adapters");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Adapters (state: string list, x: (string * string) list) = 
            objExpr "adapters" 3 (x |> List.map (fun (n,v) -> quotedField n v))::state
        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters setting in the configuration of a
        /// transport. The available adapters should be configured in this
        /// section by providing a name, and the fully qualified name of
        /// their corresponding implementation. The class given here
        /// must implement Akka.Remote.Transport.TransportAdapterProvider
        /// and have public constructor without parameters.
        member this.adapters value =
            this.Adapters([], value)
            |> this.Run

        /// Timeout after which the startup of the remoting subsystem is considered
        /// to be failed. Increase this value if your transport drivers (see the
        /// enabled-transports section) need longer time to be loaded.
        [<CustomOperation("startup_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.StartupTimeout (state: string list, value: 'Duration) =
            durationField "startup-timeout" value::state
        /// Timeout after which the startup of the remoting subsystem is considered
        /// to be failed. Increase this value if your transport drivers (see the
        /// enabled-transports section) need longer time to be loaded.
        member inline this.startup_timeout (value: 'Duration) =
            this.StartupTimeout([], value)
            |> this.Run
            
        /// Timout after which the graceful shutdown of the remoting subsystem is
        /// considered to be failed. After the timeout the remoting system is
        /// forcefully shut down. Increase this value if your transport drivers
        /// (see the enabled-transports section) need longer time to stop properly.
        [<CustomOperation("shutdown_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ShutdownTimeout (state: string list, value: 'Duration) =
            durationField "shutdown-timeout" value::state
        /// Timout after which the graceful shutdown of the remoting subsystem is
        /// considered to be failed. After the timeout the remoting system is
        /// forcefully shut down. Increase this value if your transport drivers
        /// (see the enabled-transports section) need longer time to stop properly.
        member inline this.shutdown_timeout (value: 'Duration) =
            this.ShutdownTimeout([], value)
            |> this.Run
            
        /// Before shutting down the drivers, the remoting subsystem attempts to flush
        /// all pending writes. This setting controls the maximum time the remoting is
        /// willing to wait before moving on to shut down the drivers.
        [<CustomOperation("flust_wait_on_shutdown");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FlushWaitOnShutdown (state: string list, value: 'Duration) =
            durationField "flush-wait-on-shutdown" value::state
        /// Before shutting down the drivers, the remoting subsystem attempts to flush
        /// all pending writes. This setting controls the maximum time the remoting is
        /// willing to wait before moving on to shut down the drivers.
        member inline this.flust_wait_on_shutdown (value: 'Duration) =
            this.FlushWaitOnShutdown([], value)
            |> this.Run

        /// Reuse inbound connections for outbound messages
        [<CustomOperation("use_passive_connections");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UsePassiveConnections (state: string list, value: bool) = 
            switchField "use-passive-connections" value::state
        /// Reuse inbound connections for outbound messages
        member this.use_passive_connections value =
            this.UsePassiveConnections([], value)
            |> this.Run
        
        /// Controls the backoff interval after a refused write is reattempted.
        /// (Transports may refuse writes if their internal buffer is full)
        [<CustomOperation("backoff_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BackoffInterval (state: string list, value: 'Duration) =
            durationField "flush-wait-on-shutdown" value::state
        /// Controls the backoff interval after a refused write is reattempted.
        /// (Transports may refuse writes if their internal buffer is full)
        member inline this.backoff_interval (value: 'Duration) =
            this.BackoffInterval([], value)
            |> this.Run
        
        /// Acknowledgment timeout of management commands sent to the transport stack.
        [<CustomOperation("command_ack_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.CommandAckTimeout (state: string list, value: 'Duration) =
            durationField "command-ack-timeout" value::state
        /// Acknowledgment timeout of management commands sent to the transport stack.
        member inline this.command_ack_timeout (value: 'Duration) =
            this.CommandAckTimeout([], value)
            |> this.Run
        
        /// The timeout for outbound associations to perform the handshake.
        /// If the transport is akka.remote.dot-netty.tcp or akka.remote.dot-netty.ssl
        /// the configured connection-timeout for the transport will be used instead.
        [<CustomOperation("handshake_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.HandshakeTimeout (state: string list, value: 'Duration) =
            durationField "handshake-timeout" value::state
        /// The timeout for outbound associations to perform the handshake.
        /// If the transport is akka.remote.dot-netty.tcp or akka.remote.dot-netty.ssl
        /// the configured connection-timeout for the transport will be used instead.
        member inline this.handshake_timeout (value: 'Duration) =
            this.HandshakeTimeout([], value)
            |> this.Run
        
        /// If set to a nonempty string remoting will use the given dispatcher for
        /// its internal actors otherwise the default dispatcher is used. Please note
        /// that since remoting can load arbitrary 3rd party drivers (see
        /// "enabled-transport" and "adapters" entries) it is not guaranteed that
        /// every module will respect this setting.
        [<CustomOperation("use_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcher (state: string list, x: string) =
            quotedField "use-dispatcher" x::state
        /// If set to a nonempty string remoting will use the given dispatcher for
        /// its internal actors otherwise the default dispatcher is used. Please note
        /// that since remoting can load arbitrary 3rd party drivers (see
        /// "enabled-transport" and "adapters" entries) it is not guaranteed that
        /// every module will respect this setting.
        member this.use_dispatcher value =
            this.UseDispatcher([], value)
            |> this.Run
            
        /// Enable untrusted mode for full security of server managed actors, prevents
        /// system messages to be send by clients, e.g. messages like 'Create',
        /// 'Suspend', 'Resume', 'Terminate', 'Supervise', 'Link' etc.
        [<CustomOperation("untrusted_mode");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UntrustedMode (state: string list, value: bool) = 
            switchField "untrusted-mode" value::state
        /// Enable untrusted mode for full security of server managed actors, prevents
        /// system messages to be send by clients, e.g. messages like 'Create',
        /// 'Suspend', 'Resume', 'Terminate', 'Supervise', 'Link' etc.
        member this.untrusted_mode value =
            this.UntrustedMode([], value)
            |> this.Run
        
        /// When 'untrusted-mode=on' inbound actor selections are by default discarded.
        /// Actors with paths defined in this white list are granted permission to receive actor
        /// selections messages. 
        ///
        /// E.g. trusted-selection-paths = ["/user/receptionist", "/user/namingService"]   
        [<CustomOperation("trusted_selection_paths");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TrustedSelectionPaths (state: string list, x: string list) = 
            quotedListField "trusted-selection-paths" x::state
        /// When 'untrusted-mode=on' inbound actor selections are by default discarded.
        /// Actors with paths defined in this white list are granted permission to receive actor
        /// selections messages. 
        ///
        /// E.g. trusted-selection-paths = ["/user/receptionist", "/user/namingService"]   
        member this.trusted_selection_paths value =
            this.TrustedSelectionPaths([], value)
            |> this.Run
            
        /// Should the remote server require that its peers share the same
        /// secure-cookie (defined in the 'remote' section)? Secure cookies are passed
        /// between during the initial handshake. Connections are refused if the initial
        /// message contains a mismatching cookie or the cookie is missing.
        [<CustomOperation("require_cookie");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.RequireCookie (state: string list, value: bool) = 
            switchField "require-cookie" value::state
        /// Should the remote server require that its peers share the same
        /// secure-cookie (defined in the 'remote' section)? Secure cookies are passed
        /// between during the initial handshake. Connections are refused if the initial
        /// message contains a mismatching cookie or the cookie is missing.
        member this.require_cookie value =
            this.RequireCookie([], value)
            |> this.Run
        
        /// Generate your own with the script availbale in
        /// '$AKKA_HOME/scripts/generate_config_with_secure_cookie.sh' or using
        /// 'akka.util.Crypt.generateSecureCookie'
        [<CustomOperation("secure_cookie");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SecureCookie (state: string list, x: string) =
            quotedField "secure-cookie" x::state
        /// Generate your own with the script availbale in
        /// '$AKKA_HOME/scripts/generate_config_with_secure_cookie.sh' or using
        /// 'akka.util.Crypt.generateSecureCookie'
        member this.secure_cookie value =
            this.SecureCookie([], value)
            |> this.Run
            
        /// If this is "on", Akka will log all inbound messages at DEBUG level,
        /// if off then they are not logged
        [<CustomOperation("log_received_messages");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogReceivedMessages (state: string list, value: bool) = 
            switchField "log-received-messages" value::state
        /// If this is "on", Akka will log all inbound messages at DEBUG level,
        /// if off then they are not logged
        member this.log_received_messages value =
            this.LogReceivedMessages([], value)
            |> this.Run
        
        /// If this is "on", Akka will log all outbound messages at DEBUG level,
        /// if off then they are not logged
        [<CustomOperation("log_sent_messages");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogSentMessages (state: string list, value: bool) = 
            switchField "log-sent-messages" value::state
        /// If this is "on", Akka will log all outbound messages at DEBUG level,
        /// if off then they are not logged
        member this.log_sent_messages value =
            this.LogSentMessages([], value)
            |> this.Run
        
        /// Sets the log granularity level at which Akka logs remoting events. This setting
        /// can take the values OFF, ERROR, WARNING, INFO, DEBUG, or ON. For compatibility
        /// reasons the setting "on" will default to "debug" level. 
        ///
        /// Please note that the effective
        /// logging level is still determined by the global logging level of the actor system:
        /// for example debug level remoting events will be only logged if the system
        /// is running with debug level logging.
        ///
        /// Failures to deserialize received messages also fall under this flag.
        [<CustomOperation("log_remote_lifecycle_events");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogRemoteLifecycleEvents (state: string list, value: bool) = 
            switchField "log-remote-lifecycle-events" value::state
        /// Sets the log granularity level at which Akka logs remoting events. This setting
        /// can take the values OFF, ERROR, WARNING, INFO, DEBUG, or ON. For compatibility
        /// reasons the setting "on" will default to "debug" level. 
        ///
        /// Please note that the effective
        /// logging level is still determined by the global logging level of the actor system:
        /// for example debug level remoting events will be only logged if the system
        /// is running with debug level logging.
        ///
        /// Failures to deserialize received messages also fall under this flag.
        member this.log_remote_lifecycle_events value =
            this.LogRemoteLifecycleEvents([], value)
            |> this.Run
        
        /// Logging of message types with payload size in bytes larger than
        /// this value. Maximum detected size per message type is logged once,
        /// with an increase threshold of 10%.
        ///
        /// By default this feature is turned off. Activate it by setting the property to
        /// a value in bytes, such as 1000b. Note that for all messages larger than this
        /// limit there will be extra performance and scalability cost.
        [<CustomOperation("log_frame_size_exceeding");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.LogFrameEizeExceeding (state: string list, value) =
            byteField "log-frame-size-exceeding" value::state
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogFrameEizeExceeding (state: string list, value: bool) = 
            if value then
                field "log-frame-size-exceeding" "off"::state
            else state
        /// Logging of message types with payload size in bytes larger than
        /// this value. Maximum detected size per message type is logged once,
        /// with an increase threshold of 10%.
        ///
        /// By default this feature is turned off. Activate it by setting the property to
        /// a value in bytes, such as 1000b. Note that for all messages larger than this
        /// limit there will be extra performance and scalability cost.
        member inline this.log_frame_size_exceeding value =
            byteField "log-frame-size-exceeding" value::[]
            |> this.Run
        /// Logging of message types with payload size in bytes larger than
        /// this value. Maximum detected size per message type is logged once,
        /// with an increase threshold of 10%.
        ///
        /// By default this feature is turned off. Activate it by setting the property to
        /// a value in bytes, such as 1000b. Note that for all messages larger than this
        /// limit there will be extra performance and scalability cost.
        member this.log_frame_size_exceeding (value: bool) =
            this.LogFrameEizeExceeding([], value)
            |> this.Run

        /// Log warning if the number of messages in the backoff buffer in the endpoint
        /// writer exceeds this limit. It can be disabled by setting the value to off.
        [<CustomOperation("log_buffer_size_exceeding");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogBufferSizeExceeding (state: string list, value) = 
            positiveField "log-buffer-size-exceeding" value::state
        /// Log warning if the number of messages in the backoff buffer in the endpoint
        /// writer exceeds this limit. It can be disabled by setting the value to off.
        member this.log_buffer_size_exceeding value =
            this.LogBufferSizeExceeding([], value)
            |> this.Run
        
        /// After failed to establish an outbound connection, the remoting will mark the
        /// address as failed. This configuration option controls how much time should
        /// be elapsed before reattempting a new connection. While the address is
        /// gated, all messages sent to the address are delivered to dead-letters.
        /// Since this setting limits the rate of reconnects setting it to a
        /// very short interval (i.e. less than a second) may result in a storm of
        /// reconnect attempts.
        [<CustomOperation("retry_gate_closed_for");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RetryGateClosedFor (state: string list, value: 'Duration) =
            durationField "retry-gate-closed-for" value::state
        /// After failed to establish an outbound connection, the remoting will mark the
        /// address as failed. This configuration option controls how much time should
        /// be elapsed before reattempting a new connection. While the address is
        /// gated, all messages sent to the address are delivered to dead-letters.
        /// Since this setting limits the rate of reconnects setting it to a
        /// very short interval (i.e. less than a second) may result in a storm of
        /// reconnect attempts.
        member inline this.retry_gate_closed_for (value: 'Duration) =
            this.RetryGateClosedFor([], value)
            |> this.Run
        
        /// After catastrophic communication failures that result in the loss of system
        /// messages or after the remote DeathWatch triggers the remote system gets
        /// quarantined to prevent inconsistent behavior.
        /// This setting controls how long the Quarantine marker will be kept around
        /// before being removed to avoid long-term memory leaks.
        ///
        /// WARNING: DO NOT change this to a small value to re-enable communication with
        /// quarantined nodes. Such feature is not supported and any behavior between
        /// the affected systems after lifting the quarantine is undefined.
        [<CustomOperation("prune_quarantine_marker_after");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PruneQuarantineMarkerAfter (state: string list, value: 'Duration) =
            durationField "prune-quarantine-marker-after" value::state
        /// After catastrophic communication failures that result in the loss of system
        /// messages or after the remote DeathWatch triggers the remote system gets
        /// quarantined to prevent inconsistent behavior.
        /// This setting controls how long the Quarantine marker will be kept around
        /// before being removed to avoid long-term memory leaks.
        ///
        /// WARNING: DO NOT change this to a small value to re-enable communication with
        /// quarantined nodes. Such feature is not supported and any behavior between
        /// the affected systems after lifting the quarantine is undefined.
        member inline this.prune_quarantine_marker_after (value: 'Duration) =
            this.PruneQuarantineMarkerAfter([], value)
            |> this.Run
        
        /// If system messages have been exchanged between two systems (i.e. remote death
        /// watch or remote deployment has been used) a remote system will be marked as
        /// quarantined after the two system has no active association, and no
        /// communication happens during the time configured here.
        ///
        /// The only purpose of this setting is to avoid storing system message redelivery
        /// data (sequence number state, etc.) for an undefined amount of time leading to long
        /// term memory leak. Instead, if a system has been gone for this period,
        /// or more exactly
        ///
        /// - there is no association between the two systems (TCP connection, if TCP transport is used)
        ///
        /// - neither side has been attempting to communicate with the other
        ///
        /// - there are no pending system messages to deliver
        ///
        /// for the amount of time configured here, the remote system will be quarantined and all state
        /// associated with it will be dropped.
        [<CustomOperation("quarantine_after_silence");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.QuarantineAfterSilence (state: string list, value: 'Duration) =
            durationField "quarantine-after-silence" value::state
        /// If system messages have been exchanged between two systems (i.e. remote death
        /// watch or remote deployment has been used) a remote system will be marked as
        /// quarantined after the two system has no active association, and no
        /// communication happens during the time configured here.
        ///
        /// The only purpose of this setting is to avoid storing system message redelivery
        /// data (sequence number state, etc.) for an undefined amount of time leading to long
        /// term memory leak. Instead, if a system has been gone for this period,
        /// or more exactly
        ///
        /// - there is no association between the two systems (TCP connection, if TCP transport is used)
        ///
        /// - neither side has been attempting to communicate with the other
        ///
        /// - there are no pending system messages to deliver
        ///
        /// for the amount of time configured here, the remote system will be quarantined and all state
        /// associated with it will be dropped.
        member inline this.quarantine_after_silence (value: 'Duration) =
            this.QuarantineAfterSilence([], value)
            |> this.Run
        
        /// This setting defines the maximum number of unacknowledged system messages
        /// allowed for a remote system. If this limit is reached the remote system is
        /// declared to be dead and its UID marked as tainted.
        [<CustomOperation("system_message_buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SystemMessageBufferSize (state: string list, value) =
            positiveField "system-message-buffer-size" value::state
        /// This setting defines the maximum number of unacknowledged system messages
        /// allowed for a remote system. If this limit is reached the remote system is
        /// declared to be dead and its UID marked as tainted.
        member inline this.system_message_buffer_size value =
            this.SystemMessageBufferSize([], value)
            |> this.Run
        
        /// This setting defines the maximum idle time after an individual
        /// acknowledgement for system messages is sent. System message delivery
        /// is guaranteed by explicit acknowledgement messages. These acks are
        /// piggybacked on ordinary traffic messages. If no traffic is detected
        /// during the time period configured here, the remoting will send out
        /// an individual ack.
        [<CustomOperation("system_message_ack_piggyback_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SystemMessageAckPiggybackTimeout (state: string list, value: 'Duration) =
            durationField "system-message-ack-piggyback-timeout" value::state
        /// This setting defines the maximum idle time after an individual
        /// acknowledgement for system messages is sent. System message delivery
        /// is guaranteed by explicit acknowledgement messages. These acks are
        /// piggybacked on ordinary traffic messages. If no traffic is detected
        /// during the time period configured here, the remoting will send out
        /// an individual ack.
        member inline this.system_message_ack_piggyback_timeout (value: 'Duration) =
            this.SystemMessageAckPiggybackTimeout([], value)
            |> this.Run
        
        /// This setting defines the time after internal management signals
        /// between actors (used for DeathWatch and supervision) that have not been
        /// explicitly acknowledged or negatively acknowledged are resent.
        /// Messages that were negatively acknowledged are always immediately
        /// resent.
        [<CustomOperation("resend_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ResendInterval (state: string list, value: 'Duration) =
            durationField "resend-interval" value::state
        /// This setting defines the time after internal management signals
        /// between actors (used for DeathWatch and supervision) that have not been
        /// explicitly acknowledged or negatively acknowledged are resent.
        /// Messages that were negatively acknowledged are always immediately
        /// resent.
        member inline this.resend_interval (value: 'Duration) =
            this.ResendInterval([], value)
            |> this.Run
        
        /// Maximum number of unacknowledged system messages that will be resent
        /// each 'resend-interval'. If you watch many (> 1000) remote actors you can
        /// increase this value to for example 600, but a too large limit (e.g. 10000)
        /// may flood the connection and might cause false failure detection to trigger.
        /// Test such a configuration by watching all actors at the same time and stop
        /// all watched actors at the same time.
        [<CustomOperation("resend_limit");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ResendLimit (state: string list, value) =
            positiveField "resend-limit" value::state
        /// Maximum number of unacknowledged system messages that will be resent
        /// each 'resend-interval'. If you watch many (> 1000) remote actors you can
        /// increase this value to for example 600, but a too large limit (e.g. 10000)
        /// may flood the connection and might cause false failure detection to trigger.
        /// Test such a configuration by watching all actors at the same time and stop
        /// all watched actors at the same time.
        member inline this.resend_limit value =
            this.ResendLimit([], value)
            |> this.Run
        
        /// WARNING: this setting should not be not changed unless all of its consequences
        /// are properly understood which assumes experience with remoting internals
        /// or expert advice.
        ///
        /// This setting defines the time after redelivery attempts of internal management
        /// signals are stopped to a remote system that has been not confirmed to be alive by
        /// this system before.
        [<CustomOperation("initial_system_message_delivery_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.InitialSystemMessageDeliveryTimeout (state: string list, value: 'Duration) =
            durationField "initial-system-message-delivery-timeout" value::state
        /// WARNING: this setting should not be not changed unless all of its consequences
        /// are properly understood which assumes experience with remoting internals
        /// or expert advice.
        ///
        /// This setting defines the time after redelivery attempts of internal management
        /// signals are stopped to a remote system that has been not confirmed to be alive by
        /// this system before.
        member inline this.initial_system_message_delivery_timeout (value: 'Duration) =
            this.InitialSystemMessageDeliveryTimeout([], value)
            |> this.Run
        
        /// List of the transport drivers that will be loaded by the remoting.
        ///
        /// A list of fully qualified config paths must be provided where
        /// the given configuration path contains a transport-class key
        /// pointing to an implementation class of the Transport interface.
        ///
        /// If multiple transports are provided, the address of the first
        /// one will be used as a default address.
        [<CustomOperation("enabled_transports");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnabledTransports (state: string list, x: string list) = 
            quotedListField "enabled-transports" x::state
        /// List of the transport drivers that will be loaded by the remoting.
        ///
        /// A list of fully qualified config paths must be provided where
        /// the given configuration path contains a transport-class key
        /// pointing to an implementation class of the Transport interface.
        ///
        /// If multiple transports are provided, the address of the first
        /// one will be used as a default address.
        member this.enabled_transports value =
            this.EnabledTransports([], value)
            |> this.Run
            
        // Pathing
        
        /// For TCP it is not important to have fast failure detection, since
        /// most connection failures are captured by TCP itself.
        /// The default DeadlineFailureDetector will trigger if there are no heartbeats within
        /// the duration heartbeat-interval + acceptable-heartbeat-pause, i.e. 20 seconds
        /// the duration heartbeat-interval + acceptable-heartbeat-pause, i.e. 124 seconds
        member _.transport_failure_detector =
            { new TransportFailureDetectorBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "transport-failure-detector" state }
                
        /// Settings for the Phi accrual failure detector (http://ddg.jaist.ac.jp/pub/HDY+04.pdf
        /// [Hayashibara et al]) used for remote death watch.
        member _.watch_failure_detector =
            { new WatchFailureDetectorBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "watch-failure-detector" state }
                
        member _.gremlin = 
            { new GremlinBuilder<Akka.Field>() with
                member _.Run (state: string list) = pathObjExpr "gremlin" state }
        
        member _.dot_netty = 
            { new DotNettyBuilder<Akka.Field>(Akka.Field, "remote.dot-netty", indent) with
                member _.Run (state: string list) = pathObjExpr "dot-netty" state }

    let remote =
        { new RemoteBuilder<Akka.Field>() with
            member _.Run (state: string list) = 
                objExpr "remote" 2 state
                |> Akka.Field }
