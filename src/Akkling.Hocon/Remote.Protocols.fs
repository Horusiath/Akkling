namespace Akkling.Hocon

[<AutoOpen>]
module Protocols =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    /// The batching feature of DotNetty is designed to help batch together logical writes into a smaller
    /// number of physical writes across the socket. This helps significantly reduce the number of system calls
    /// and can improve performance by as much as 150% on some systems when enabled.
    [<AbstractClass>]
    type BatchingBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Enables the batching system. When disabled, every write will be flushed immediately.
        /// Disable this setting if you're working with a VERY low-traffic system that requires
        /// fast (< 40ms) acknowledgement for all periodic messages.
        [<CustomOperation("enabled"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Enabled(state: string list, value: bool) = boolField "enabled" value :: state

        /// Enables the batching system. When disabled, every write will be flushed immediately.
        /// Disable this setting if you're working with a VERY low-traffic system that requires
        /// fast (< 40ms) acknowledgement for all periodic messages.
        member this.enabled value = this.Enabled([], value) |> this.Run

        /// The max write threshold based on the number of logical messages regardless of their size.
        /// This is a safe default value - decrease it if you have a small number of remote actors
        /// who engage in frequent request->response communication which requires low latency (< 40ms).
        [<CustomOperation("max_pending_writes"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaxPendingWrites(state: string list, value) =
            positiveField "max-pending-writes" value :: state

        /// The max write threshold based on the number of logical messages regardless of their size.
        /// This is a safe default value - decrease it if you have a small number of remote actors
        /// who engage in frequent request->response communication which requires low latency (< 40ms).
        member inline this.max_pending_writes value =
            this.MaxPendingWrites([], value) |> this.Run

        /// The max write threshold based on the byte size of all buffered messages. If there are 4 messages
        /// waiting to be written (with batching.max-pending-writes = 30) but their total size is greater than
        /// batching.max-pending-bytes, a flush will be triggered immediately.
        ///
        /// Increase this value is you have larger message sizes and watch to take advantage of batching, but
        /// otherwise leave it as-is.
        ///
        /// NOTE: this value should always be smaller than dot-netty.tcp.maximum-frame-size.
        [<CustomOperation("max_pending_bytes"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaxPendingBytes(state: string list, value) =
            byteField "max-pending-bytes" value :: state

        /// The max write threshold based on the byte size of all buffered messages. If there are 4 messages
        /// waiting to be written (with batching.max-pending-writes = 30) but their total size is greater than
        /// batching.max-pending-bytes, a flush will be triggered immediately.
        ///
        /// Increase this value is you have larger message sizes and watch to take advantage of batching, but
        /// otherwise leave it as-is.
        ///
        /// NOTE: this value should always be smaller than dot-netty.tcp.maximum-frame-size.
        member inline this.max_pending_bytes value =
            this.MaxPendingBytes([], value) |> this.Run

        /// In the event that neither the batching.max-pending-writes or batching.max-pending-bytes
        /// is hit we guarantee that all pending writes will be flushed within this interval.
        ///
        /// This setting, realistically, can't be enforced any lower than the OS' clock resolution (~20ms).
        /// If you have a very low-traffic system, either disable pooling altogether or lower the batching.max-pending-writes
        /// threshold to maximize throughput. Otherwise, leave this setting as-is.
        [<CustomOperation("flush_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FlushInterval(state: string list, value: 'Duration) =
            durationField "flush-interval" value :: state

        /// In the event that neither the batching.max-pending-writes or batching.max-pending-bytes
        /// is hit we guarantee that all pending writes will be flushed within this interval.
        ///
        /// This setting, realistically, can't be enforced any lower than the OS' clock resolution (~20ms).
        /// If you have a very low-traffic system, either disable pooling altogether or lower the batching.max-pending-writes
        /// threshold to maximize throughput. Otherwise, leave this setting as-is.
        member inline this.flush_interval(value: 'Duration) =
            this.FlushInterval([], value) |> this.Run

    let batching =
        { new BatchingBuilder<Akka.Remote.Batching.Field>() with
            member _.Run(state: string list) =
                objExpr "batching" 4 state |> Akka.Remote.Batching.Field }

    [<AbstractClass>]
    type WorkerPoolBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Min number of threads to cap factor-based number to
        [<CustomOperation("pool_size_min"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PoolSizeMin(state: string list, value) =
            positiveField "pool-size-min" value :: state

        /// Min number of threads to cap factor-based number to
        member inline this.pool_size_min value = this.PoolSizeMin([], value) |> this.Run

        /// The pool size factor is used to determine thread pool size
        /// using the following formula: ceil(available processors * factor).
        /// Resulting size is then bounded by the pool-size-min and
        /// pool-size-max values.
        [<CustomOperation("pool_size_factor"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PoolSizeFactor(state: string list, value) =
            positiveFieldf "pool-size-factor" value :: state

        /// The pool size factor is used to determine thread pool size
        /// using the following formula: ceil(available processors * factor).
        /// Resulting size is then bounded by the pool-size-min and
        /// pool-size-max values.
        member inline this.pool_size_factor value =
            this.PoolSizeFactor([], value) |> this.Run

        /// Max number of threads to cap factor-based number to
        [<CustomOperation("pool_size_max"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.PoolSizeMax(state: string list, value) =
            positiveField "pool-size-max" value :: state

        /// Max number of threads to cap factor-based number to
        member inline this.pool_size_max value = this.PoolSizeMax([], value) |> this.Run

    let client_socket_worker_pool =
        { new WorkerPoolBuilder<Akka.Remote.WorkerPool.Field>() with
            member _.Run(state: string list) =
                objExpr "client-socket-worker-pool" 4 state |> Akka.Remote.WorkerPool.Field }

    let server_socket_worker_pool =
        { new WorkerPoolBuilder<Akka.Remote.WorkerPool.Field>() with
            member _.Run(state: string list) =
                objExpr "server-socket-worker-pool" 4 state |> Akka.Remote.WorkerPool.Field }
