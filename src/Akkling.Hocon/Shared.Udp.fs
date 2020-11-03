namespace Akkling.Hocon

[<AutoOpen>]
module Udp =
    open MarkerClasses
    open InternalHocon
    open SharedInternal
    open System.ComponentModel

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    module Udp =
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
    type UdpBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string) =
        let pathObjExpr = pathObjExpr mkField path
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        abstract Run : State<Udp.Target> -> Akka.Shared.Field<'T>
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: string) = Udp.State.create [x]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.IO.DisabledBufferPool.Field) = State.create Udp.Target.IO [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.IO.DirectBufferPool.Field) = State.create Udp.Target.IO [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.Batching.Field) = State.create Udp.Target.Remote [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.WorkerPool.Field) = State.create Udp.Target.Remote [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (a: unit) = Udp.State.create []
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine (state1: State<Udp.Target>, state2: State<Udp.Target>) : State<Udp.Target> =
            let fields = state1.Fields @ state2.Fields

            match Udp.Target.merge state1.Target state2.Target with
            | Some target -> { Target = target; Fields = fields }
            | None -> 
                failwithf "Udp fields mismatch found was given:%s%A"
                    System.Environment.NewLine fields
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero () = Udp.State.create []
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f()
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member this.For (state: State<Udp.Target>, f: unit -> State<Udp.Target>) = this.Combine(state, f())

        // Akka.IO

        /// A buffer pool used to acquire and release byte buffers from the managed
        /// heap. Once byte buffer is no longer needed is can be released, landing 
        /// on the pool again, to be reused later. This way we can reduce a GC pressure
        /// by reusing the same components instead of recycling them.
        ///
        /// NOTE: pooling is disabled by default, to enable use direct-buffer-pool:
        /// set this field to "akka.io.tcp.direct-buffer-pool"
        [<CustomOperation("buffer_pool");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BufferPool (state: State<Udp.Target>, value: string) =
            quotedField "buffer-pool" value
            |> State.addWith state Udp.Target.IO
        /// A buffer pool used to acquire and release byte buffers from the managed
        /// heap. Once byte buffer is no longer needed is can be released, landing 
        /// on the pool again, to be reused later. This way we can reduce a GC pressure
        /// by reusing the same components instead of recycling them.
        ///
        /// NOTE: pooling is disabled by default, to enable use direct-buffer-pool:
        /// set this field to "akka.io.tcp.direct-buffer-pool"
        member this.buffer_pool value =
            this.BufferPool(State.create Udp.Target.IO [], value)
            |> this.Run
            
        /// The number of selectors to stripe the served channels over; each of
        /// these will use one select loop on the selector-dispatcher.
        [<CustomOperation("nr_of_socket_async_event_args");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.NrOfSocketAsyncEventArgs (state: State<Udp.Target>, value) =
            positiveField "nr-of-socket-async-event-args" value
            |> State.addWith state Udp.Target.IO
        /// The number of selectors to stripe the served channels over; each of
        /// these will use one select loop on the selector-dispatcher.
        member inline this.nr_of_socket_async_event_args value =
            this.NrOfSocketAsyncEventArgs(State.create Udp.Target.IO [], value)
            |> this.Run

        /// Maximum number of open channels supported by this UDP module Generally
        /// UDP does not require a large number of channels, therefore it is
        /// recommended to keep this setting low.
        [<CustomOperation("max_channels");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaxChannels (state: State<Udp.Target>, value) =
            positiveField "max-channels" value
            |> State.addWith state Udp.Target.IO
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxChannels (state: State<Udp.Target>, value: Hocon.Unlimited) =
            quotedField "max-channels" (value.ToString())
            |> State.addWith state Udp.Target.IO
        /// Maximum number of open channels supported by this UDP module Generally
        /// UDP does not require a large number of channels, therefore it is
        /// recommended to keep this setting low.
        member inline this.max_channels value =
            positiveField "max-channels" value
            |> State.add (State.create Udp.Target.IO [])
            |> this.Run
        /// Maximum number of open channels supported by this UDP module Generally
        /// UDP does not require a large number of channels, therefore it is
        /// recommended to keep this setting low.
        member this.max_channels (value: Hocon.Unlimited) =
            this.MaxChannels(State.create Udp.Target.IO [], value)
            |> this.Run
            
        /// The select loop can be used in two modes:
        ///
        /// - setting "infinite" will select without a timeout, hogging a thread
        ///
        /// - setting a positive timeout will do a bounded select call,
        ///   enabling sharing of a single thread between multiple selectors
        ///   (in this case you will have to use a different configuration for the
        ///   selector-dispatcher, e.g. using "type=Dispatcher" with size 1)
        ///
        /// - setting it to zero means polling, i.e. calling selectNow()
        [<CustomOperation("select_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SelectTimeout (state: State<Udp.Target>, value: 'Duration) =
            durationField "select-timeout" value
            |> State.addWith state Udp.Target.IO
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SelectTimeout (state: State<Udp.Target>, value: Hocon.Infinite) =
            field "select-timeout" (value.ToString())
            |> State.addWith state Udp.Target.IO
        /// The select loop can be used in two modes:
        ///
        /// - setting "infinite" will select without a timeout, hogging a thread
        ///
        /// - setting a positive timeout will do a bounded select call,
        ///   enabling sharing of a single thread between multiple selectors
        ///   (in this case you will have to use a different configuration for the
        ///   selector-dispatcher, e.g. using "type=Dispatcher" with size 1)
        ///
        /// - setting it to zero means polling, i.e. calling selectNow()
        member inline this.select_timeout value =
            durationField "select-timeout" value
            |> State.add (State.create Udp.Target.IO [])
            |> this.Run
        /// The select loop can be used in two modes:
        ///
        /// - setting "infinite" will select without a timeout, hogging a thread
        ///
        /// - setting a positive timeout will do a bounded select call,
        ///   enabling sharing of a single thread between multiple selectors
        ///   (in this case you will have to use a different configuration for the
        ///   selector-dispatcher, e.g. using "type=Dispatcher" with size 1)
        ///
        /// - setting it to zero means polling, i.e. calling selectNow()
        member this.select_timeout (value: Hocon.Infinite) =
            this.SelectTimeout(State.create Udp.Target.IO [], value)
            |> this.Run

        /// When trying to assign a new connection to a selector and the chosen
        /// selector is at full capacity, retry selector choosing and assignment
        /// this many times before giving up
        [<CustomOperation("selector_association_retries");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SelectorAssociationRetries (state: State<Udp.Target>, value) =
            positiveField "selector-association-retries" value
            |> State.addWith state Udp.Target.IO
        /// When trying to assign a new connection to a selector and the chosen
        /// selector is at full capacity, retry selector choosing and assignment
        /// this many times before giving up
        member inline this.selector_association_retries value =
            this.SelectorAssociationRetries(State.create Udp.Target.IO [], value)
            |> this.Run
            
        /// The maximum number of datagrams that are read in one go,
        /// higher numbers decrease latency, lower numbers increase fairness on
        /// the worker-dispatcher
        [<CustomOperation("receive_throughput");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ReceiveThroughput (state: State<Udp.Target>, value) =
            positiveField "receive-throughput" value
            |> State.addWith state Udp.Target.IO
        /// The maximum number of datagrams that are read in one go,
        /// higher numbers decrease latency, lower numbers increase fairness on
        /// the worker-dispatcher
        member inline this.receive_throughput value =
            this.ReceiveThroughput(State.create Udp.Target.IO [], value)
            |> this.Run

        /// The number of bytes per direct buffer in the pool used to read or write
        /// network data from the kernel.
        [<CustomOperation("direct_buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DirectBufferSize (state: State<Udp.Target>, value) =
            positiveField "direct-buffer-size" value
            |> State.addWith state Udp.Target.IO
        /// The number of bytes per direct buffer in the pool used to read or write
        /// network data from the kernel.
        member inline this.direct_buffer_size value =
            this.DirectBufferSize(State.create Udp.Target.IO [], value)
            |> this.Run

        /// The maximal number of direct buffers kept in the direct buffer pool for
        /// reuse.
        [<CustomOperation("direct_buffer_pool_limit");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DirectBufferPoolLimit (state: State<Udp.Target>, value) =
            positiveField "direct-buffer-pool-limit" value
            |> State.addWith state Udp.Target.IO
        /// The maximal number of direct buffers kept in the direct buffer pool for
        /// reuse.
        member inline this.direct_buffer_pool_limit value =
            this.DirectBufferPoolLimit(State.create Udp.Target.IO [], value)
            |> this.Run
            
        /// The maximum number of bytes delivered by a `Received` message. Before
        /// more data is read from the network the connection actor will try to
        /// do other work.
        [<CustomOperation("received_message_size_limit");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ReceivedMessageSizeLimit (state: State<Udp.Target>, value) =
            positiveField "received-message-size-limit" value
            |> State.addWith state Udp.Target.IO
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ReceivedMessageSizeLimit (state: State<Udp.Target>, value: Hocon.Unlimited) =
            quotedField "received-message-size-limit" (value.ToString())
            |> State.addWith state Udp.Target.IO
        /// The maximum number of bytes delivered by a `Received` message. Before
        /// more data is read from the network the connection actor will try to
        /// do other work.
        member inline this.received_message_size_limit value =
            positiveField "received-message-size-limit" value
            |> State.add (State.create Udp.Target.IO [])
            |> this.Run
        /// The maximum number of bytes delivered by a `Received` message. Before
        /// more data is read from the network the connection actor will try to
        /// do other work.
        member this.received_message_size_limit (value: Hocon.Unlimited) =
            this.ReceivedMessageSizeLimit(State.create Udp.Target.IO [], value)
            |> this.Run

        /// Enable fine grained logging of what goes on inside the implementation.
        /// Be aware that this may log more than once per message sent to the actors
        /// of the tcp implementation.
        [<CustomOperation("trace_logging");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TraceLogging (state: State<Udp.Target>, value: bool) =
            switchField "trace-logging" value
            |> State.addWith state Udp.Target.IO
        /// Enable fine grained logging of what goes on inside the implementation.
        /// Be aware that this may log more than once per message sent to the actors
        /// of the tcp implementation.
        member this.trace_logging value =
            this.TraceLogging(State.create Udp.Target.IO [], value)
            |> this.Run
        
        /// Fully qualified config path which holds the dispatcher configuration
        /// to be used for running the select() calls in the selectors
        [<CustomOperation("selector_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SelectorDispatcher (state: State<Udp.Target>, value: string) =
            quotedField "selector-dispatcher" value
            |> State.addWith state Udp.Target.IO
        /// Fully qualified config path which holds the dispatcher configuration
        /// to be used for running the select() calls in the selectors
        member this.selector_dispatcher value =
            this.SelectorDispatcher(State.create Udp.Target.IO [], value)
            |> this.Run
            
        /// Fully qualified config path which holds the dispatcher configuration
        /// for the read/write worker actors
        [<CustomOperation("worker_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.WorkerDispatcher (state: State<Udp.Target>, value: string) =
            quotedField "worker-dispatcher" value
            |> State.addWith state Udp.Target.IO
        /// Fully qualified config path which holds the dispatcher configuration
        /// for the read/write worker actors
        member this.worker_dispatcher value =
            this.WorkerDispatcher(State.create Udp.Target.IO [], value)
            |> this.Run

        /// Fully qualified config path which holds the dispatcher configuration
        /// for the selector management actors
        [<CustomOperation("management_dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ManagementDispatcher (state: State<Udp.Target>, value: string) =
            quotedField "management-dispatcher" value
            |> State.addWith state Udp.Target.IO
        /// Fully qualified config path which holds the dispatcher configuration
        /// for the selector management actors
        member this.management_dispatcher value =
            this.ManagementDispatcher(State.create Udp.Target.IO [], value)
            |> this.Run
        
        // Akka.Remote.DotNetty

        /// The class given here must implement the akka.remote.transport.Transport
        /// interface and offer a public constructor which takes two arguments:
        ///
        ///  1) akka.actor.ExtendedActorSystem
        ///
        ///  2) com.typesafe.config.Config
        [<CustomOperation("transport_class");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TransportClass (state: State<Udp.Target>, x: string) = 
            quotedField "transport-class" x
            |> State.addWith state Udp.Target.Remote
        /// The class given here must implement the akka.remote.transport.Transport
        /// interface and offer a public constructor which takes two arguments:
        ///
        ///  1) akka.actor.ExtendedActorSystem
        ///
        ///  2) com.typesafe.config.Config
        member this.transport_class value =
            this.TransportClass(State.create Udp.Target.Remote [], value)
            |> this.Run
        
        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters list. The last adapter in the
        /// list is the adapter immediately above the driver, while
        /// the first one is the top of the stack below the standard
        /// Akka protocol 
        [<CustomOperation("applied_adapters");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppliedAdapters (state: State<Udp.Target>, x: string list) = 
            quotedListField "applied-adapters" x
            |> State.addWith state Udp.Target.Remote
        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters list. The last adapter in the
        /// list is the adapter immediately above the driver, while
        /// the first one is the top of the stack below the standard
        /// Akka protocol 
        member this.applied_adapters value =
            this.AppliedAdapters(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Byte order used for network communication. Event thou DotNetty is big-endian
        /// by default, we need to switch it back to little endian in order to support
        /// backward compatibility with Helios.
        [<CustomOperation("byte_order");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ByteOrder (state: State<Udp.Target>, x: Hocon.ByteOrder) = 
            quotedField "byte-order" (x.Text)
            |> State.addWith state Udp.Target.Remote
        /// Byte order used for network communication. Event thou DotNetty is big-endian
        /// by default, we need to switch it back to little endian in order to support
        /// backward compatibility with Helios.
        member this.byte_order value =
            this.ByteOrder(State.create Udp.Target.Remote [], value)
            |> this.Run
        
        /// The default remote server port clients should connect to.
        /// Default is 2552 (AKKA), use 0 if you want a random available port
        /// This port needs to be unique for each actor system on the same machine.
        [<CustomOperation("port");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Port (state: State<Udp.Target>, x) =
            positiveField "port" x
            |> State.addWith state Udp.Target.Remote
        /// The default remote server port clients should connect to.
        /// Default is 2552 (AKKA), use 0 if you want a random available port
        /// This port needs to be unique for each actor system on the same machine.
        member inline this.port value =
            this.Port(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Similar in spirit to "public-hostname" setting, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// "port" setting, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        ///
        /// Leave this setting to "0" if you don't intend to use it.
        [<CustomOperation("public_port");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PublicPort (state: State<Udp.Target>, x) =
            positiveField "public-port" x
            |> State.addWith state Udp.Target.Remote
        /// Similar in spirit to "public-hostname" setting, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// "port" setting, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        ///
        /// Leave this setting to "0" if you don't intend to use it.
        member inline this.public_port value =
            this.PublicPort(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// The hostname or ip to bind the remoting to,
        /// InetAddress.getLocalHost.getHostAddress is used if empty
        [<CustomOperation("hostname");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Hostname (state: State<Udp.Target>, x: string) = 
            field "hostname" x
            |> State.addWith state Udp.Target.Remote
        /// The hostname or ip to bind the remoting to,
        /// InetAddress.getLocalHost.getHostAddress is used if empty
        member this.hostname value =
            this.Hostname(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname)
        /// this is designed to make it easy to support private / public addressing schemes
        [<CustomOperation("public_hostname");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.PublicHostname (state: State<Udp.Target>, x: string) = 
            field "public-hostname" x
            |> State.addWith state Udp.Target.Remote
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname)
        /// this is designed to make it easy to support private / public addressing schemes
        member this.public_hostname value =
            this.PublicHostname(State.create Udp.Target.Remote [], value)
            |> this.Run
        
        /// If set to true, we will use IPV6 addresses upon DNS resolution for host names.
        /// Otherwise, we will use IPV4.
        [<CustomOperation("dns_use_ipv6");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DnsUseIpv6 (state: State<Udp.Target>, value: bool) = 
            boolField "dns-use-ipv6" value
            |> State.addWith state Udp.Target.Remote
        /// If set to true, we will use IPV6 addresses upon DNS resolution for host names.
        /// Otherwise, we will use IPV4.
        member this.dns_use_ipv6 value =
            this.DnsUseIpv6(State.create Udp.Target.Remote [], value)
            |> this.Run
        
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
        [<CustomOperation("enforce_ip_family");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnforceIpFamily (state: State<Udp.Target>, value: bool) = 
            boolField "enforce-ip-family" value
            |> State.addWith state Udp.Target.Remote
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
            this.EnforceIpFamily(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Enables SSL support on this transport
        [<CustomOperation("enable_ssl");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnableSsl (state: State<Udp.Target>, value: bool) = 
            boolField "enable-ssl" value
            |> State.addWith state Udp.Target.Remote
        /// Enables SSL support on this transport
        member this.enable_ssl value =
            this.EnableSsl(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
        [<CustomOperation("enable_backwards_compatibility");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnableBackwardsCompatibility (state: State<Udp.Target>, value: bool) = 
            boolField "enable-backwards-compatibility" value
            |> State.addWith state Udp.Target.Remote
        /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
        member this.enable_backwards_compatibility value =
            this.EnableBackwardsCompatibility(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Sets the connectTimeoutMillis of all outbound connections,
        /// i.e. how long a connect may take until it is timed out
        [<CustomOperation("connection_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ConnectionTimeout (state: State<Udp.Target>, value: 'Duration) =
            durationField "connection-timeout" value
            |> State.addWith state Udp.Target.Remote
        /// Sets the connectTimeoutMillis of all outbound connections,
        /// i.e. how long a connect may take until it is timed out
        member inline this.connection_timeout (value: 'Duration) =
            this.ConnectionTimeout(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// If set to "<id.of.dispatcher>" then the specified dispatcher
        /// will be used to accept inbound connections, and perform IO. If "" then
        /// dedicated threads will be used.
        ///
        /// Please note that the Helios driver only uses this configuration and does
        /// not read the "akka.remote.use-dispatcher" entry. Instead it has to be
        /// configured manually to point to the same dispatcher if needed.
        [<CustomOperation("use_dispatcher_for_io");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcherForIo (state: State<Udp.Target>, x: string) = 
            quotedField "use-dispatcher-for-io" x
            |> State.addWith state Udp.Target.Remote
        /// If set to "<id.of.dispatcher>" then the specified dispatcher
        /// will be used to accept inbound connections, and perform IO. If "" then
        /// dedicated threads will be used.
        ///
        /// Please note that the Helios driver only uses this configuration and does
        /// not read the "akka.remote.use-dispatcher" entry. Instead it has to be
        /// configured manually to point to the same dispatcher if needed.
        member this.use_dispatcher_for_io value =
            this.UseDispatcherForIo(State.create Udp.Target.Remote [], value)
            |> this.Run
        
        /// Sets the high water mark for the in and outbound sockets,
        /// set to 0b for platform default
        [<CustomOperation("write_buffer_high_water_mark");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WriteBufferHighWaterMark (state: State<Udp.Target>, value) =
            byteField "write-buffer-high-water-mark" value
            |> State.addWith state Udp.Target.Remote
        /// Sets the high water mark for the in and outbound sockets,
        /// set to 0b for platform default
        member inline this.write_buffer_high_water_mark value =
            this.WriteBufferHighWaterMark(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Sets the low water mark for the in and outbound sockets,
        /// set to 0b for platform default
        [<CustomOperation("write_buffer_low_water_mark");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WriteBufferLowhWaterMark (state: State<Udp.Target>, value) =
            byteField "write-buffer-low-water-mark" value
            |> State.addWith state Udp.Target.Remote
        /// Sets the low water mark for the in and outbound sockets,
        /// set to 0b for platform default
        member inline this.write_buffer_low_water_mark value =
            this.WriteBufferLowhWaterMark(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Sets the send buffer size of the Sockets,
        /// set to 0b for platform default
        [<CustomOperation("send_buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SendBufferSize (state: State<Udp.Target>, value) =
            byteField "send-buffer-size" value
            |> State.addWith state Udp.Target.Remote
        /// Sets the send buffer size of the Sockets,
        /// set to 0b for platform default
        member inline this.send_buffer_size value =
            this.SendBufferSize(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Sets the receive buffer size of the Sockets,
        /// set to 0b for platform default
        [<CustomOperation("receive_buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ReceiveBufferSize (state: State<Udp.Target>, value) =
            byteField "receive-buffer-size" value
            |> State.addWith state Udp.Target.Remote
        /// Sets the receive buffer size of the Sockets,
        /// set to 0b for platform default
        member inline this.receive_buffer_size value =
            this.ReceiveBufferSize(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Maximum message size the transport will accept, but at least
        /// 32000 bytes.
        ///
        /// Please note that UDP does not support arbitrary large datagrams,
        /// so this setting has to be chosen carefully when using UDP.
        /// Both send-buffer-size and receive-buffer-size settings has to
        /// be adjusted to be able to buffer messages of maximum size.
        [<CustomOperation("maximum_frame_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaximumFrameSize (state: State<Udp.Target>, value) =
            byteField "maximum-frame-size" value
            |> State.addWith state Udp.Target.Remote
        /// Maximum message size the transport will accept, but at least
        /// 32000 bytes.
        ///
        /// Please note that UDP does not support arbitrary large datagrams,
        /// so this setting has to be chosen carefully when using UDP.
        /// Both send-buffer-size and receive-buffer-size settings has to
        /// be adjusted to be able to buffer messages of maximum size.
        member inline this.maximum_frame_size value =
            this.MaximumFrameSize(State.create Udp.Target.Remote [], value)
            |> this.Run
            
        /// Sets the size of the connection backlog
        [<CustomOperation("backlog");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Backlog (state: State<Udp.Target>, value) =
            positiveField "backlog" value
            |> State.addWith state Udp.Target.Remote
        /// Sets the size of the connection backlog
        member inline this.backlog value =
            this.Backlog(State.create Udp.Target.Remote [], value)
            |> this.Run
        
        /// configured manually to point to the same dispatcher if needed.
        [<CustomOperation("transport_protocol");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TransportProtocol (state: State<Udp.Target>, x: string) = 
            field "transport-protocol" x
            |> State.addWith state Udp.Target.Remote
        /// configured manually to point to the same dispatcher if needed.
        member this.transport_protocol value =
            this.TransportProtocol(State.create Udp.Target.Remote [], value)
            |> this.Run
        
        // Pathing

        member _.direct_buffer_pool =
            { new DirectBufferPoolBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr 0 "direct-buffer-pool" state }
        
        member _.client_socket_worker_pool = 
            { new WorkerPoolBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr 0 "client-socket-worker-pool" state }

        member _.server_socket_worker_pool = 
            { new WorkerPoolBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr 0 "server-socket-worker-pool" state }

    let udp =
        let path = "udp"

        { new UdpBuilder<Akka.Shared.Udp.Field>(Akka.Shared.Udp.Field, path) with
            member _.Run (state: State<Udp.Target>) : Akka.Shared.Field<Akka.Shared.Udp.Field> =
                fun indent ->
                    objExpr path indent state.Fields
                    |> Akka.Shared.Udp.Field }

    let udp_connected =
        let path = "udp-connected"

        { new UdpBuilder<Akka.Shared.Udp.Field>(Akka.Shared.Udp.Field, path) with
            member _.Run (state: State<Udp.Target>) : Akka.Shared.Field<Akka.Shared.Udp.Field> =
                fun indent ->
                    objExpr path indent state.Fields
                    |> Akka.Shared.Udp.Field }
