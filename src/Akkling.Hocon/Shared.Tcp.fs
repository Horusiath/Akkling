namespace Akkling.Hocon

[<AutoOpen>]
module Tcp =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    module Tcp =
        [<RequireQualifiedAccess>]
        type Target =
            | Any
            | IO
            | Remote

        [<RequireQualifiedAccess>]
        module Target =
            let merge (target1: Target) (target2: Target) =
                match target1, target2 with
                | Target.Any, _
                | _, Target.Any -> Some target1
                | _, _ when target1 = target2 -> Some target1
                | _ -> None

        [<RequireQualifiedAccess>]
        module State =
            let create = State.create Target.Any

    [<AbstractClass>]
    type TcpBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string) =
        let pathObjExpr = pathObjExpr mkField path

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        abstract Run: State<Tcp.Target> -> Akka.Shared.Field<'T>

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: string) = Tcp.State.create [ x ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.IO.DisabledBufferPool.Field) =
            State.create Tcp.Target.IO [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.IO.DirectBufferPool.Field) =
            State.create Tcp.Target.IO [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Remote.Batching.Field) =
            State.create Tcp.Target.Remote [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Remote.WorkerPool.Field) =
            State.create Tcp.Target.Remote [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(a: unit) = Tcp.State.create []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine(state1: State<Tcp.Target>, state2: State<Tcp.Target>) : State<Tcp.Target> =
            let fields = state1.Fields @ state2.Fields

            match Tcp.Target.merge state1.Target state2.Target with
            | Some target -> { Target = target; Fields = fields }
            | None -> failwithf "Tcp fields mismatch found was given:%s%A" System.Environment.NewLine fields

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero() = Tcp.State.create []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f ()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.For(state: State<Tcp.Target>, f: unit -> State<Tcp.Target>) = this.Combine(state, f ())

        // Akka.IO

        /// A buffer pool used to acquire and release byte buffers from the managed
        /// heap. Once byte buffer is no longer needed is can be released, landing
        /// on the pool again, to be reused later. This way we can reduce a GC pressure
        /// by reusing the same components instead of recycling them.
        ///
        /// NOTE: pooling is disabled by default, to enable use direct-buffer-pool:
        /// set this field to "akka.io.tcp.direct-buffer-pool"
        [<CustomOperation("buffer_pool"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BufferPool(state: State<Tcp.Target>, value: string) =
            quotedField "buffer-pool" value |> State.addWith state Tcp.Target.IO

        /// A buffer pool used to acquire and release byte buffers from the managed
        /// heap. Once byte buffer is no longer needed is can be released, landing
        /// on the pool again, to be reused later. This way we can reduce a GC pressure
        /// by reusing the same components instead of recycling them.
        ///
        /// NOTE: pooling is disabled by default, to enable use direct-buffer-pool:
        /// set this field to "akka.io.tcp.direct-buffer-pool"
        member this.buffer_pool value =
            this.BufferPool(State.create Tcp.Target.IO [], value) |> this.Run

        /// Maximum number of open channels supported by this TCP module; there is
        /// no intrinsic general limit, this setting is meant to enable DoS
        /// protection by limiting the number of concurrently connected clients.
        /// Also note that this is a "soft" limit; in certain cases the implementation
        /// will accept a few connections more or a few less than the number configured
        /// here. Must be an integer > 0 or "unlimited".
        [<CustomOperation("max_channels"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaxChannels(state: State<Tcp.Target>, value) =
            positiveField "max-channels" value |> State.addWith state Tcp.Target.IO

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxChannels(state: State<Tcp.Target>, value: Hocon.Unlimited) =
            quotedField "max-channels" (value.ToString())
            |> State.addWith state Tcp.Target.IO

        /// Maximum number of open channels supported by this TCP module; there is
        /// no intrinsic general limit, this setting is meant to enable DoS
        /// protection by limiting the number of concurrently connected clients.
        /// Also note that this is a "soft" limit; in certain cases the implementation
        /// will accept a few connections more or a few less than the number configured
        /// here. Must be an integer > 0 or "unlimited".
        member inline this.max_channels value =
            positiveField "max-channels" value
            |> State.add (State.create Tcp.Target.IO [])
            |> this.Run

        /// Maximum number of open channels supported by this TCP module; there is
        /// no intrinsic general limit, this setting is meant to enable DoS
        /// protection by limiting the number of concurrently connected clients.
        /// Also note that this is a "soft" limit; in certain cases the implementation
        /// will accept a few connections more or a few less than the number configured
        /// here. Must be an integer > 0 or "unlimited".
        member this.max_channels(value: Hocon.Unlimited) =
            this.MaxChannels(State.create Tcp.Target.IO [], value) |> this.Run

        /// When trying to assign a new connection to a selector and the chosen
        /// selector is at full capacity, retry selector choosing and assignment
        /// this many times before giving up
        [<CustomOperation("selector_association_retries"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SelectorAssociationRetries(state: State<Tcp.Target>, value) =
            positiveField "selector-association-retries" value
            |> State.addWith state Tcp.Target.IO

        /// When trying to assign a new connection to a selector and the chosen
        /// selector is at full capacity, retry selector choosing and assignment
        /// this many times before giving up
        member inline this.selector_association_retries value =
            this.SelectorAssociationRetries(State.create Tcp.Target.IO [], value)
            |> this.Run

        /// The maximum number of connection that are accepted in one go,
        /// higher numbers decrease latency, lower numbers increase fairness on
        /// the worker-dispatcher
        [<CustomOperation("batch_accept_limit"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BatchAcceptLimit(state: State<Tcp.Target>, value) =
            positiveField "batch-accept-limit" value |> State.addWith state Tcp.Target.IO

        /// The maximum number of connection that are accepted in one go,
        /// higher numbers decrease latency, lower numbers increase fairness on
        /// the worker-dispatcher
        member inline this.batch_accept_limit value =
            this.BatchAcceptLimit(State.create Tcp.Target.IO [], value) |> this.Run

        /// The duration a connection actor waits for a `Register` message from
        /// its commander before aborting the connection.
        [<CustomOperation("register_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.RegisterTimeout(state: State<Tcp.Target>, value: 'Duration) =
            durationField "register-timeout" value |> State.addWith state Tcp.Target.IO

        /// The duration a connection actor waits for a `Register` message from
        /// its commander before aborting the connection.
        member inline this.register_timeout(value: 'Duration) =
            this.RegisterTimeout(State.create Tcp.Target.IO [], value) |> this.Run

        /// The maximum number of bytes delivered by a `Received` message. Before
        /// more data is read from the network the connection actor will try to
        /// do other work.
        ///
        /// The purpose of this setting is to impose a smaller limit than the
        /// configured receive buffer size. When using value 'unlimited' it will
        /// try to read all from the receive buffer.
        [<CustomOperation("max_received_message_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaxReceivedMessageSize(state: State<Tcp.Target>, value) =
            positiveField "max-received-message-size" value
            |> State.addWith state Tcp.Target.IO

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxReceivedMessageSize(state: State<Tcp.Target>, value: Hocon.Unlimited) =
            quotedField "max-received-message-size" (value.ToString())
            |> State.addWith state Tcp.Target.IO

        /// The maximum number of bytes delivered by a `Received` message. Before
        /// more data is read from the network the connection actor will try to
        /// do other work.
        ///
        /// The purpose of this setting is to impose a smaller limit than the
        /// configured receive buffer size. When using value 'unlimited' it will
        /// try to read all from the receive buffer.
        member inline this.max_received_message_size value =
            positiveField "max-received-message-size" value
            |> State.add (State.create Tcp.Target.IO [])
            |> this.Run

        /// The maximum number of bytes delivered by a `Received` message. Before
        /// more data is read from the network the connection actor will try to
        /// do other work.
        ///
        /// The purpose of this setting is to impose a smaller limit than the
        /// configured receive buffer size. When using value 'unlimited' it will
        /// try to read all from the receive buffer.
        member this.max_received_message_size(value: Hocon.Unlimited) =
            this.MaxReceivedMessageSize(State.create Tcp.Target.IO [], value) |> this.Run

        /// Enable fine grained logging of what goes on inside the implementation.
        /// Be aware that this may log more than once per message sent to the actors
        /// of the tcp implementation.
        [<CustomOperation("trace_logging"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TraceLogging(state: State<Tcp.Target>, value: bool) =
            switchField "trace-logging" value |> State.addWith state Tcp.Target.IO

        /// Enable fine grained logging of what goes on inside the implementation.
        /// Be aware that this may log more than once per message sent to the actors
        /// of the tcp implementation.
        member this.trace_logging value =
            this.TraceLogging(State.create Tcp.Target.IO [], value) |> this.Run

        /// Fully qualified config path which holds the dispatcher configuration
        /// to be used for running the select() calls in the selectors
        [<CustomOperation("selector_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SelectorDispatcher(state: State<Tcp.Target>, value: string) =
            quotedField "selector-dispatcher" value |> State.addWith state Tcp.Target.IO

        /// Fully qualified config path which holds the dispatcher configuration
        /// to be used for running the select() calls in the selectors
        member this.selector_dispatcher value =
            this.SelectorDispatcher(State.create Tcp.Target.IO [], value) |> this.Run

        /// Fully qualified config path which holds the dispatcher configuration
        /// for the read/write worker actors
        [<CustomOperation("worker_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.WorkerDispatcher(state: State<Tcp.Target>, value: string) =
            quotedField "worker-dispatcher" value |> State.addWith state Tcp.Target.IO

        /// Fully qualified config path which holds the dispatcher configuration
        /// for the read/write worker actors
        member this.worker_dispatcher value =
            this.WorkerDispatcher(State.create Tcp.Target.IO [], value) |> this.Run

        /// Fully qualified config path which holds the dispatcher configuration
        /// for the selector management actors
        [<CustomOperation("management_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ManagementDispatcher(state: State<Tcp.Target>, value: string) =
            quotedField "management-dispatcher" value |> State.addWith state Tcp.Target.IO

        /// Fully qualified config path which holds the dispatcher configuration
        /// for the selector management actors
        member this.management_dispatcher value =
            this.ManagementDispatcher(State.create Tcp.Target.IO [], value) |> this.Run

        /// Fully qualified config path which holds the dispatcher configuration
        /// on which file IO tasks are scheduled
        [<CustomOperation("file_io_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.FileIODispatcher(state: State<Tcp.Target>, value: string) =
            quotedField "file-io-dispatcher" value |> State.addWith state Tcp.Target.IO

        /// Fully qualified config path which holds the dispatcher configuration
        /// on which file IO tasks are scheduled
        member this.file_io_dispatcher value =
            this.FileIODispatcher(State.create Tcp.Target.IO [], value) |> this.Run

        /// The maximum number of bytes (or "unlimited") to transfer in one batch
        /// when using `WriteFile` command which uses `FileChannel.transferTo` to
        /// pipe files to a TCP socket. On some OS like Linux `FileChannel.transferTo`
        /// may block for a long time when network IO is faster than file IO.
        /// Decreasing the value may improve fairness while increasing may improve
        /// throughput.
        [<CustomOperation("file_io_transferTo_limit"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FileIoTransferToLimit(state: State<Tcp.Target>, value) =
            positiveField "file-io-transferTo-limit" value
            |> State.addWith state Tcp.Target.IO

        /// The maximum number of bytes (or "unlimited") to transfer in one batch
        /// when using `WriteFile` command which uses `FileChannel.transferTo` to
        /// pipe files to a TCP socket. On some OS like Linux `FileChannel.transferTo`
        /// may block for a long time when network IO is faster than file IO.
        /// Decreasing the value may improve fairness while increasing may improve
        /// throughput.
        member inline this.file_io_transferTo_limit value =
            this.FileIoTransferToLimit(State.create Tcp.Target.IO [], value) |> this.Run

        /// The number of times to retry the `finishConnect` call after being notified about
        /// OP_CONNECT. Retries are needed if the OP_CONNECT notification doesn't imply that
        /// `finishConnect` will succeed, which is the case on Android.
        [<CustomOperation("finish_connect_retries"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FinishConnectRetries(state: State<Tcp.Target>, value) =
            positiveField "finish-connect-retries" value
            |> State.addWith state Tcp.Target.IO

        /// The number of times to retry the `finishConnect` call after being notified about
        /// OP_CONNECT. Retries are needed if the OP_CONNECT notification doesn't imply that
        /// `finishConnect` will succeed, which is the case on Android.
        member inline this.finish_connect_retries value =
            this.FinishConnectRetries(State.create Tcp.Target.IO [], value) |> this.Run

        /// On Windows connection aborts are not reliably detected unless an OP_READ is
        /// registered on the selector _after_ the connection has been reset. This
        /// workaround enables an OP_CONNECT which forces the abort to be visible on Windows.
        /// Enabling this setting on other platforms than Windows will cause various failures
        /// and undefined behavior.
        /// Possible values of this key are on, off and auto where auto will enable the
        /// workaround if Windows is detected automatically.
        [<CustomOperation("windows_connection_abort_workaround_enabled"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.WindowsConnectionAbortWorkaroundEnabled(state: State<Tcp.Target>, value: bool) =
            switchField "windows-connection-abort-workaround-enabled" value
            |> State.addWith state Tcp.Target.IO

        /// On Windows connection aborts are not reliably detected unless an OP_READ is
        /// registered on the selector _after_ the connection has been reset. This
        /// workaround enables an OP_CONNECT which forces the abort to be visible on Windows.
        /// Enabling this setting on other platforms than Windows will cause various failures
        /// and undefined behavior.
        /// Possible values of this key are on, off and auto where auto will enable the
        /// workaround if Windows is detected automatically.
        member this.windows_connection_abort_workaround_enabled value =
            this.WindowsConnectionAbortWorkaroundEnabled(State.create Tcp.Target.IO [], value)
            |> this.Run

        /// Enforce outgoing socket connection to use IPv4 address family. Required in
        /// scenario when IPv6 is not available, for example in Azure Web App sandbox.
        /// When set to true it is required to set akka.io.dns.inet-address.use-ipv6 to false
        /// in cases when DnsEndPoint is used to describe the remote address
        [<CustomOperation("outgoing_socket_force_ipv4"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.OutgoingSocketForceIpv4(state: State<Tcp.Target>, value: bool) =
            boolField "outgoing-socket-force-ipv4" value
            |> State.addWith state Tcp.Target.IO

        /// Enforce outgoing socket connection to use IPv4 address family. Required in
        /// scenario when IPv6 is not available, for example in Azure Web App sandbox.
        /// When set to true it is required to set akka.io.dns.inet-address.use-ipv6 to false
        /// in cases when DnsEndPoint is used to describe the remote address
        member this.outgoing_socket_force_ipv4 value =
            this.OutgoingSocketForceIpv4(State.create Tcp.Target.IO [], value) |> this.Run

        // Akka.Remote.DotNetty

        /// The class given here must implement the akka.remote.transport.Transport
        /// interface and offer a public constructor which takes two arguments:
        ///
        ///  1) akka.actor.ExtendedActorSystem
        ///
        ///  2) com.typesafe.config.Config
        [<CustomOperation("transport_class"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TransportClass(state: State<Tcp.Target>, x: string) =
            quotedField "transport-class" x |> State.addWith state Tcp.Target.Remote

        /// The class given here must implement the akka.remote.transport.Transport
        /// interface and offer a public constructor which takes two arguments:
        ///
        ///  1) akka.actor.ExtendedActorSystem
        ///
        ///  2) com.typesafe.config.Config
        member this.transport_class value =
            this.TransportClass(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters list. The last adapter in the
        /// list is the adapter immediately above the driver, while
        /// the first one is the top of the stack below the standard
        /// Akka protocol
        [<CustomOperation("applied_adapters"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppliedAdapters(state: State<Tcp.Target>, x: string list) =
            quotedListField "applied-adapters" x |> State.addWith state Tcp.Target.Remote

        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters list. The last adapter in the
        /// list is the adapter immediately above the driver, while
        /// the first one is the top of the stack below the standard
        /// Akka protocol
        member this.applied_adapters value =
            this.AppliedAdapters(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Byte order used for network communication. Event thou DotNetty is big-endian
        /// by default, we need to switch it back to little endian in order to support
        /// backward compatibility with Helios.
        [<CustomOperation("byte_order"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ByteOrder(state: State<Tcp.Target>, x: Hocon.ByteOrder) =
            quotedField "byte-order" (x.Text) |> State.addWith state Tcp.Target.Remote

        /// Byte order used for network communication. Event thou DotNetty is big-endian
        /// by default, we need to switch it back to little endian in order to support
        /// backward compatibility with Helios.
        member this.byte_order value =
            this.ByteOrder(State.create Tcp.Target.Remote [], value) |> this.Run

        /// The default remote server port clients should connect to.
        /// Default is 2552 (AKKA), use 0 if you want a random available port
        /// This port needs to be unique for each actor system on the same machine.
        [<CustomOperation("port"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Port(state: State<Tcp.Target>, x) =
            positiveField "port" x |> State.addWith state Tcp.Target.Remote

        /// The default remote server port clients should connect to.
        /// Default is 2552 (AKKA), use 0 if you want a random available port
        /// This port needs to be unique for each actor system on the same machine.
        member inline this.port value =
            this.Port(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Similar in spirit to "public-hostname" setting, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// "port" setting, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        ///
        /// Leave this setting to "0" if you don't intend to use it.
        [<CustomOperation("public_port"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PublicPort(state: State<Tcp.Target>, x) =
            positiveField "public-port" x |> State.addWith state Tcp.Target.Remote

        /// Similar in spirit to "public-hostname" setting, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// "port" setting, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        ///
        /// Leave this setting to "0" if you don't intend to use it.
        member inline this.public_port value =
            this.PublicPort(State.create Tcp.Target.Remote [], value) |> this.Run

        /// The hostname or ip to bind the remoting to,
        /// InetAddress.getLocalHost.getHostAddress is used if empty
        [<CustomOperation("hostname"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Hostname(state: State<Tcp.Target>, x: string) =
            field "hostname" x |> State.addWith state Tcp.Target.Remote

        /// The hostname or ip to bind the remoting to,
        /// InetAddress.getLocalHost.getHostAddress is used if empty
        member this.hostname value =
            this.Hostname(State.create Tcp.Target.Remote [], value) |> this.Run

        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname)
        /// this is designed to make it easy to support private / public addressing schemes
        [<CustomOperation("public_hostname"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.PublicHostname(state: State<Tcp.Target>, x: string) =
            field "public-hostname" x |> State.addWith state Tcp.Target.Remote

        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname)
        /// this is designed to make it easy to support private / public addressing schemes
        member this.public_hostname value =
            this.PublicHostname(State.create Tcp.Target.Remote [], value) |> this.Run

        /// If set to true, we will use IPV6 addresses upon DNS resolution for host names.
        /// Otherwise, we will use IPV4.
        [<CustomOperation("dns_use_ipv6"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DnsUseIpv6(state: State<Tcp.Target>, value: bool) =
            boolField "dns-use-ipv6" value |> State.addWith state Tcp.Target.Remote

        /// If set to true, we will use IPV6 addresses upon DNS resolution for host names.
        /// Otherwise, we will use IPV4.
        member this.dns_use_ipv6 value =
            this.DnsUseIpv6(State.create Tcp.Target.Remote [], value) |> this.Run

        /// If set to true, we will enforce usage of IPV4 or IPV6 addresses upon DNS resolution for host names.
        /// If dns-use-ipv6 = true, we will use IPV6 enforcement
        /// Otherwise, we will use IPV4.
        /// Warning: when ip family is enforced, any connection between IPV4 and IPV6 is impossible
        ///
        /// enforce-ip-family setting is used only in some special cases, when default behaviour of
        /// underlying sockets leads to errors. Typically this occurs when an environment doesn't support
        /// IPV6 or dual-mode sockets.
        /// As of 09/21/2016 there are two known cases: running under Mono and in Azure WebApp
        /// for them we will need enforce-ip-family = true, and for Azure dns-use-ipv6 = false
        /// This property is always set to true if Mono runtime is detected.
        [<CustomOperation("enforce_ip_family"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnforceIpFamily(state: State<Tcp.Target>, value: bool) =
            boolField "enforce-ip-family" value |> State.addWith state Tcp.Target.Remote

        /// If set to true, we will enforce usage of IPV4 or IPV6 addresses upon DNS resolution for host names.
        /// If dns-use-ipv6 = true, we will use IPV6 enforcement
        /// Otherwise, we will use IPV4.
        /// Warning: when ip family is enforced, any connection between IPV4 and IPV6 is impossible
        ///
        /// enforce-ip-family setting is used only in some special cases, when default behaviour of
        /// underlying sockets leads to errors. Typically this occurs when an environment doesn't support
        /// IPV6 or dual-mode sockets.
        /// As of 09/21/2016 there are two known cases: running under Mono and in Azure WebApp
        /// for them we will need enforce-ip-family = true, and for Azure dns-use-ipv6 = false
        /// This property is always set to true if Mono runtime is detected.
        member this.enforce_ip_family value =
            this.EnforceIpFamily(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Enables SSL support on this transport
        [<CustomOperation("enable_ssl"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnableSsl(state: State<Tcp.Target>, value: bool) =
            boolField "enable-ssl" value |> State.addWith state Tcp.Target.Remote

        /// Enables SSL support on this transport
        member this.enable_ssl value =
            this.EnableSsl(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
        [<CustomOperation("enable_backwards_compatibility"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnableBackwardsCompatibility(state: State<Tcp.Target>, value: bool) =
            boolField "enable-backwards-compatibility" value
            |> State.addWith state Tcp.Target.Remote

        /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
        member this.enable_backwards_compatibility value =
            this.EnableBackwardsCompatibility(State.create Tcp.Target.Remote [], value)
            |> this.Run

        /// Sets the connectTimeoutMillis of all outbound connections,
        /// i.e. how long a connect may take until it is timed out
        [<CustomOperation("connection_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ConnectionTimeout(state: State<Tcp.Target>, value: 'Duration) =
            durationField "connection-timeout" value
            |> State.addWith state Tcp.Target.Remote

        /// Sets the connectTimeoutMillis of all outbound connections,
        /// i.e. how long a connect may take until it is timed out
        member inline this.connection_timeout(value: 'Duration) =
            this.ConnectionTimeout(State.create Tcp.Target.Remote [], value) |> this.Run

        /// If set to "<id.of.dispatcher>" then the specified dispatcher
        /// will be used to accept inbound connections, and perform IO. If "" then
        /// dedicated threads will be used.
        ///
        /// Please note that the Helios driver only uses this configuration and does
        /// not read the "akka.remote.use-dispatcher" entry. Instead it has to be
        /// configured manually to point to the same dispatcher if needed.
        [<CustomOperation("use_dispatcher_for_io"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcherForIo(state: State<Tcp.Target>, x: string) =
            quotedField "use-dispatcher-for-io" x |> State.addWith state Tcp.Target.Remote

        /// If set to "<id.of.dispatcher>" then the specified dispatcher
        /// will be used to accept inbound connections, and perform IO. If "" then
        /// dedicated threads will be used.
        ///
        /// Please note that the Helios driver only uses this configuration and does
        /// not read the "akka.remote.use-dispatcher" entry. Instead it has to be
        /// configured manually to point to the same dispatcher if needed.
        member this.use_dispatcher_for_io value =
            this.UseDispatcherForIo(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Sets the high water mark for the in and outbound sockets,
        /// set to 0b for platform default
        [<CustomOperation("write_buffer_high_water_mark"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WriteBufferHighWaterMark(state: State<Tcp.Target>, value) =
            byteField "write-buffer-high-water-mark" value
            |> State.addWith state Tcp.Target.Remote

        /// Sets the high water mark for the in and outbound sockets,
        /// set to 0b for platform default
        member inline this.write_buffer_high_water_mark value =
            this.WriteBufferHighWaterMark(State.create Tcp.Target.Remote [], value)
            |> this.Run

        /// Sets the low water mark for the in and outbound sockets,
        /// set to 0b for platform default
        [<CustomOperation("write_buffer_low_water_mark"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WriteBufferLowhWaterMark(state: State<Tcp.Target>, value) =
            byteField "write-buffer-low-water-mark" value
            |> State.addWith state Tcp.Target.Remote

        /// Sets the low water mark for the in and outbound sockets,
        /// set to 0b for platform default
        member inline this.write_buffer_low_water_mark value =
            this.WriteBufferLowhWaterMark(State.create Tcp.Target.Remote [], value)
            |> this.Run

        /// Sets the send buffer size of the Sockets,
        /// set to 0b for platform default
        [<CustomOperation("send_buffer_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SendBufferSize(state: State<Tcp.Target>, value) =
            byteField "send-buffer-size" value |> State.addWith state Tcp.Target.Remote

        /// Sets the send buffer size of the Sockets,
        /// set to 0b for platform default
        member inline this.send_buffer_size value =
            this.SendBufferSize(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Sets the receive buffer size of the Sockets,
        /// set to 0b for platform default
        [<CustomOperation("receive_buffer_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ReceiveBufferSize(state: State<Tcp.Target>, value) =
            byteField "receive-buffer-size" value |> State.addWith state Tcp.Target.Remote

        /// Sets the receive buffer size of the Sockets,
        /// set to 0b for platform default
        member inline this.receive_buffer_size value =
            this.ReceiveBufferSize(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Maximum message size the transport will accept, but at least
        /// 32000 bytes.
        ///
        /// Please note that UDP does not support arbitrary large datagrams,
        /// so this setting has to be chosen carefully when using UDP.
        /// Both send-buffer-size and receive-buffer-size settings has to
        /// be adjusted to be able to buffer messages of maximum size.
        [<CustomOperation("maximum_frame_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaximumFrameSize(state: State<Tcp.Target>, value) =
            byteField "maximum-frame-size" value |> State.addWith state Tcp.Target.Remote

        /// Maximum message size the transport will accept, but at least
        /// 32000 bytes.
        ///
        /// Please note that UDP does not support arbitrary large datagrams,
        /// so this setting has to be chosen carefully when using UDP.
        /// Both send-buffer-size and receive-buffer-size settings has to
        /// be adjusted to be able to buffer messages of maximum size.
        member inline this.maximum_frame_size value =
            this.MaximumFrameSize(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Sets the size of the connection backlog
        [<CustomOperation("backlog"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Backlog(state: State<Tcp.Target>, value) =
            positiveField "backlog" value |> State.addWith state Tcp.Target.Remote

        /// Sets the size of the connection backlog
        member inline this.backlog value =
            this.Backlog(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Enables the TCP_NODELAY flag, i.e. disables Nagleâ€™s algorithm
        [<CustomOperation("tcp_nodelay"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TcpNodelay(state: State<Tcp.Target>, value: bool) =
            switchField "tcp-nodelay" value |> State.addWith state Tcp.Target.Remote

        /// Enables the TCP_NODELAY flag, i.e. disables Nagleâ€™s algorithm
        member this.tcp_nodelay value =
            this.TcpNodelay(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Enables TCP Keepalive, subject to the O/S kernelâ€™s configuration
        [<CustomOperation("tcp_keepalive"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TcpKeepalive(state: State<Tcp.Target>, value: bool) =
            switchField "tcp-keepalive" value |> State.addWith state Tcp.Target.Remote

        /// Enables TCP Keepalive, subject to the O/S kernelâ€™s configuration
        member this.tcp_keepalive value =
            this.TcpKeepalive(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Enables SO_REUSEADDR, which determines when an ActorSystem can open
        /// the specified listen port (the meaning differs between *nix and Windows)
        ///
        /// Valid values are "on", "off", and "off-for-windows"
        /// due to the following Windows bug: http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4476378
        /// "off-for-windows" of course means that it's "on" for all other platforms
        [<CustomOperation("tcp_reuse_addr"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TcpReuseAddr(state: State<Tcp.Target>, value: bool) =
            switchField "tcp-reuse-addr" value |> State.addWith state Tcp.Target.Remote

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TcpReuseAddr(state: State<Tcp.Target>, value: Hocon.OffForWindows) =
            field "tcp-reuse-addr" (value.ToString())
            |> State.addWith state Tcp.Target.Remote

        /// Enables SO_REUSEADDR, which determines when an ActorSystem can open
        /// the specified listen port (the meaning differs between *nix and Windows)
        ///
        /// Valid values are "on", "off", and "off-for-windows"
        /// due to the following Windows bug: http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4476378
        /// "off-for-windows" of course means that it's "on" for all other platforms
        member this.tcp_reuse_addr(value: bool) =
            this.TcpReuseAddr(State.create Tcp.Target.Remote [], value) |> this.Run

        /// Enables SO_REUSEADDR, which determines when an ActorSystem can open
        /// the specified listen port (the meaning differs between *nix and Windows)
        ///
        /// Valid values are "on", "off", and "off-for-windows"
        /// due to the following Windows bug: http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=4476378
        /// "off-for-windows" of course means that it's "on" for all other platforms
        member this.tcp_reuse_addr(value: Hocon.OffForWindows) =
            this.TcpReuseAddr(State.create Tcp.Target.Remote [], value) |> this.Run

        /// configured manually to point to the same dispatcher if needed.
        [<CustomOperation("transport_protocol"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TransportProtocol(state: State<Tcp.Target>, x: string) =
            field "transport-protocol" x |> State.addWith state Tcp.Target.Remote

        /// configured manually to point to the same dispatcher if needed.
        member this.transport_protocol value =
            this.TransportProtocol(State.create Tcp.Target.Remote [], value) |> this.Run

        // Pathing

        // TODO: make extensions return explicit types and have overloads at call sites for type safety

        member _.direct_buffer_pool =
            { new DirectBufferPoolBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr 3 "direct-buffer-pool" state }

        member _.disabled_buffer_pool =
            { new DisabledBufferPoolBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr 3 "disabled-buffer-pool" state }

        member _.batching =
            { new BatchingBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr 4 "batching" state }

        member _.client_socket_worker_pool =
            { new WorkerPoolBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr 4 "client-socket-worker-pool" state }

        member _.server_socket_worker_pool =
            { new WorkerPoolBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr 4 "server-socket-worker-pool" state }

    let tcp =
        let path = "tcp"

        { new TcpBuilder<Akka.Shared.Tcp.Field>(Akka.Shared.Tcp.Field, path) with
            member _.Run(state: State<Tcp.Target>) : Akka.Shared.Field<Akka.Shared.Tcp.Field> =
                fun indent -> objExpr path indent state.Fields |> Akka.Shared.Tcp.Field }
