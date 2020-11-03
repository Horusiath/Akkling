namespace Akkling.Hocon

[<AutoOpen>]
module DotNetty =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type CertificateBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
                
        /// Valid certificate path required
        [<CustomOperation("path");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Path (state: string list, x: string) = 
            quotedField "path" x::state
        /// Valid certificate path required
        member this.path value =
            this.Path([], value)
            |> this.Run
        
        /// Valid certificate password required
        [<CustomOperation("password");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Password (state: string list, x: string) = 
            quotedField "password" x::state
        /// Valid certificate password required
        member this.password value =
            this.Password([], value)
            |> this.Run
        
        /// Default key storage flag is "default-key-set"
        [<CustomOperation("flags");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Flags (state: string list, x: Hocon.Remoting.CertificateFlags list) = 
            quotedListField "flags" (x |> List.map (fun x -> x.Text))::state
        /// Default key storage flag is "default-key-set"
        member this.flags value =
            this.Flags([], value)
            |> this.Run
            
        /// To use a Thumbprint instead of a file, set this to true
        /// And specify a thumprint and it's storage location below
        [<CustomOperation("use_thumprint_over_file");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseThumprintOverFile (state: string list, value: bool) = 
            boolField "use-thumprint-over-file" value::state
        /// To use a Thumbprint instead of a file, set this to true
        /// And specify a thumprint and it's storage location below
        member this.use_thumprint_over_file value =
            this.UseThumprintOverFile([], value)
            |> this.Run
        
        /// Valid Thumprint required (if use-thumbprint-over-file is true)
        /// A typical thumbprint is a format similar to: "45df32e258c92a7abf6c112e54912ab15bbb9eb0"
        /// On Windows machines, The thumprint for an installed certificate can be located
        /// By using certlm.msc and opening the certificate under the 'Details' tab.
        [<CustomOperation("thumpbrint");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Thumpbrint (state: string list, x: string) = 
            quotedField "thumpbrint" x::state
        /// Valid Thumprint required (if use-thumbprint-over-file is true)
        /// A typical thumbprint is a format similar to: "45df32e258c92a7abf6c112e54912ab15bbb9eb0"
        /// On Windows machines, The thumprint for an installed certificate can be located
        /// By using certlm.msc and opening the certificate under the 'Details' tab.
        member this.thumpbrint value =
            this.Thumpbrint([], value)
            |> this.Run
        
        /// The Store name. Under windows The most common option is "My", which indicates the personal store.
        /// See System.Security.Cryptography.X509Certificates.StoreName for other common values.
        [<CustomOperation("store_name");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StoreName (state: string list, x: string) = 
            quotedField "store-name" x::state
        /// The Store name. Under windows The most common option is "My", which indicates the personal store.
        /// See System.Security.Cryptography.X509Certificates.StoreName for other common values.
        member this.store_name value =
            this.StoreName([], value)
            |> this.Run
        
        /// Valid options : local-machine or current-user
        ///
        /// current-user indicates a certificate stored under the user's account
        ///
        /// local-machine indicates a certificate stored at an operating system level (potentially shared by users)
        [<CustomOperation("store_location");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StoreLocation (state: string list, x: Hocon.Remoting.StoreLocation) = 
            quotedField "store-location" (x.Text)::state
        /// Valid options : local-machine or current-user
        ///
        /// current-user indicates a certificate stored under the user's account
        ///
        /// local-machine indicates a certificate stored at an operating system level (potentially shared by users)
        member this.store_location value =
            this.StoreLocation([], value)
            |> this.Run
        
    let certificate = 
        { new CertificateBuilder<Akka.Remote.Certificate.Field>() with
            member _.Run (state: string list) = 
                objExpr "certificate" 4 state
                |> Akka.Remote.Certificate.Field }
    
    [<AbstractClass>]
    type SslProtocolBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.Batching.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.Certificate.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.WorkerPool.Field) = [x.ToString()]
        
        /// The class given here must implement the akka.remote.transport.Transport
        /// interface and offer a public constructor which takes two arguments:
        ///
        ///  1) akka.actor.ExtendedActorSystem
        ///
        ///  2) com.typesafe.config.Config
        [<CustomOperation("transport_class");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TransportClass (state: string list, x: string) = 
            quotedField "transport-class" x::state
        /// The class given here must implement the akka.remote.transport.Transport
        /// interface and offer a public constructor which takes two arguments:
        ///
        ///  1) akka.actor.ExtendedActorSystem
        ///
        ///  2) com.typesafe.config.Config
        member this.transport_class value =
            this.TransportClass([], value)
            |> this.Run
        
        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters list. The last adapter in the
        /// list is the adapter immediately above the driver, while
        /// the first one is the top of the stack below the standard
        /// Akka protocol 
        [<CustomOperation("applied_adapters");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AppliedAdapters (state: string list, x: string list) = 
            quotedListField "applied-adapters" x::state
        /// Transport drivers can be augmented with adapters by adding their
        /// name to the applied-adapters list. The last adapter in the
        /// list is the adapter immediately above the driver, while
        /// the first one is the top of the stack below the standard
        /// Akka protocol 
        member this.applied_adapters value =
            this.AppliedAdapters([], value)
            |> this.Run
            
        /// Byte order used for network communication. Event thou DotNetty is big-endian
        /// by default, we need to switch it back to little endian in order to support
        /// backward compatibility with Helios.
        [<CustomOperation("byte_order");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ByteOrder (state: string list, x: Hocon.ByteOrder) = 
            quotedField "byte-order" (x.Text)::state
        /// Byte order used for network communication. Event thou DotNetty is big-endian
        /// by default, we need to switch it back to little endian in order to support
        /// backward compatibility with Helios.
        member this.byte_order value =
            this.ByteOrder([], value)
            |> this.Run
        
        /// The default remote server port clients should connect to.
        /// Default is 2552 (AKKA), use 0 if you want a random available port
        /// This port needs to be unique for each actor system on the same machine.
        [<CustomOperation("port");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Port (state: string list, x) =
            positiveField "port" x::state
        /// The default remote server port clients should connect to.
        /// Default is 2552 (AKKA), use 0 if you want a random available port
        /// This port needs to be unique for each actor system on the same machine.
        member inline this.port value =
            this.Port([], value)
            |> this.Run
            
        /// Similar in spirit to "public-hostname" setting, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// "port" setting, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        ///
        /// Leave this setting to "0" if you don't intend to use it.
        [<CustomOperation("public_port");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PublicPort (state: string list, x) =
            positiveField "public-port" x::state
        /// Similar in spirit to "public-hostname" setting, this allows Akka.Remote users
        /// to alias the port they're listening on. The socket will actually listen on the
        /// "port" setting, but when connecting to other ActorSystems this node will advertise
        /// itself as being connected to the "public-port". This is helpful when working with 
        /// hosting environments that rely on address translation and port-forwarding, such as Docker.
        ///
        /// Leave this setting to "0" if you don't intend to use it.
        member inline this.public_port value =
            this.PublicPort([], value)
            |> this.Run
            
        /// The hostname or ip to bind the remoting to,
        /// InetAddress.getLocalHost.getHostAddress is used if empty
        [<CustomOperation("hostname");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Hostname (state: string list, x: string) = 
            field "hostname" x::state
        /// The hostname or ip to bind the remoting to,
        /// InetAddress.getLocalHost.getHostAddress is used if empty
        member this.hostname value =
            this.Hostname([], value)
            |> this.Run
            
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname)
        /// this is designed to make it easy to support private / public addressing schemes
        [<CustomOperation("public_hostname");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.PublicHostname (state: string list, x: string) = 
            field "public-hostname" x::state
        /// If this value is set, this becomes the public address for the actor system on this
        /// transport, which might be different than the physical ip address (hostname)
        /// this is designed to make it easy to support private / public addressing schemes
        member this.public_hostname value =
            this.PublicHostname([], value)
            |> this.Run
        
        /// If set to true, we will use IPV6 addresses upon DNS resolution for host names.
        /// Otherwise, we will use IPV4.
        [<CustomOperation("dns_use_ipv6");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DnsUseIpv6 (state: string list, value: bool) = 
            boolField "dns-use-ipv6" value::state
        /// If set to true, we will use IPV6 addresses upon DNS resolution for host names.
        /// Otherwise, we will use IPV4.
        member this.dns_use_ipv6 value =
            this.DnsUseIpv6([], value)
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
        member _.EnforceIpFamily (state: string list, value: bool) = 
            boolField "enforce-ip-family" value::state
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
            this.EnforceIpFamily([], value)
            |> this.Run
            
        /// Enables SSL support on this transport
        [<CustomOperation("enable_ssl");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnableSsl (state: string list, value: bool) = 
            boolField "enable-ssl" value::state
        /// Enables SSL support on this transport
        member this.enable_ssl value =
            this.EnableSsl([], value)
            |> this.Run
            
        /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
        [<CustomOperation("enable_backwards_compatibility");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.EnableBackwardsCompatibility (state: string list, value: bool) = 
            boolField "enable-backwards-compatibility" value::state
        /// Enables backwards compatibility with Akka.Remote clients running Helios 1.*
        member this.enable_backwards_compatibility value =
            this.EnableBackwardsCompatibility([], value)
            |> this.Run
            
        /// Sets the connectTimeoutMillis of all outbound connections,
        /// i.e. how long a connect may take until it is timed out
        [<CustomOperation("connection_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ConnectionTimeout (state: string list, value: 'Duration) =
            durationField "connection-timeout" value::state
        /// Sets the connectTimeoutMillis of all outbound connections,
        /// i.e. how long a connect may take until it is timed out
        member inline this.connection_timeout (value: 'Duration) =
            this.ConnectionTimeout([], value)
            |> this.Run
            
        /// If set to "<id.of.dispatcher>" then the specified dispatcher
        /// will be used to accept inbound connections, and perform IO. If "" then
        /// dedicated threads will be used.
        ///
        /// Please note that the Helios driver only uses this configuration and does
        /// not read the "akka.remote.use-dispatcher" entry. Instead it has to be
        /// configured manually to point to the same dispatcher if needed.
        [<CustomOperation("use_dispatcher_for_io");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseDispatcherForIo (state: string list, x: string) = 
            quotedField "use-dispatcher-for-io" x::state
        /// If set to "<id.of.dispatcher>" then the specified dispatcher
        /// will be used to accept inbound connections, and perform IO. If "" then
        /// dedicated threads will be used.
        ///
        /// Please note that the Helios driver only uses this configuration and does
        /// not read the "akka.remote.use-dispatcher" entry. Instead it has to be
        /// configured manually to point to the same dispatcher if needed.
        member this.use_dispatcher_for_io value =
            this.UseDispatcherForIo([], value)
            |> this.Run
        
        /// Sets the high water mark for the in and outbound sockets,
        /// set to 0b for platform default
        [<CustomOperation("write_buffer_high_water_mark");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WriteBufferHighWaterMark (state: string list, value) =
            byteField "write-buffer-high-water-mark" value::state
        /// Sets the high water mark for the in and outbound sockets,
        /// set to 0b for platform default
        member inline this.write_buffer_high_water_mark value =
            this.WriteBufferHighWaterMark([], value)
            |> this.Run
            
        /// Sets the low water mark for the in and outbound sockets,
        /// set to 0b for platform default
        [<CustomOperation("write_buffer_low_water_mark");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.WriteBufferLowhWaterMark (state: string list, value) =
            byteField "write-buffer-low-water-mark" value::state
        /// Sets the low water mark for the in and outbound sockets,
        /// set to 0b for platform default
        member inline this.write_buffer_low_water_mark value =
            this.WriteBufferLowhWaterMark([], value)
            |> this.Run
            
        /// Sets the send buffer size of the Sockets,
        /// set to 0b for platform default
        [<CustomOperation("send_buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SendBufferSize (state: string list, value) =
            byteField "send-buffer-size" value::state
        /// Sets the send buffer size of the Sockets,
        /// set to 0b for platform default
        member inline this.send_buffer_size value =
            this.SendBufferSize([], value)
            |> this.Run
            
        /// Sets the receive buffer size of the Sockets,
        /// set to 0b for platform default
        [<CustomOperation("receive_buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ReceiveBufferSize (state: string list, value) =
            byteField "receive-buffer-size" value::state
        /// Sets the receive buffer size of the Sockets,
        /// set to 0b for platform default
        member inline this.receive_buffer_size value =
            this.ReceiveBufferSize([], value)
            |> this.Run
            
        /// Maximum message size the transport will accept, but at least
        /// 32000 bytes.
        ///
        /// Please note that UDP does not support arbitrary large datagrams,
        /// so this setting has to be chosen carefully when using UDP.
        /// Both send-buffer-size and receive-buffer-size settings has to
        /// be adjusted to be able to buffer messages of maximum size.
        [<CustomOperation("maximum_frame_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaximumFrameSize (state: string list, value) =
            byteField "maximum-frame-size" value::state
        /// Maximum message size the transport will accept, but at least
        /// 32000 bytes.
        ///
        /// Please note that UDP does not support arbitrary large datagrams,
        /// so this setting has to be chosen carefully when using UDP.
        /// Both send-buffer-size and receive-buffer-size settings has to
        /// be adjusted to be able to buffer messages of maximum size.
        member inline this.maximum_frame_size value =
            this.MaximumFrameSize([], value)
            |> this.Run
            
        /// Sets the size of the connection backlog
        [<CustomOperation("backlog");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Backlog (state: string list, value) =
            positiveField "backlog" value::state
        /// Sets the size of the connection backlog
        member inline this.backlog value =
            this.Backlog([], value)
            |> this.Run

        [<CustomOperation("suppress_validation");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SuppressValidation (state: string list, value: bool) = 
            boolField "suppress-validation" value::state
        member this.suppress_validation value =
            this.SuppressValidation([], value)
            |> this.Run
        
        /// configured manually to point to the same dispatcher if needed.
        [<CustomOperation("transport_protocol");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TransportProtocol (state: string list, x: string) = 
            field "transport-protocol" x::state
        /// configured manually to point to the same dispatcher if needed.
        member this.transport_protocol value =
            this.TransportProtocol([], value)
            |> this.Run
        
        member _.client_socket_worker_pool = 
            { new WorkerPoolBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "client-socket-worker-pool" state }

        member _.server_socket_worker_pool = 
            { new WorkerPoolBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "server-socket-worker-pool" state }
        
        member _.certificate = 
            { new CertificateBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "certificate" state }
        
    let ssl = 
        let path = "ssl"
        let indent = 4

        { new SslProtocolBuilder<Akka.Remote.DotNetty.Field>(Akka.Remote.DotNetty.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.Remote.DotNetty.Field }
    
    [<AbstractClass>]
    type DotNettyBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()
        
        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Remote.DotNetty.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Shared.Field<Akka.Shared.Tcp.Field>) = [(x 4).ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Shared.Field<Akka.Shared.Udp.Field>) = [(x 4).ToString()]

        // Pathing

        member _.tcp =
            { new TcpBuilder<'T>(mkField, "dot-netty.tcp") with
                member _.Run (state: SharedInternal.State<Tcp.Target>) = 
                    fun _ -> pathObjExpr "tcp" state.Fields }

        member _.udp = 
            { new UdpBuilder<'T>(mkField, "dot-netty.udp") with
                member _.Run (state: SharedInternal.State<Udp.Target>) = 
                    fun _ -> pathObjExpr "udp" state.Fields }
        
        member _.ssl = 
            { new SslProtocolBuilder<'T>(mkField, "dot-netty.ssl", indent) with
                member _.Run (state: string list) = pathObjExpr "ssl" state }

    let dot_netty = 
        let path = "dot-netty"
        let indent = 3

        { new DotNettyBuilder<Akka.Remote.Field>(Akka.Remote.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.Remote.Field }
