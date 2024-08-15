namespace Akkling.Hocon

open System.ComponentModel

[<AutoOpen>]
module Units =
    [<AutoOpen>]
    module Duration =
        /// Nanoseconds
        [<Measure>]
        type ns

        /// Microseconds
        [<Measure>]
        type us

        /// Miliseconds
        [<Measure>]
        type ms

        /// Seconds
        [<Measure>]
        type s

        /// Minutes
        [<Measure>]
        type m

        /// Hours
        [<Measure>]
        type h

        /// Days
        [<Measure>]
        type d

    [<AutoOpen>]
    module Bytes =
        /// Bytes
        [<Measure>]
        type B

        /// Kilobytes
        [<Measure>]
        type kB

        /// Kibibytes
        [<Measure>]
        type KiB

        /// Megabytes
        [<Measure>]
        type MB

        /// Mebibytes
        [<Measure>]
        type MiB

        /// Gigabytes
        [<Measure>]
        type GB

        /// Gibibytes
        [<Measure>]
        type GiB

        /// Terabytes
        [<Measure>]
        type TB

        /// Tebibytes
        [<Measure>]
        type TiB

        /// Petabytes
        [<Measure>]
        type PB

        /// Pebibytes
        [<Measure>]
        type PiB

        /// Exabytes
        [<Measure>]
        type EB

        /// Exbibytes
        [<Measure>]
        type EiB

        /// Zettabytes
        [<Measure>]
        type ZB

        /// Zebibytes
        [<Measure>]
        type ZiB

        /// Yottabytes
        [<Measure>]
        type YB

        /// Yobibytes
        [<Measure>]
        type YiB

[<AutoOpen>]
module Types =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    module MarkerClasses =
        type IField = interface end

        module Akka =
            type Field internal (s: string) =
                interface IField

                override _.ToString() = s

            module Actor =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module CurrentContextExecutor =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module DefaultExecutor =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module DedicatedThreadPool =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Deployment =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module DeploymentDefault =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Dispatcher =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module ForkJoinExecutor =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Mailbox =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Router =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module RouterTypeMapping =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module ThreadPoolExecutor =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Typed =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module Cluster =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module Client =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Metrics =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module SplitBrainResolver =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Strategy =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Supervisor =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module CoordinatedShutdown =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module Phases =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module IO =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module DisabledBufferPool =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module DirectBufferPool =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Dns =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module INetAddress =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module Persistence =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module CircuitBreaker =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Plugin =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module SnapshotStore =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module ReplayFilter =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module Remote =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module Batching =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Certificate =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module DotNetty =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module WorkerPool =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module Scheduler =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

            module Shared =
                type Field<'T> = int -> 'T

                module Cluster =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Debug =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Tcp =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

                module Udp =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module Streams =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module Materializer =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

            module Test =
                type Field internal (s: string) =
                    interface IField

                    override _.ToString() = s

                module TestActor =
                    type Field internal (s: string) =
                        interface IField

                        override _.ToString() = s

