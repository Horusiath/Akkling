namespace Akkling.Hocon

open System

[<AutoOpen>]
module TransportFailureDetector =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type TransportFailureDetectorBuilder<'T when 'T :> IField> () =
        inherit BaseBuilder<'T>()
        
        /// FQCN of the failure detector implementation.
        /// It must implement akka.remote.FailureDetector and have
        /// a public constructor with a com.typesafe.config.Config and
        /// akka.actor.EventStream parameter.
        [<CustomOperation("implementation_class");EditorBrowsable(EditorBrowsableState.Never)>]
        member _.ImplementationClass (state: string list, x: Type) = 
            quotedField "implementation-class" (fqcn x)::state
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
    
    /// For TCP it is not important to have fast failure detection, since
    /// most connection failures are captured by TCP itself.
    /// The default DeadlineFailureDetector will trigger if there are no heartbeats within
    /// the duration heartbeat-interval + acceptable-heartbeat-pause, i.e. 20 seconds
    /// the duration heartbeat-interval + acceptable-heartbeat-pause, i.e. 124 seconds
    let transport_failure_detector =
        { new TransportFailureDetectorBuilder<Akka.Remote.Field>() with
            member _.Run (state: string list) = 
                objExpr "transport-failure-detector" 3 state
                |> Akka.Remote.Field }
