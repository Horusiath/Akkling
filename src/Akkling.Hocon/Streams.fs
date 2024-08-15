namespace Akkling.Hocon

[<AutoOpen>]
module Streams =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type SubscriptionTimeoutBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// When the subscription timeout is reached one of the following strategies on
        /// the "stale" publisher:
        /// cancel - cancel it (via `onError` or subscribing to the publisher and
        ///          `cancel()`ing the subscription right away
        /// warn   - log a warning statement about the stale element (then drop the
        ///          reference to it)
        /// noop   - do nothing (not recommended)
        /// Note: If you change this value also change the fallback value in StreamSubscriptionTimeoutSettings
        [<CustomOperation("mode"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Mode(state: string list, x: Hocon.SubscriptionTimeout.Mode) = field "mode" x.Text :: state

        /// When the subscription timeout is reached one of the following strategies on
        /// the "stale" publisher:
        /// cancel - cancel it (via `onError` or subscribing to the publisher and
        ///          `cancel()`ing the subscription right away
        /// warn   - log a warning statement about the stale element (then drop the
        ///          reference to it)
        /// noop   - do nothing (not recommended)
        /// Note: If you change this value also change the fallback value in StreamSubscriptionTimeoutSettings
        member this.mode value = this.Mode([], value) |> this.Run

        /// Time after which a subscriber / publisher is considered stale and eligible
        /// for cancelation (see `akka.stream.subscription-timeout.mode`)
        /// Note: If you change this value also change the fallback value in StreamSubscriptionTimeoutSettings
        [<CustomOperation("timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Timeout(state: string list, value: 'Duration) = durationField "timeout" value :: state

        /// Time after which a subscriber / publisher is considered stale and eligible
        /// for cancelation (see `akka.stream.subscription-timeout.mode`)
        /// Note: If you change this value also change the fallback value in StreamSubscriptionTimeoutSettings
        member inline this.timeout(value: 'Duration) = this.Timeout([], value) |> this.Run

    let subscription_timeout =
        { new SubscriptionTimeoutBuilder<Akka.Streams.Materializer.Field>() with
            member _.Run(state: string list) =
                objExpr "subscription-timeout" 4 state |> Akka.Streams.Materializer.Field }

    [<AbstractClass>]
    type StreamRefBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Buffer of a SinkRef that is used to batch Request elements from the other side of the stream ref
        ///
        /// The buffer will be attempted to be filled eagerly even while the local stage did not request elements,
        /// because the delay of requesting over network boundaries is much higher.
        [<CustomOperation("buffer_capacity"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BufferCapacity(state: string list, value: int) =
            positiveField "buffer-capacity" value :: state

        /// Buffer of a SinkRef that is used to batch Request elements from the other side of the stream ref
        ///
        /// The buffer will be attempted to be filled eagerly even while the local stage did not request elements,
        /// because the delay of requesting over network boundaries is much higher.
        member this.buffer_capacity value =
            this.BufferCapacity([], value) |> this.Run

        /// Demand is signalled by sending a cumulative demand message ("requesting messages until the n-th sequence number)
        /// Using a cumulative demand model allows us to re-deliver the demand message in case of message loss (which should
        /// be very rare in any case, yet possible -- mostly under connection break-down and re-establishment).
        ///
        /// The semantics of handling and updating the demand however are in-line with what Reactive Streams dictates.
        ///
        /// In normal operation, demand is signalled in response to arriving elements, however if no new elements arrive
        /// within `demand-redelivery-interval` a re-delivery of the demand will be triggered, assuming that it may have gotten lost.
        [<CustomOperation("demand_redelivery_interval"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DemandRedeliveryInterval(state: string list, value: 'Duration) =
            durationField "demand-redelivery-interval" value :: state

        /// Demand is signalled by sending a cumulative demand message ("requesting messages until the n-th sequence number)
        /// Using a cumulative demand model allows us to re-deliver the demand message in case of message loss (which should
        /// be very rare in any case, yet possible -- mostly under connection break-down and re-establishment).
        ///
        /// The semantics of handling and updating the demand however are in-line with what Reactive Streams dictates.
        ///
        /// In normal operation, demand is signalled in response to arriving elements, however if no new elements arrive
        /// within `demand-redelivery-interval` a re-delivery of the demand will be triggered, assuming that it may have gotten lost.
        member inline this.demand_redelivery_interval(value: 'Duration) =
            this.DemandRedeliveryInterval([], value) |> this.Run

        /// Subscription timeout, during which the "remote side" MUST subscribe (materialize) the handed out stream ref.
        /// This timeout does not have to be very low in normal situations, since the remote side may also need to
        /// prepare things before it is ready to materialize the reference. However the timeout is needed to avoid leaking
        /// in-active streams which are never subscribed to.
        [<CustomOperation("subscription_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SubscriptionTimeout(state: string list, value: 'Duration) =
            durationField "subscription-timeout" value :: state

        /// Subscription timeout, during which the "remote side" MUST subscribe (materialize) the handed out stream ref.
        /// This timeout does not have to be very low in normal situations, since the remote side may also need to
        /// prepare things before it is ready to materialize the reference. However the timeout is needed to avoid leaking
        /// in-active streams which are never subscribed to.
        member inline this.subscription_timeout(value: 'Duration) =
            this.SubscriptionTimeout([], value) |> this.Run

        /// In order to guard the receiving end of a stream ref from never terminating (since awaiting a Completion or Failed
        /// message) after / before a Terminated is seen, a special timeout is applied once Terminated is received by it.
        /// This allows us to terminate stream refs that have been targeted to other nodes which are Downed, and as such the
        /// other side of the stream ref would never send the "final" terminal message.
        ///
        /// The timeout specifically means the time between the Terminated signal being received and when the local SourceRef
        /// determines to fail itself, assuming there was message loss or a complete partition of the completion signal.
        [<CustomOperation("final_termination_signal_deadline"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FinalTerminationSignalDeadline(state: string list, value: 'Duration) =
            durationField "final-termination-signal-deadline" value :: state

        /// In order to guard the receiving end of a stream ref from never terminating (since awaiting a Completion or Failed
        /// message) after / before a Terminated is seen, a special timeout is applied once Terminated is received by it.
        /// This allows us to terminate stream refs that have been targeted to other nodes which are Downed, and as such the
        /// other side of the stream ref would never send the "final" terminal message.
        ///
        /// The timeout specifically means the time between the Terminated signal being received and when the local SourceRef
        /// determines to fail itself, assuming there was message loss or a complete partition of the completion signal.
        member inline this.final_termination_signal_deadline(value: 'Duration) =
            this.FinalTerminationSignalDeadline([], value) |> this.Run

    let stream_ref =
        { new StreamRefBuilder<Akka.Streams.Materializer.Field>() with
            member _.Run(state: string list) =
                objExpr "stream-ref" 4 state |> Akka.Streams.Materializer.Field }

    [<AbstractClass>]
    type MaterializerBuilder<'T when 'T :> IField>(mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()

        let pathObjExpr = pathObjExpr mkField path indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Shared.Debug.Field>) = [ (x 4).ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Streams.Materializer.Field) = [ x.ToString() ]

        /// Initial size of buffers used in stream elements
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("initial_input_buffer_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.InitialInputBufferSize(state: string list, value: int) =
            positiveField "initial-input-buffer-size" value :: state

        /// Initial size of buffers used in stream elements
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.initial_input_buffer_size value =
            this.InitialInputBufferSize([], value) |> this.Run

        /// Maximum size of buffers used in stream elements
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("max_input_buffer_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxInputBufferSize(state: string list, value: int) =
            positiveField "max-input-buffer-size" value :: state

        /// Maximum size of buffers used in stream elements
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.max_input_buffer_size value =
            this.MaxInputBufferSize([], value) |> this.Run

        /// Fully qualified config path which holds the dispatcher configuration
        /// to be used by FlowMaterialiser when creating Actors.
        /// When this value is left empty, the default-dispatcher will be used.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Dispatcher(state: string list, x: string) = quotedField "dispatcher" x :: state

        /// Fully qualified config path which holds the dispatcher configuration
        /// to be used by FlowMaterialiser when creating Actors.
        /// When this value is left empty, the default-dispatcher will be used.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.dispatcher value = this.Dispatcher([], value) |> this.Run

        /// Storage location of snapshot files.
        [<CustomOperation("blocking_io_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BlockingIoDispatcher(state: string list, x: string) =
            quotedField "blocking-io-dispatcher" x :: state

        /// Storage location of snapshot files.
        member this.blocking_io_dispatcher value =
            this.BlockingIoDispatcher([], value) |> this.Run

        /// Enable additional troubleshooting logging at DEBUG log level
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("debug_logging"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DebugLogging(state: string list, value: bool) =
            switchField "debug-logging" value :: state

        /// Enable additional troubleshooting logging at DEBUG log level
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.debug_logging value =
            this.DebugLogging([], value) |> this.Run

        /// Maximum number of elements emitted in batch if downstream signals large demand
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("output_burst_limit"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.OutputBurstLimit(state: string list, value: int) =
            positiveField "output-burst-limit" value :: state

        /// Maximum number of elements emitted in batch if downstream signals large demand
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.output_burst_limit value =
            this.OutputBurstLimit([], value) |> this.Run

        /// Enable automatic fusing of all graphs that are run. For short-lived streams
        /// this may cause an initial runtime overhead, but most of the time fusing is
        /// desirable since it reduces the number of Actors that are created.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("auto_fusing"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AutoFusing(state: string list, value: bool) =
            switchField "auto-fusing" value :: state

        /// Enable automatic fusing of all graphs that are run. For short-lived streams
        /// this may cause an initial runtime overhead, but most of the time fusing is
        /// desirable since it reduces the number of Actors that are created.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.auto_fusing value = this.AutoFusing([], value) |> this.Run

        /// Those stream elements which have explicit buffers (like mapAsync, mapAsyncUnordered,
        /// buffer, flatMapMerge, Source.actorRef, Source.queue, etc.) will preallocate a fixed
        /// buffer upon stream materialization if the requested buffer size is less than this
        /// configuration parameter. The default is very high because failing early is better
        /// than failing under load.
        ///
        /// Buffers sized larger than this will dynamically grow/shrink and consume more memory
        /// per element than the fixed size buffers.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("max_fixed_buffer_size"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MaxFixedBufferSize(state: string list, value: int) =
            positiveField "max-fixed-buffer-size" value :: state

        /// Those stream elements which have explicit buffers (like mapAsync, mapAsyncUnordered,
        /// buffer, flatMapMerge, Source.actorRef, Source.queue, etc.) will preallocate a fixed
        /// buffer upon stream materialization if the requested buffer size is less than this
        /// configuration parameter. The default is very high because failing early is better
        /// than failing under load.
        ///
        /// Buffers sized larger than this will dynamically grow/shrink and consume more memory
        /// per element than the fixed size buffers.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.max_fixed_buffer_size value =
            this.MaxFixedBufferSize([], value) |> this.Run

        /// Maximum number of sync messages that actor can process for stream to substream communication.
        /// Parameter allows to interrupt synchronous processing to get upsteam/downstream messages.
        /// Allows to accelerate message processing that happening withing same actor but keep system responsive.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        [<CustomOperation("sync_processing_limit"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.SyncProcessingLimit(state: string list, value: int) =
            positiveField "sync-processing-limit" value :: state

        /// Maximum number of sync messages that actor can process for stream to substream communication.
        /// Parameter allows to interrupt synchronous processing to get upsteam/downstream messages.
        /// Allows to accelerate message processing that happening withing same actor but keep system responsive.
        ///
        /// Note: If you change this value also change the fallback value in ActorMaterializerSettings
        member this.sync_processing_limit value =
            this.SyncProcessingLimit([], value) |> this.Run

        // Pathing

        member _.stream_ref =
            { new StreamRefBuilder<'T>() with
                member _.Run(state: string list) = pathObjExpr "stream-ref" state }

        member _.subscription_timeout =
            { new SubscriptionTimeoutBuilder<'T>() with
                member _.Run(state: string list) =
                    pathObjExpr "subscription-timeout" state }

    let materializer =
        let path = "materializer"
        let indent = 3

        { new MaterializerBuilder<Akka.Streams.Field>(Akka.Streams.Field, path, indent) with
            member _.Run(state: string list) =
                objExpr path indent state |> Akka.Streams.Field }

    [<AbstractClass>]
    type StreamBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        let indent = 2
        let pathObjExpr = pathObjExpr Akka.Field "actor" indent

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Streams.Field) = [ x.ToString() ]

        /// Deprecated, left here to not break Akka HTTP which refers to it
        [<CustomOperation("blocking_io_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.BlockingIoDispatcher(state: string list, value: string) =
            quotedField "blocking-io-dispatcher" value :: state

        /// Deprecated, left here to not break Akka HTTP which refers to it
        member this.blocking_io_dispatcher value =
            this.BlockingIoDispatcher([], value) |> this.Run

        /// Deprecated, will not be used unless user code refer to it, use 'akka.stream.materializer.blocking-io-dispatcher'
        /// instead, or if from code, prefer the 'ActorAttributes.IODispatcher' attribute
        [<CustomOperation("default_blocking_io_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.DefaultBlockingIoDispatcher(state: string list, value: string) =
            quotedField "default-blocking-io-dispatcher" value :: state

        /// Deprecated, will not be used unless user code refer to it, use 'akka.stream.materializer.blocking-io-dispatcher'
        /// instead, or if from code, prefer the 'ActorAttributes.IODispatcher' attribute
        member this.default_blocking_io_dispatcher value =
            this.DefaultBlockingIoDispatcher([], value) |> this.Run

        // Pathing

        member _.materializer =
            { new MaterializerBuilder<Akka.Field>(Akka.Field, "stream.materializer", indent) with
                member _.Run(state: string list) = pathObjExpr "materializer" state }

    let stream =
        { new StreamBuilder<Akka.Field>() with
            member _.Run(state: string list) = objExpr "stream" 2 state |> Akka.Field }
