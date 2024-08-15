//-----------------------------------------------------------------------
// <copyright file="Configuration.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------
module Akkling.Tests.Configuration

open Akkling
open Akkling.Hocon
open System
open Xunit
open Akka.Configuration

type MyEnum =
    | A = 1
    | B = 2

let ceConfig =
    akka {
        actor {
            debug { autoreceive true }

            default_dispatcher { executor "hello" }
        }

        "multi-part-property = 999999999999"
        "an-integer = 1"
        "some-strange-float = 2.5"
        logger_async_start true
        loggers [ "hello"; "hocon" ]
        logger_startup_timeout 30<s>
        "option-type = b"
    }
    |> Configuration.parse

let config =
    Configuration.parse
        """
outer {
    inner {
        inner2 {
            my-prop = "hello"
        }
        inner3 {
            my-other-prop = "world"
        }
    }
    multi-part-property = 999999999999
    an-integer = 1
    some-strange-float = 2.5
    a-boolean = on
    a-time = 30s
    list-of-strings = [ "hello", "hocon" ]
    option-type = b
}
"""

[<Fact>]
let ``Dynamic config operation must return Config`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: Config) = c1?Inner?Inner2
    let (value2: Config) = c2?actor?debug

    equals typeof<Config> <| value.GetType()
    equals typeof<Config> <| value2.GetType()

    equals true <| value.HasPath "my-prop"
    equals true <| value2.HasPath "autoreceive"

[<Fact>]
let ``Dynamic config operation must return string`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: string) = c1?Inner?Inner2?MyProp
    let (value2: string) = c2?actor?DefaultDispatcher?executor

    equals "hello" value
    equals "hello" value2

[<Fact>]
let ``Dynamic config operation must return int32`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: int) = c1?AnInteger
    let (value2: int) = c2?AnInteger

    equals 1 value
    equals 1 value2

[<Fact>]
let ``Dynamic config operation must return int64`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: int64) = c1?MultiPartProperty
    let (value2: int64) = c2?MultiPartProperty

    equals 999999999999L value
    equals 999999999999L value2

[<Fact>]
let ``Dynamic config operation must return bool`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: bool) = c1?ABoolean
    let (value2: bool) = c2?LoggerAsyncStart

    equals true value
    equals true value2

[<Fact>]
let ``Dynamic config operation must return TimeSpan`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: TimeSpan) = c1?ATime
    let (value2: TimeSpan) = c2?LoggerStartupTimeout

    equals (TimeSpan.FromSeconds 30.) value
    equals (TimeSpan.FromSeconds 30.) value2

[<Fact>]
let ``Dynamic config operation must return float`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: float32) = c1?SomeStrangeFloat
    let (value2: float32) = c2?SomeStrangeFloat

    equals 2.5f value
    equals 2.5f value2

[<Fact>]
let ``Dynamic config operation must return double`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: float) = c1?SomeStrangeFloat
    let (value2: float32) = c2?SomeStrangeFloat

    equals 2.5 value
    equals 2.5f value2

[<Fact>]
let ``Dynamic config operation must return decimal`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: decimal) = c1?SomeStrangeFloat
    let (value2: decimal) = c2?SomeStrangeFloat

    equals 2.5M value
    equals 2.5M value2

[<Fact>]
let ``Dynamic config operation must return string list`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: string seq) = c1?ListOfStrings
    let (value2: string seq) = c2?loggers

    equals [ "hello"; "hocon" ] <| List.ofSeq value
    equals [ "hello"; "hocon" ] <| List.ofSeq value2

[<Fact>]
let ``Dynamic config operation must return an enum`` () =
    let c1 = config?outer
    let c2 = ceConfig?akka

    let (value: MyEnum) = c1?OptionType
    let (value2: MyEnum) = c2?OptionType

    equals MyEnum.B value
    equals MyEnum.B value2

