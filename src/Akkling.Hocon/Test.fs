namespace Akkling.Hocon

[<AutoOpen>]
module Test =
    open MarkerClasses
    open InternalHocon
    open System.ComponentModel

    let test_actor =
        { new ActorBuilder<Akka.Test.Field>() with
            member _.Run(state: string list) =
                objExpr "test-actor" 3 state |> Akka.Test.Field }

    [<AbstractClass>]
    type TestBuilder<'T when 'T :> IField>() =
        inherit BaseBuilder<'T>()

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Test.Field) = [ x.ToString() ]

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Yield(x: Akka.Actor.Dispatcher.Field) = [ x.ToString() ]

        /// Factor by which to scale timeouts during tests, e.g. to account for shared
        /// build system load
        [<CustomOperation("timefactor"); EditorBrowsable(EditorBrowsableState.Never)>]
        member _.Timefactor(state: string list, value: float) =
            positiveFieldf "timefactor" value :: state

        /// Factor by which to scale timeouts during tests, e.g. to account for shared
        /// build system load
        member this.timefactor value = this.Timefactor([], value) |> this.Run

        /// Duration of EventFilter.intercept waits after the block is finished until
        /// all required messages are received
        [<CustomOperation("filter_leeway"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.FilterLeeway(state: string list, value: 'Duration) =
            durationField "filter-leeway" value :: state

        /// Duration of EventFilter.intercept waits after the block is finished until
        /// all required messages are received
        member inline this.filter_leeway(value: 'Duration) =
            this.FilterLeeway([], value) |> this.Run

        /// The timeout that is added as an implicit by DefaultTimeout trait
        /// This is used for Ask-pattern
        [<CustomOperation("single_expect_default"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.SingleExpectDefault(state: string list, value: 'Duration) =
            durationField "single-expect-default" value :: state

        /// The timeout that is added as an implicit by DefaultTimeout trait
        /// This is used for Ask-pattern
        member inline this.single_expect_default(value: 'Duration) =
            this.SingleExpectDefault([], value) |> this.Run

        /// determines to fail itself, assuming there was message loss or a complete partition of the completion signal.
        [<CustomOperation("default_timeout"); EditorBrowsable(EditorBrowsableState.Never)>]
        member inline _.DefaultTimeout(state: string list, value: 'Duration) =
            durationField "default-timeout" value :: state

        /// determines to fail itself, assuming there was message loss or a complete partition of the completion signal.
        member inline this.default_timeout(value: 'Duration) =
            this.DefaultTimeout([], value) |> this.Run

        // Pathing

        member _.test_actor =
            { new ActorBuilder<Akka.Field>() with
                member _.Run(state: string list) =
                    objExpr "test.test-actor" 2 state |> Akka.Field }

    let test =
        { new TestBuilder<Akka.Field>() with
            member _.Run(state: string list) = objExpr "test" 2 state |> Akka.Field }
