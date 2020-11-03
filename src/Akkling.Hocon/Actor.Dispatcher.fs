namespace Akkling.Hocon

[<AutoOpen>]
module Dispatcher =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel
    
    /// For running in current synchronization contexts
    let current_context_executor = 
        { new BaseBuilder<Akka.Actor.CurrentContextExecutor.Field>() with
            member _.Run (state: string list) =
                objExpr "current-context-executor" 4 state
                |> Akka.Actor.CurrentContextExecutor.Field }
    
    /// This will be used if you have set "executor = "default-executor" (and by default)"
    let default_executor = 
        { new BaseBuilder<Akka.Actor.DefaultExecutor.Field>() with
            member _.Run (state: string list) =
                objExpr "default-executor" 4 state
                |> Akka.Actor.DefaultExecutor.Field }

    [<AbstractClass>]
    type ForkJoinExecutorBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
            
        /// Min number of threads to cap factor-based parallelism number to
        [<CustomOperation("parallelism_min");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ParallelismMin (state: string list, value: int) =
            positiveField "parallelism-min" value::state
        /// Min number of threads to cap factor-based parallelism number to
        member this.parallelism_min value =
            this.ParallelismMin([], value)
            |> this.Run
            
        /// Min number of threads to cap factor-based parallelism number to
        [<CustomOperation("parallelism_factor");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ParallelismFactor (state: string list, value) =
            positiveFieldf "parallelism-factor" value::state
        /// Min number of threads to cap factor-based parallelism number to
        member this.parallelism_factor value =
            this.ParallelismFactor([], value)
            |> this.Run
            
        /// Max number of threads to cap factor-based parallelism number to
        [<CustomOperation("parallelism_max");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ParallelismMax (state: string list, value: int) =
            positiveField "parallelism-max" value::state
        /// Max number of threads to cap factor-based parallelism number to
        member this.parallelism_max value =
            this.ParallelismMax([], value)
            |> this.Run
            
        /// Setting to "FIFO" to use queue like peeking mode which "poll" or "LIFO" to use stack
        /// like peeking mode which "pop".
        [<CustomOperation("task_peeking_mode");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.TaskPeekingMode (state: string list, value: Hocon.TaskPeekingMode) =
            field "task-peeking-mode" value.Text::state
        /// Setting to "FIFO" to use queue like peeking mode which "poll" or "LIFO" to use stack
        /// like peeking mode which "pop".
        member this.task_peeking_mode value =
            this.TaskPeekingMode([], value)
            |> this.Run
            
    /// This will be used if you have set "executor = "fork-join-executor""
    ///
    /// Underlying thread pool implementation is scala.concurrent.forkjoin.ForkJoinPool
    let fork_join_executor = 
        { new ForkJoinExecutorBuilder<Akka.Actor.ForkJoinExecutor.Field>() with
            member _.Run (state: string list) =
                objExpr "fork-join-executor" 4 state
                |> Akka.Actor.ForkJoinExecutor.Field }

    /// This will be used if you have set "executor = "thread-pool-executor""
    let thread_pool_executor = 
        { new BaseBuilder<Akka.Actor.ThreadPoolExecutor.Field>() with
            member _.Run (state: string list) =
                objExpr "thread-pool-executor" 4 state
                |> Akka.Actor.ThreadPoolExecutor.Field }
    
    [<AbstractClass>]
    type DedicatedThreadPoolBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
            
        /// Number of threads
        [<CustomOperation("thread_count");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ThreadCount (state: string list, value: int) =
            positiveField "thread-count" value::state
        /// Number of threads
        member this.thread_count value =
            this.ThreadCount([], value)
            |> this.Run
            
        [<CustomOperation("deadlock_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DeadlockTimeout (state: string list, value: 'Duration) =
            durationField "deadlock-timeout" value::state
        member inline this.deadlock_timeout (value: 'Duration) =
            this.DeadlockTimeout([], value)
            |> this.Run
            
        [<CustomOperation("threadtype");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Threadtype (state: string list, x: Hocon.ThreadType) =
            field "threadtype" x.Text::state
        member this.threadtype value =
            this.Threadtype([], value)
            |> this.Run
            
    /// Settings for Helios.DedicatedThreadPool
    let dedicated_thread_pool = 
        { new DedicatedThreadPoolBuilder<Akka.Actor.DedicatedThreadPool.Field>() with
            member _.Run (state: string list) =
                objExpr "dedicated-thread-pool" 4 state
                |> Akka.Actor.DedicatedThreadPool.Field }
    
    [<AbstractClass>]
    type DispatcherBuilder<'T when 'T :> IField> (name: string, mkField: string -> 'T, path: string, indent: int) =
        inherit BaseBuilder<'T>()

        let pathObjExpr = pathObjExpr mkField (sprintf "%s.%s" path name) indent
        
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.CurrentContextExecutor.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.DefaultExecutor.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.ForkJoinExecutor.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.ThreadPoolExecutor.Field) = [x.ToString()]
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield (x: Akka.Actor.DedicatedThreadPool.Field) = [x.ToString()]
            
        /// For BalancingDispatcher: If the balancing dispatcher should attempt to
        /// schedule idle actors using the same dispatcher when a message comes in,
        /// and the dispatchers ExecutorService is not fully busy already.
        [<CustomOperation("attempt_teamwork");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.AttemptTeamwork (state: string list, value: bool) = 
            switchField "attempt-teamwork" value::state
        /// For BalancingDispatcher: If the balancing dispatcher should attempt to
        /// schedule idle actors using the same dispatcher when a message comes in,
        /// and the dispatchers ExecutorService is not fully busy already.
        member this.attempt_teamwork value =
            this.AttemptTeamwork([], value)
            |> this.Run

        /// Must be one of the following
        /// Dispatcher, PinnedDispatcher, or a FQCN to a class inheriting
        /// MessageDispatcherConfigurator with a public constructor with
        /// both com.typesafe.config.Config parameter and
        /// akka.dispatch.DispatcherPrerequisites parameters.
        ///
        /// PinnedDispatcher must be used together with executor=fork-join-executor.
        [<CustomOperation("type'");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Type' (state: string list, x: Hocon.Dispatcher) =
            field "type" x.Text::state
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Type' (state: string list, x: string) =
            quotedField "type" x::state
        /// Must be one of the following
        /// Dispatcher, PinnedDispatcher, or a FQCN to a class inheriting
        /// MessageDispatcherConfigurator with a public constructor with
        /// both com.typesafe.config.Config parameter and
        /// akka.dispatch.DispatcherPrerequisites parameters.
        ///
        /// PinnedDispatcher must be used together with executor=fork-join-executor.
        member this.type' (value: Hocon.Dispatcher) =
            this.Type'([], value)
            |> this.Run
        /// Must be one of the following
        /// Dispatcher, PinnedDispatcher, or a FQCN to a class inheriting
        /// MessageDispatcherConfigurator with a public constructor with
        /// both com.typesafe.config.Config parameter and
        /// akka.dispatch.DispatcherPrerequisites parameters.
        ///
        /// PinnedDispatcher must be used together with executor=fork-join-executor.
        member this.type' (value: string) =
            this.Type'([], value)
            |> this.Run
            
        /// Which kind of ExecutorService to use for this dispatcher
        ///
        /// Valid options:
        /// - "default-executor" requires a "default-executor" section
        ///
        /// - "fork-join-executor" requires a "fork-join-executor" section
        ///
        /// - "thread-pool-executor" requires a "thread-pool-executor" section
        ///
        /// - "current-context-executor" requires a "current-context-executor" section
        ///
        /// - "task-executor" requires a "task-executor" section
        ///
        /// - A FQCN of a class extending ExecutorServiceConfigurator
        [<CustomOperation("executor");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Executor (state: string list, x: Hocon.Executor) =
            quotedField "executor" x.Text::state
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Executor (state: string list, x: string) =
            quotedField "executor" x::state
        /// Which kind of ExecutorService to use for this dispatcher
        ///
        /// Valid options:
        /// - "default-executor" requires a "default-executor" section
        ///
        /// - "fork-join-executor" requires a "fork-join-executor" section
        ///
        /// - "thread-pool-executor" requires a "thread-pool-executor" section
        ///
        /// - "current-context-executor" requires a "current-context-executor" section
        ///
        /// - "task-executor" requires a "task-executor" section
        ///
        /// - A FQCN of a class extending ExecutorServiceConfigurator
        member this.executor (value: Hocon.Executor) =
            this.Executor([], value)
            |> this.Run
        /// Which kind of ExecutorService to use for this dispatcher
        ///
        /// Valid options:
        /// - "default-executor" requires a "default-executor" section
        ///
        /// - "fork-join-executor" requires a "fork-join-executor" section
        ///
        /// - "thread-pool-executor" requires a "thread-pool-executor" section
        ///
        /// - "current-context-executor" requires a "current-context-executor" section
        ///
        /// - "task-executor" requires a "task-executor" section
        ///
        /// - A FQCN of a class extending ExecutorServiceConfigurator
        member this.executor (value: string) =
            this.Executor([], value)
            |> this.Run
            
        /// If this dispatcher requires a specific type of mailbox, specify the
        /// fully-qualified class name here; the actually created mailbox will
        /// be a subtype of this type. 
        ///
        /// An empty string signifies no requirement.
        [<CustomOperation("mailbox_requirement");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MailboxRequirement (state: string list, x: string) =
            quotedField "mailbox-requirement" x::state
        /// If this dispatcher requires a specific type of mailbox, specify the
        /// fully-qualified class name here; the actually created mailbox will
        /// be a subtype of this type. 
        ///
        /// An empty string signifies no requirement.
        member this.mailbox_requirement value =
            this.MailboxRequirement([], value)
            |> this.Run

        /// How long time the dispatcher will wait for new actors until it shuts down
        [<CustomOperation("shutdown_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ShutdownTimeout (state: string list, value: 'Duration) =
            durationField "shutdown-timeout" value::state
        /// How long time the dispatcher will wait for new actors until it shuts down
        member inline this.shutdown_timeout (value: 'Duration) =
            this.ShutdownTimeout([], value)
            |> this.Run
            
        /// Throughput defines the number of messages that are processed in a batch
        /// before the thread is returned to the pool. 
        ///
        /// Set to 1 for as fair as possible.
        [<CustomOperation("throughput");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Throughput (state: string list, value: int) =
            positiveField "throughput" value::state
        /// Throughput defines the number of messages that are processed in a batch
        /// before the thread is returned to the pool. 
        ///
        /// Set to 1 for as fair as possible.
        member this.throughput value =
            this.Throughput([], value)
            |> this.Run
            
        /// Throughput deadline for Dispatcher, set to 0 for no deadline
        [<CustomOperation("throughput_deadline_time");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ThroughputDeadlineTime (state: string list, value: 'Duration) =
            durationField "throughput-deadline-time" value::state
        /// Throughput deadline for Dispatcher, set to 0 for no deadline
        member inline this.throughput_deadline_time (value: 'Duration) =
            this.ThroughputDeadlineTime([], value)
            |> this.Run

        // Pathing

        member _.current_context_executor = 
            { new BaseBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "current-context-executor" state }
        
        /// This will be used if you have set "executor = "default-executor" (and by default)"
        member _.default_executor = 
            { new BaseBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "default-executor" state }

        member _.fork_join_executor = 
            { new ForkJoinExecutorBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "fork-join-executor" state }

        /// This will be used if you have set "executor = "thread-pool-executor""
        member _.thread_pool_executor = 
            { new BaseBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "thread-pool-executor" state }

        member _.dedicated_thread_pool = 
            { new DedicatedThreadPoolBuilder<'T>() with
                member _.Run (state: string list) = pathObjExpr "dedicated-thread-pool" state }
    
    let custom_dispatcher (name: string) =
        let indent = 3

        { new DispatcherBuilder<Akka.Actor.Dispatcher.Field>(name, Akka.Actor.Dispatcher.Field, name, indent) with
            member _.Run (state: string list) =
                objExpr name indent state
                |> Akka.Actor.Dispatcher.Field }

    let backoff_remote_dispatcher = custom_dispatcher "backoff-remote-dispatcher"
    let calling_thread_dispatcher = custom_dispatcher "calling-thread-dispatcher"
    let default_dispatcher = custom_dispatcher "default-dispatcher"
    let dispatcher = custom_dispatcher "dispatcher"
    let default_blocking_io_dispatcher = custom_dispatcher "default-blocking-io-dispatcher"
    let default_fork_join_dispatcher = custom_dispatcher "default-fork-join-dispatcher"
    let default_plugin_dispatcher = custom_dispatcher "default-plugin-dispatcher"
    let default_remote_dispatcher = custom_dispatcher "default-remote-dispatcher"
    let default_replay_dispatcher = custom_dispatcher "default-replay-dispatcher"
    let default_stream_dispatcher = custom_dispatcher "default-stream-dispatcher"

    /// Default separate internal dispatcher to run Akka internal tasks and actors on
    /// protecting them against starvation because of accidental blocking in user actors (which run on the
    /// default dispatcher
    let internal_dispatcher = custom_dispatcher "internal-dispatcher"

    let pinned_dispatcher = custom_dispatcher "pinned-dispatcher"

    /// Used for GUI applications
    let synchronized_dispatcher = custom_dispatcher "synchronized-dispatcher"
    let task_dispatcher = custom_dispatcher "task-dispatcher"
