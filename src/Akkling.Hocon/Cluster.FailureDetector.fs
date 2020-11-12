namespace Akkling.Hocon

[<AutoOpen>]
module FailureDetector =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type FailureDetectorBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()

        /// FQCN of the failure detector implementation.
        /// It must implement akka.remote.FailureDetector and have
        /// a public constructor with a com.typesafe.config.Config and
        /// akka.actor.EventStream parameter.
        [<CustomOperation("implementation_class");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ImplementationClass (state: string list, x: string) = 
            quotedField "implementation-class" x::state
        /// FQCN of the failure detector implementation.
        /// It must implement akka.remote.FailureDetector and have
        /// a public constructor with a com.typesafe.config.Config and
        /// akka.actor.EventStream parameter.
        member this.implementation_class value =
            this.ImplementationClass([], value)
            |> this.Run
        
        /// How often keep-alive heartbeat messages should be sent to each connection.
        [<CustomOperation("heartbeat_interval");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.HeartbeatInterval (state: string list, value: 'Duration) =
            durationField "heartbeat-interval" value::state
        /// How often keep-alive heartbeat messages should be sent to each connection.
        member inline this.heartbeat_interval (value: 'Duration) =
            this.HeartbeatInterval([], value)
            |> this.Run
        
        /// Defines the failure detector threshold.
        /// A low threshold is prone to generate many wrong suspicions but ensures
        /// a quick detection in the event of a real crash. Conversely, a high
        /// threshold generates fewer mistakes but needs more time to detect
        /// actual crashes.
        [<CustomOperation("threshold");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.Threshold (state: string list, x) = 
            positiveFieldf "threshold" x::state
        /// Defines the failure detector threshold.
        /// A low threshold is prone to generate many wrong suspicions but ensures
        /// a quick detection in the event of a real crash. Conversely, a high
        /// threshold generates fewer mistakes but needs more time to detect
        /// actual crashes.
        member inline this.threshold value =
            this.Threshold([], value)
            |> this.Run
            
        /// Number of the samples of inter-heartbeat arrival times to adaptively
        /// calculate the failure timeout for connections.
        [<CustomOperation("max_sample_size");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MaxSampleSize (state: string list, x) = 
            positiveField "max-sample-size" x::state
        /// Number of the samples of inter-heartbeat arrival times to adaptively
        /// calculate the failure timeout for connections.
        member inline this.max_sample_size value =
            this.MaxSampleSize([], value)
            |> this.Run
            
        /// Minimum standard deviation to use for the normal distribution in
        /// AccrualFailureDetector. Too low standard deviation might result in
        /// too much sensitivity for sudden, but normal, deviations in heartbeat
        /// inter arrival times.
        [<CustomOperation("min_std_deviation");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.MinStdDeviation (state: string list, value: 'Duration) =
            durationField "min-std-deviation" value::state
        /// Minimum standard deviation to use for the normal distribution in
        /// AccrualFailureDetector. Too low standard deviation might result in
        /// too much sensitivity for sudden, but normal, deviations in heartbeat
        /// inter arrival times.
        member inline this.min_std_deviation (value: 'Duration) =
            this.MinStdDeviation([], value)
            |> this.Run
        
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        /// This margin is important to be able to survive sudden, occasional,
        /// pauses in heartbeat arrivals, due to for example garbage collect or
        /// network drop.
        [<CustomOperation("acceptable_heartbeat_pause");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.AcceptableHeartbeatPause (state: string list, value: 'Duration) =
            durationField "acceptable-heartbeat-pause" value::state
        /// Number of potentially lost/delayed heartbeats that will be
        /// accepted before considering it to be an anomaly.
        /// This margin is important to be able to survive sudden, occasional,
        /// pauses in heartbeat arrivals, due to for example garbage collect or
        /// network drop.
        member inline this.acceptable_heartbeat_pause (value: 'Duration) =
            this.AcceptableHeartbeatPause([], value)
            |> this.Run
        
        /// Number of member nodes that each member will send heartbeat messages to,
        /// i.e. each node will be monitored by this number of other nodes.
        [<CustomOperation("monitored_by_nr_of_members");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.MonitoredByNrOfMembers (state: string list, value: int) =
            positiveField "monitored-by-nr-of-members" value::state
        /// Number of member nodes that each member will send heartbeat messages to,
        /// i.e. each node will be monitored by this number of other nodes.
        member this.monitored_by_nr_of_members value =
            this.MonitoredByNrOfMembers([], value)
            |> this.Run
        
        /// After the heartbeat request has been sent the first failure detection
        /// will start after this period, even though no heartbeat mesage has
        /// been received.
        [<CustomOperation("expected_response_after");EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.ExpectedResponseAfter (state: string list, value: 'Duration) =
            durationField "expected-response-after" value::state
        /// After the heartbeat request has been sent the first failure detection
        /// will start after this period, even though no heartbeat mesage has
        /// been received.
        member inline this.expected_response_after (value: 'Duration) =
            this.ExpectedResponseAfter([], value)
            |> this.Run
            
    /// Settings for the Phi accrual failure detector (http://ddg.jaist.ac.jp/pub/HDY+04.pdf
    /// [Hayashibara et al]) used by the cluster subsystem to detect unreachable
    /// members.
    let failure_detector =
        { new FailureDetectorBuilder<Akka.Cluster.Field>() with
            member _.Run (state: string list) = 
                objExpr "failure-detector" 3 state
                |> Akka.Cluster.Field }
