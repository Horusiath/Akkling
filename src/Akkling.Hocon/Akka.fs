namespace Akkling.Hocon

[<AutoOpen>]
module Akka =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    [<AbstractClass>]
    type SslConfigBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        /// Home directory of Akka, modules in the deploy directory will be loaded.
        [<CustomOperation("protocol"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Protocol(state: string list, x: string) = quotedField "protocol" x :: state

        /// Home directory of Akka, modules in the deploy directory will be loaded.
        member this.protocol value = this.Protocol([], value) |> this.Run

    let ssl_config =
        { new SslConfigBuilder<Akka.Field>() with
            member _.Run(state: string list) =
                objExpr "ssl-config" 2 state |> Akka.Field }

    type AkkaBuilder() =
        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: string) = [ x ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(a: unit) = []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Shared.Cluster.Field>) = [ (x 2).ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Shared.Field<Akka.Field>) = [ (x 2).ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Scheduler.Field) = [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Field) = [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Combine(state: string list, x: string list) = x @ state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Zero() = []

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Delay f = f ()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.For(xs: string list, f: unit -> string list) = f () @ xs

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Run(state: string list) = objExpr "akka" 1 (state |> List.rev)

        /// Toggles whether threads created by this ActorSystem should be daemons or not.
        [<CustomOperation("daemonic"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Daemonic(state: string list, value: bool) = switchField "daemonic" value :: state

        /// List FQCN of extensions which shall be loaded at actor system startup.
        /// Should be on the format: 'extensions = ["foo", "bar"]' etc.
        /// See the Akka Documentation for more info about Extensions
        [<CustomOperation("extensions"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Extensions(state: string list, x: string list) = quotedListField "extensions" x :: state

        /// Home directory of Akka, modules in the deploy directory will be loaded.
        [<CustomOperation("home"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Home(state: string list, x: string) = field "home" x :: state

        /// Log the complete configuration at INFO level when the actor system is started.
        /// This is useful when you are uncertain of what configuration is used.
        [<CustomOperation("log_config_on_start"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogConfigOnStart(state: string list, value: bool) =
            switchField "log-config-on-start" value :: state

        /// Log at info level when messages are sent to dead letters.
        ///
        /// Possible values:
        ///
        /// on: all dead letters are logged
        ///
        /// off: no logging of dead letters
        ///
        /// n: positive integer, number of dead letters that will be logged
        [<CustomOperation("log_dead_letters"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogDeadLetters(state: string list, value: bool) =
            switchField "log-dead-letters" value :: state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.LogDeadLetters(state: string list, value) =
            positiveField "log-dead-letters" value :: state

        /// Possibility to turn off logging of dead letters while the actor system
        /// is shutting down. Logging is only done when enabled by 'log-dead-letters'
        /// setting.
        [<CustomOperation("log_dead_letters_during_shutdown"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogDeadLettersDuringShutdown(state: string list, value: bool) =
            switchField "log-dead-letters-during-shutdown" value :: state

        /// Possibility to turn off logging of dead letters while the actor system
        /// is shutting down. Logging is only done when enabled by 'log-dead-letters'
        /// setting.
        [<CustomOperation("log_dead_letters_suspend_duration"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.LogDeadLettersSuspendDuration(state: string list, value: 'Duration) =
            durationField "log-dead-letters-suspend-duration" value :: state

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LogDeadLettersSuspendDuration(state: string list, _: Hocon.Infinite) =
            "log-dead-letters-suspend-duration = infinite" :: state

        /// You can enable asynchronous loggers creation by setting this to `true`.
        /// This may be useful in cases when ActorSystem creation takes more time
        /// then it should, or for whatever other reason
        [<CustomOperation("logger_async_start"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LoggerAsyncStart(state: string list, value: bool) =
            boolField "logger-async-start" value :: state

        /// Loggers are created and registered synchronously during ActorSystem
        /// start-up, and since they are actors, this timeout is used to bound the
        /// waiting time
        [<CustomOperation("logger_startup_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.LoggerStartupTimeout(state: string list, value: 'Duration) =
            durationField "logger-startup-timeout" value :: state

        /// Loggers to register at boot time (akka.event.Logging$DefaultLogger logs to STDOUT)
        [<CustomOperation("loggers"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Loggers(state: string list, x: string list) = quotedListField "loggers" x :: state

        /// Specifies the default loggers dispatcher
        [<CustomOperation("loggers_dispatcher"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.LoggersDispatcher(state: string list, x: string) = field "loggers-dispatcher" x :: state

        /// Log level used by the configured loggers (see "loggers") as soon
        /// as they have been started; before that, see "stdout-loglevel"
        ///
        /// Options: OFF, ERROR, WARNING, INFO, DEBUG
        [<CustomOperation("loglevel"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Loglevel(state: string list, loglevel: Hocon.LogLevel) = field "loglevel" loglevel.Text :: state

        /// Log level for the very basic logger activated during AkkaApplication startup
        ///
        /// Options: OFF, ERROR, WARNING, INFO, DEBUG
        [<CustomOperation("stdout_loglevel"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.StdoutLoglevel(state: string list, loglevel: Hocon.LogLevel) =
            field "stdout-loglevel" loglevel.Text :: state

        /// Akka version, checked against the runtime version of Akka.
        [<CustomOperation("version"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Version(state: string list, major: int, minor: int, patch: int) =
            sprintf "version = %i.%i.%i Akka" major minor patch :: state

    let akka = AkkaBuilder()