module Hocon =
    [<NoComparison; NoEquality>]
    type Infinite internal () =
        override _.ToString() = "infinite"

    let infinite = Infinite()

    [<NoComparison; NoEquality>]
    type Unlimited internal () =
        override _.ToString() = "unlimited"

    let unlimited = Unlimited()

    [<NoComparison; NoEquality>]
    type OffForWindows internal () =
        override _.ToString() = "off-for-windows"

    let offForWindows = OffForWindows()

    type Bytes =
        static member bytes(i: int) = i * 1<B>

        static member kilobytes(i: int) = i * 1<kB>
        static member kibibytes(i: int) = i * 1<KiB>

        static member megabytes(i: int) = i * 1<MB>
        static member mebibytes(i: int) = i * 1<MiB>

        static member gigabytes(i: int) = i * 1<GB>
        static member gibibytes(i: int) = i * 1<GiB>

        static member terabytes(i: int) = i * 1<TB>
        static member tebibytes(i: int) = i * 1<TiB>

        static member petabytes(i: int) = i * 1<PB>
        static member pebibytes(i: int) = i * 1<PiB>

        static member exabytes(i: int) = i * 1<EB>
        static member exbibytes(i: int) = i * 1<EiB>

        static member zettabytes(i: int) = i * 1<ZB>
        static member zebibytes(i: int) = i * 1<ZiB>

        static member yottabytes(i: int) = i * 1<YB>
        static member yobibytes(i: int) = i * 1<YiB>

    [<RequireQualifiedAccess>]
    type ByteOrder =
        | BigEndian
        | LittleEndian

        member internal this.Text =
            match this with
            | BigEndian -> "big-endian"
            | LittleEndian -> "little-endian"

    [<RequireQualifiedAccess>]
    type Dispatcher =
        | Dispatcher
        | ForkJoinDispatcher
        | PinnedDispatcher
        | SynchronizedDispatcher
        | TaskDispatcher
        | ThreadPoolDispatcher

        member internal this.Text =
            match this with
            | Dispatcher -> "Dispatcher"
            | ForkJoinDispatcher -> "ForkJoinDispatcher"
            | PinnedDispatcher -> "PinnedDispatcher"
            | SynchronizedDispatcher -> "SynchronizedDispatcher"
            | TaskDispatcher -> "TaskDispatcher"
            | ThreadPoolDispatcher -> "ThreadPoolDispatcher"

    type Duration =
        static member nanoseconds(i: float) = i * 1.<ns>
        static member nanoseconds(i: int) = i * 1<ns>

        static member microseconds(i: float) = i * 1.<us>
        static member microseconds(i: int) = i * 1<us>

        static member milliseconds(i: float) = i * 1.<ms>
        static member milliseconds(i: int) = i * 1<ms>

        static member seconds(i: float) = i * 1.<s>
        static member seconds(i: int) = i * 1<s>

        static member minutes(i: float) = i * 1.<m>
        static member minutes(i: int) = i * 1<m>

        static member hours(i: float) = i * 1.<h>
        static member hours(i: int) = i * 1<h>

        static member days(i: float) = i * 1.<d>
        static member days(i: int) = i * 1<d>

    type Executor =
        | CurrentContext
        | Default
        | ForkJoin
        | Task
        | ThreadPool

        member internal this.Text =
            match this with
            | CurrentContext -> "current-context-executor"
            | Default -> "default-executor"
            | ForkJoin -> "fork-join-executor"
            | Task -> "task-executor"
            | ThreadPool -> "thread-pool-executor"

    type LogLevel =
        | DEBUG
        | ERROR
        | INFO
        | OFF
        | WARNING

        member internal this.Text =
            match this with
            | DEBUG -> "DEBUG"
            | ERROR -> "ERROR"
            | INFO -> "INFO"
            | OFF -> "OFF"
            | WARNING -> "WARNING"

    type MetricSelection =
        | CPU
        | Heap
        | Load
        | Mix

        member internal this.Text =
            match this with
            | CPU -> "cpu"
            | Heap -> "heap"
            | Load -> "load"
            | Mix -> "mix"

    module Remoting =
        type CertificateFlags =
            | Default
            | Exportable
            | Machine
            | Persist
            | User
            | UserProtected

            member internal this.Text =
                match this with
                | Default -> "default-key-set"
                | Exportable -> "exportable"
                | Machine -> "machine-key-set"
                | Persist -> "persist-key-set"
                | User -> "user-key-set"
                | UserProtected -> "user-protected"

        type StoreLocation =
            | CurrentUser
            | LocalMachine

            member internal this.Text =
                match this with
                | CurrentUser -> "current-user"
                | LocalMachine -> "local-machine"

    [<RequireQualifiedAccess>]
    module ReplayFilter =
        type Mode =
            /// Fail the replay, error is logged
            | Fail
            /// Disable this feature completely
            | Off
            /// Discard events from old writers, warning is logged
            | RepairByDiscardOld
            /// Log warning but emit events untouched
            | Warn

            member internal this.Text =
                match this with
                | Fail -> "fail"
                | Off -> "off"
                | RepairByDiscardOld -> "repair-by-discard-old"
                | Warn -> "warn"

    module Router =
        type LoadBalance =
            | Broadcast
            | FromCode
            | Random
            | RoundRobin
            | ScatterGather
            | SmallestMailbox

            member internal this.Text =
                match this with
                | Broadcast -> "broadcast"
                | FromCode -> "from-code"
                | Random -> "random"
                | RoundRobin -> "round-robin"
                | ScatterGather -> "scatter-gather"
                | SmallestMailbox -> "smallest-mailbox"

        type Logic =
            | Broadcast
            | Random
            | RoundRobin

            member internal this.Text =
                match this with
                | Broadcast -> "broadcast"
                | Random -> "random"
                | RoundRobin -> "round-robin"

    module SplitBrainResolver =
        type Strategy =
            | KeepMajority
            | KeepOldest
            | KeepReferee
            | StaticQuorum

            member internal this.Text =
                match this with
                | KeepMajority -> "keep-majority"
                | KeepOldest -> "keep-oldest"
                | KeepReferee -> "keep-referee"
                | StaticQuorum -> "static-quorum"

    [<RequireQualifiedAccess>]
    module SubscriptionTimeout =
        type Mode =
            | Cancel
            | Noop
            | Warn

            member internal this.Text =
                match this with
                | Cancel -> "cancel"
                | Noop -> "noop"
                | Warn -> "warn"

    type TaskPeekingMode =
        | FIFO
        | LIFO

        member internal this.Text =
            match this with
            | FIFO -> "FIFO"
            | LIFO -> "LIFO"

    type ThreadType =
        | Background
        | Foreground

        member internal this.Text =
            match this with
            | Background -> "background"
            | Foreground -> "foreground"