let akkaDefaultConfig () =
    akka {
        version 0 0 1
        home ""
        loggers [ "Akka.Event.DefaultLogger" ]
        loggers_dispatcher "akka.actor.default-dispatcher"
        logger_startup_timeout 5<s>
        logger_async_start false
        loglevel Hocon.LogLevel.INFO
        "suppress-json-serializer-warning = on"
        stdout_loglevel Hocon.LogLevel.WARNING
        log_config_on_start false
        log_dead_letters 10
        log_dead_letters_during_shutdown false
        log_dead_letters_suspend_duration 5<m>
        extensions []
        daemonic false

        actor {
            provider typeof<Akka.Actor.LocalActorRefProvider>
            guardian_supervisor_strategy typeof<Akka.Actor.DefaultSupervisorStrategy>
            creation_timeout 20<s>
            reaper_interval 5
            serialize_messages false
            serialize_creators false
            unstarted_push_timeout 10<s>
            ask_timeout Hocon.infinite

            typed'.timeout 5<s>

            inbox {
                inbox_size 1000
                default_timeout 5<s>
            }

            router.type_mapping {
                from_code "Akka.Routing.NoRouter"
                round_robin_pool "Akka.Routing.RoundRobinPool"
                round_robin_group "Akka.Routing.RoundRobinGroup"
                random_pool "Akka.Routing.RandomPool"
                random_group "Akka.Routing.RandomGroup"
                smallest_mailbox_pool "Akka.Routing.SmallestMailboxPool"
                broadcast_pool "Akka.Routing.BroadcastPool"
                broadcast_group "Akka.Routing.BroadcastGroup"
                scatter_gather_pool "Akka.Routing.ScatterGatherFirstCompletedPool"
                scatter_gather_group "Akka.Routing.ScatterGatherFirstCompletedGroup"
                consistent_hashing_pool "Akka.Routing.ConsistentHashingPool"
                consistent_hashing_group "Akka.Routing.ConsistentHashingGroup"
                tail_chopping_pool "Akka.Routing.TailChoppingPool"
                tail_chopping_group "Akka.Routing.TailChoppingGroup"
            }

            deployment {
                default' {
                    dispatcher ""
                    mailbox ""
                    router Hocon.Router.LoadBalance.FromCode
                    nr_of_instances 1
                    within 5<s>
                    virtual_nodes_factor 10

                    routees.paths []

                    resizer {
                        enabled false
                        lower_bound 1
                        upper_bound 10
                        pressure_threshold 1
                        rampup_rate 0.2
                        backoff_threshold 0.3
                        backoff_rate 0.1
                        messages_per_resize 10
                    }
                }
            }

            synchronized_dispatcher {
                type' "SynchronizedDispatcher"
                executor "current-context-executor"
                throughput 10
            }

            task_dispatcher {
                type' "TaskDispatcher"
                executor "task-executor"
                throughput 30
            }

            default_fork_join_dispatcher {
                type' "ForkJoinDispatcher"
                executor "fork-join-executor"
                throughput 30

                dedicated_thread_pool {
                    thread_count 3
                    threadtype Hocon.ThreadType.Background
                }
            }

            default_dispatcher {
                type' "Dispatcher"
                executor "default-executor"

                default_executor { "" }
                thread_pool_executor { "" }

                fork_join_executor {
                    parallelism_min 8
                    parallelism_factor 1.0
                    parallelism_max 64
                    task_peeking_mode Hocon.TaskPeekingMode.FIFO
                }

                current_context_executor { "" }

                shutdown_timeout 1<s>
                throughput 30
                throughput_deadline_time 0<ms>
                attempt_teamwork true
                mailbox_requirement ""
            }

            internal_dispatcher {
                type' "Dispatcher"
                executor "fork-join-executor"
                throughput 5

                fork_join_executor {
                    parallelism_min 4
                    parallelism_factor 1.0
                    parallelism_max 64
                }
            }

            default_blocking_io_dispatcher {
                type' "Dispatcher"
                executor "thread-pool-executor"
                throughput 1
            }

            default_mailbox {
                mailbox_type typeof<Akka.Dispatch.UnboundedMailbox>
                mailbox_capacity 1000
                mailbox_push_timeout_time 10<s>
                stash_capacity -1
            }

            mailbox {
                requirements
                    [ "Akka.Dispatch.IUnboundedMessageQueueSemantics", "akka.actor.mailbox.unbounded-queue-based"
                      "Akka.Dispatch.IBoundedMessageQueueSemantics", "akka.actor.mailbox.bounded-queue-based"
                      "Akka.Dispatch.IDequeBasedMessageQueueSemantics", "akka.actor.mailbox.unbounded-deque-based"
                      "Akka.Dispatch.IUnboundedDequeBasedMessageQueueSemantics",
                      "akka.actor.mailbox.unbounded-deque-based"
                      "Akka.Dispatch.IBoundedDequeBasedMessageQueueSemantics", "akka.actor.mailbox.bounded-deque-based"
                      "Akka.Dispatch.IMultipleConsumerSemantics", "akka.actor.mailbox.unbounded-queue-based"
                      "Akka.Event.ILoggerMessageQueueSemantics", "akka.actor.mailbox.logger-queue" ]

                unbounded_queue_based.mailbox_type typeof<Akka.Dispatch.UnboundedMailbox>
                bounded_queue_based.mailbox_type typeof<Akka.Dispatch.BoundedMailbox>
                unbounded_deque_based.mailbox_type typeof<Akka.Dispatch.UnboundedDequeBasedMailbox>
                bounded_deque_based.mailbox_type typeof<Akka.Dispatch.BoundedDequeBasedMailbox>
                logger_queue.mailbox_type (Type.GetType "Akka.Event.LoggerMailboxType, Akka")
            }

            debug {
                receive false
                autoreceive false
                lifecycle false
                fsm false
                event_stream false
                router_misconfiguration false
            }

            serializers {
                json "Akka.Serialization.NewtonSoftJsonSerializer, Akka"
                bytes "Akka.Serialization.ByteArraySerializer, Akka"
            }

            serialization_bindings [ typeof<byte[]>, "bytes"; typeof<obj>, "json" ]

            serialization_identifiers
                [ typeof<Akka.Serialization.ByteArraySerializer>, 4
                  typeof<Akka.Serialization.NewtonSoftJsonSerializer>, 1 ]

            serialization_settings { "" }
        }

        scheduler {
            tick_duration 10<ms>
            ticks_per_wheel 512
            implementation typeof<Akka.Actor.HashedWheelTimerScheduler>
            shutdown_timeout 5<s>
        }

        io {

            pinned_dispatcher {
                type' "PinnedDispatcher"
                executor "fork-join-executor"
            }

            let ioDirectBufferPool =
                direct_buffer_pool {
                    class' "Akka.IO.Buffers.DirectBufferPool, Akka"
                    buffer_size 512
                    buffers_per_segment 500
                    initial_segments 1
                    buffer_pool_limit 1024
                }

            tcp {
                ioDirectBufferPool

                disabled_buffer_pool {
                    class' "Akka.IO.Buffers.DisabledBufferPool, Akka"
                    buffer_size 512
                }

                buffer_pool "akka.io.tcp.disabled-buffer-pool"
                max_channels 256000
                selector_association_retries 10
                batch_accept_limit 10
                register_timeout 5<s>
                max_received_message_size Hocon.unlimited
                trace_logging false

                selector_dispatcher "akka.io.pinned-dispatcher"
                worker_dispatcher "akka.actor.internal-dispatcher"
                management_dispatcher "akka.actor.internal-dispatcher"
                file_io_dispatcher "akka.actor.default-blocking-io-dispatcher"

                file_io_transferTo_limit 524288
                finish_connect_retries 5
                windows_connection_abort_workaround_enabled false
                outgoing_socket_force_ipv4 false
            }

            udp {
                ioDirectBufferPool

                buffer_pool "akka.io.udp.direct-buffer-pool"
                nr_of_socket_async_event_args 32
                max_channels 4096
                select_timeout Hocon.infinite
                selector_association_retries 10
                receive_throughput 3
                direct_buffer_size 18432
                direct_buffer_pool_limit 1000
                received_message_size_limit Hocon.unlimited
                trace_logging false

                selector_dispatcher "akka.io.pinned-dispatcher"
                worker_dispatcher "akka.actor.internal-dispatcher"
                management_dispatcher "akka.actor.internal-dispatcher"
            }

            udp_connected {
                ioDirectBufferPool

                buffer_pool "akka.io.udp-connected.direct-buffer-pool"
                nr_of_socket_async_event_args 32
                max_channels 4096
                select_timeout Hocon.infinite
                selector_association_retries 10
                receive_throughput 3
                direct_buffer_size 18432
                direct_buffer_pool_limit 1000
                received_message_size_limit Hocon.unlimited
                trace_logging false

                selector_dispatcher "akka.io.pinned-dispatcher"
                worker_dispatcher "akka.actor.internal-dispatcher"
                management_dispatcher "akka.actor.internal-dispatcher"
            }

            dns {
                dispatcher "akka.actor.internal-dispatcher"
                resolver "inet-address"

                inet_address {
                    provider_object "Akka.IO.InetAddressDnsProvider"

                    positive_ttl 30<s>
                    negative_ttl 10<s>

                    cache_cleanup_interval 120<s>

                    use_ipv6 true
                }
            }
        }

        coordinated_shutdown {
            default_phase_timeout 5<s>
            terminate_actor_system true
            exit_clr false
            run_by_clr_shutdown_hook true
            run_by_actor_system_terminate true

            phases {
                before_service_unbind { "" }
                service_unbind.depends_on [ before_service_unbind ]
                service_stop.depends_on [ service_requests_done ]
                before_cluster_shutdown.depends_on [ service_stop ]

                cluster_sharding_shutdown_region {
                    timeout 10<s>
                    depends_on [ before_cluster_shutdown ]
                }

                cluster_leave.depends_on [ cluster_sharding_shutdown_region ]

                cluster_exiting {
                    timeout 10<s>
                    depends_on [ cluster_leave ]
                }

                cluster_exiting_done.depends_on [ cluster_exiting ]
                cluster_shutdown.depends_on [ cluster_exiting_done ]
                before_actor_system_terminate.depends_on [ cluster_shutdown ]

                actor_system_terminate {
                    timeout 10<s>
                    depends_on [ before_actor_system_terminate ]
                }
            }
        }
    }
    |> Configuration.parse

[<Fact>]
let ``Hocon CE builds configuration faithfully`` () =
    // Would be nice if there was a good way to compare configs
    // this does subscribe to fail fast and does value checks on build

    //let cleanup (xs: Config) =
    //    xs.AsEnumerable()
    //    |> List.ofSeq
    //    |> List.map (fun kvPair -> kvPair.Key,kvPair.Value.GetHashCode())
    //    |> List.sort

    //let def =
    //    Akka.Configuration.ConfigurationFactory.Default()
    //    |> cleanup

    try
        akkaDefaultConfig () |> ignore
        true
    with _ ->
        false
    |> equals true
