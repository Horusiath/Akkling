namespace Akkling.Hocon

[<AutoOpen>]
module Pools =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type DirectBufferPoolBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        /// Class implementing `Akka.IO.Buffers.IBufferPool` interface, which
        /// will be created with this configuration.
        [<CustomOperation("class'");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Class (state: string list, value: string) =
            quotedField "class" value::state
        member this.class' value =
            this.Class([], value)
            |> this.Run
            
        /// Size of a single byte buffer in bytes.
        [<CustomOperation("buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BufferSize (state: string list, value) =
            positiveField "buffer-size" value::state
        /// Size of a single byte buffer in bytes.
        member inline this.buffer_size value =
            this.BufferSize([], value)
            |> this.Run
            
        /// Number of byte buffers per segment. Every segement is a single continuous
        /// byte array in memory. Once buffer pool will run out of byte buffers to 
        /// lend it will allocate a next segment of memory. 
        /// Each segments size is equal to `buffer-size` * `buffers-per-segment`.
        [<CustomOperation("buffers_per_segment");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BuffersPerSegment (state: string list, value) =
            positiveField "buffers-per-segment" value::state
        member inline this.buffers_per_segment value =
            this.BuffersPerSegment([], value)
            |> this.Run
    
        /// Number of segments to be created at extension start.
        [<CustomOperation("initial_segments");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.InitialSegments (state: string list, value) =
            positiveField "initial-segments" value::state
        /// Number of segments to be created at extension start.
        member inline this.initial_segments value =
            this.InitialSegments([], value)
            |> this.Run
            
        /// Maximum number of segments that can be created by this byte buffer pool
        /// instance. Once this limit will be reached, a next allocation attempt will
        /// cause `BufferPoolAllocationException` to be thrown.
        [<CustomOperation("buffer_pool_limit");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BufferPoolLimit (state: string list, value) =
            positiveField "buffer-pool-limit" value::state
        /// Maximum number of segments that can be created by this byte buffer pool
        /// instance. Once this limit will be reached, a next allocation attempt will
        /// cause `BufferPoolAllocationException` to be thrown.
        member inline this.buffer_pool_limit value =
            this.BufferPoolLimit([], value)
            |> this.Run

    /// Implementation of `Akka.IO.Buffers.IBufferPool` interface. It
    /// allocates memory is so called segments. Each segment is then cut into 
    /// buffers of equal size (see: `buffers-per-segment`). Those buffers are 
    /// then lend to the requestor. They have to be released later on.
    let direct_buffer_pool =
        { new DirectBufferPoolBuilder<Akka.IO.DirectBufferPool.Field>() with
            member _.Run (state: string list) = 
                objExpr "direct-buffer-pool" 4 state
                |> Akka.IO.DirectBufferPool.Field }

    [<AbstractClass>]
    type DisabledBufferPoolBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        /// Class implementing `Akka.IO.Buffers.IBufferPool` interface, which
        /// will be created with this configuration.
        [<CustomOperation("class'");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Class (state: string list, value: string) =
            quotedField "class" value::state
        /// Class implementing `Akka.IO.Buffers.IBufferPool` interface, which
        /// will be created with this configuration.
        member this.class' value =
            this.Class([], value)
            |> this.Run
            
        /// Size of a single byte buffer in bytes.
        [<CustomOperation("buffer_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.BufferSize (state: string list, value) =
            positiveField "buffer-size" value::state
        /// Size of a single byte buffer in bytes.
        member this.buffer_size value =
            this.BufferSize([], value)
            |> this.Run
                
    /// Default implementation of `Akka.IO.Buffers.IBufferPool` interface. 
    /// Instead of maintaining allocated buffers and reusing them
    /// between different SocketAsyncEventArgs instances, it allocates new 
    /// buffer each time when new socket connection is established.
    let disabled_buffer_pool =
        { new DisabledBufferPoolBuilder<Akka.IO.DisabledBufferPool.Field>() with
            member _.Run (state: string list) = 
                objExpr "disabled-buffer-pool" 4 state
                |> Akka.IO.DisabledBufferPool.Field }
