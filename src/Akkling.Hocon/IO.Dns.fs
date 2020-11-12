namespace Akkling.Hocon

[<AutoOpen>]
module Dns =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type INetAddressBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()

        /// Must implement akka.io.DnsProvider
        [<CustomOperation("provider_object");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ProviderObject (state: string list, value: string) =
            quotedField "provider-object" value::state
        /// Must implement akka.io.DnsProvider
        member this.provider_object value =
            this.ProviderObject([], value)
            |> this.Run
        
        [<CustomOperation("positive_ttl");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PositiveTtl (state: string list, value: 'Duration) =
            durationField "positive-ttl" value::state
        member inline this.positive_ttl (value: 'Duration) =
            this.PositiveTtl([], value)
            |> this.Run
        
        [<CustomOperation("negative_ttl");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.NegativeTtl (state: string list, value: 'Duration) =
            durationField "negative-ttl" value::state
        member inline this.negative_ttl (value: 'Duration) =
            this.NegativeTtl([], value)
            |> this.Run
        
        /// How often to sweep out expired cache entries.
        /// Note that this interval has nothing to do with TTLs
        [<CustomOperation("cache_cleanup_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.CacheCleanupInterval (state: string list, value: 'Duration) =
            durationField "cache-cleanup-interval" value::state
        /// How often to sweep out expired cache entries.
        /// Note that this interval has nothing to do with TTLs
        member inline this.cache_cleanup_interval (value: 'Duration) =
            this.CacheCleanupInterval([], value)
            |> this.Run

        [<CustomOperation("use_ipv6");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.UseIpv6 (state: string list, value: bool) =
            boolField "use-ipv6" value::state
        member this.use_ipv6 value =
            this.UseIpv6([], value)
            |> this.Run

    let inet_address =
        { new INetAddressBuilder<Akka.IO.INetAddress.Field>() with
            member _.Run (state: string list) = 
                objExpr "inet-address" 4 state
                |> Akka.IO.INetAddress.Field }

    [<AbstractClass>]
    type DnsBuilder<'T when 'T :> IField> (mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()

        let pathObjExpr = pathObjExpr mkField path indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.IO.INetAddress.Field) = [x.ToString()]

        /// Fully qualified config path which holds the dispatcher configuration
        /// for the manager and resolver router actors.
        /// For actual router configuration see akka.actor.deployment./IO-DNS/*
        [<CustomOperation("dispatcher");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Dispatcher (state: string list, value: string) =
            quotedField "dispatcher" value::state
        /// Fully qualified config path which holds the dispatcher configuration
        /// for the manager and resolver router actors.
        /// For actual router configuration see akka.actor.deployment./IO-DNS/*
        member this.dispatcher value =
            this.Dispatcher([], value)
            |> this.Run
            
        /// Name of the subconfig at path akka.io.dns
        [<CustomOperation("resolver");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Resolver (state: string list, value: string) =
            quotedField "resolver" value::state
        /// Name of the subconfig at path akka.io.dns
        member this.resolver value =
            this.Resolver([], value)
            |> this.Run
            
        // Pathing

        member _.inet_address =
            { new INetAddressBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "inet-address" state }

    let dns =
        let path = "dns"
        let indent = 3

        { new DnsBuilder<Akka.IO.Field>(Akka.IO.Field, path, indent) with
            member _.Run (state: string list) = 
                objExpr path indent state
                |> Akka.IO.Field }
