namespace Akkling.Hocon

[<AutoOpen>]
module Scheduler =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type SchedulerBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        /// The LightArrayRevolverScheduler is used as the default scheduler in the
        /// system. It does not execute the scheduled tasks on exact time, but on every
        /// tick, it will run everything that is (over)due. You can increase or decrease
        /// the accuracy of the execution timing by specifying smaller or larger tick
        /// duration. If you are scheduling a lot of tasks you should consider increasing
        /// the ticks per wheel.
        ///
        /// Note that it might take up to 1 tick to stop the Timer, so setting the
        /// tick-duration to a high value will make shutting down the actor system
        /// take longer.
        [<CustomOperation("tick_duration");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.TickDuration (state: string list, value: 'Duration) =
            durationField "tick-duration" value::state
        /// The LightArrayRevolverScheduler is used as the default scheduler in the
        /// system. It does not execute the scheduled tasks on exact time, but on every
        /// tick, it will run everything that is (over)due. You can increase or decrease
        /// the accuracy of the execution timing by specifying smaller or larger tick
        /// duration. If you are scheduling a lot of tasks you should consider increasing
        /// the ticks per wheel.
        ///
        /// Note that it might take up to 1 tick to stop the Timer, so setting the
        /// tick-duration to a high value will make shutting down the actor system
        /// take longer.
        member inline this.tick_duration (value: 'Duration) =
            this.TickDuration([], value)
            |> this.Run
            
        /// The timer uses a circular wheel of buckets to store the timer tasks.
        ///
        /// This should be set such that the majority of scheduled timeouts (for high
        /// scheduling frequency) will be shorter than one rotation of the wheel
        /// (ticks-per-wheel * ticks-duration)
        ///
        /// THIS MUST BE A POWER OF TWO!
        [<CustomOperation("ticks_per_wheel");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.TicksPerWheel (state: string list, value: int) =
            if (value &&& (value - 1)) = 0 then
                numField "ticks-per-wheel" value::state
            else failwithf "ticks-per-wheel must be a power of 2, was given: %i" value
        /// The timer uses a circular wheel of buckets to store the timer tasks.
        ///
        /// This should be set such that the majority of scheduled timeouts (for high
        /// scheduling frequency) will be shorter than one rotation of the wheel
        /// (ticks-per-wheel * ticks-duration)
        ///
        /// THIS MUST BE A POWER OF TWO!
        member inline this.ticks_per_wheel (value: int) =
            this.TicksPerWheel([], value)
            |> this.Run
            
        /// This setting selects the timer implementation which shall be loaded at
        /// system start-up.
        /// The class given here must implement the akka.actor.Scheduler interface
        /// and offer a public constructor which takes three arguments:
        ///
        ///  1) com.typesafe.config.Config
        ///
        ///  2) akka.event.LoggingAdapter
        ///
        ///  3) java.util.concurrent.ThreadFactory
        [<CustomOperation("implementation");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Implementation (state: string list, value: string) =
            quotedField "implementation" value::state
        /// This setting selects the timer implementation which shall be loaded at
        /// system start-up.
        /// The class given here must implement the akka.actor.Scheduler interface
        /// and offer a public constructor which takes three arguments:
        ///
        ///  1) com.typesafe.config.Config
        ///
        ///  2) akka.event.LoggingAdapter
        ///
        ///  3) java.util.concurrent.ThreadFactory
        member this.implementation value =
            this.Implementation([], value)
            |> this.Run
    
        /// When shutting down the scheduler, there will typically be a thread which
        /// needs to be stopped, and this timeout determines how long to wait for
        /// that to happen. In case of timeout the shutdown of the actor system will
        /// proceed without running possibly still enqueued tasks.
        [<CustomOperation("shutdown_timeout");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ShutdownTimeout (state: string list, value: 'Duration) =
            durationField "shutdown-timeout" value::state
        /// When shutting down the scheduler, there will typically be a thread which
        /// needs to be stopped, and this timeout determines how long to wait for
        /// that to happen. In case of timeout the shutdown of the actor system will
        /// proceed without running possibly still enqueued tasks.
        member inline this.shutdown_timeout (value: 'Duration) =
            this.ShutdownTimeout([], value)
            |> this.Run

    /// Used to set the behavior of the scheduler.
    ///
    /// Changing the default values may change the system behavior drastically so make
    /// sure you know what you're doing! 
    ///
    /// See the Scheduler section of the Akka documentation for more details.
    let scheduler = 
        { new SchedulerBuilder<Akka.Scheduler.Field>() with
            member _.Run (state: string list) = 
                objExpr "scheduler" 2 state
                |> Akka.Scheduler.Field }
