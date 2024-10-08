﻿//-----------------------------------------------------------------------
// <copyright file="ActorBuilder.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.ComputationExpressions

open System
open System.Threading.Tasks

/// Gives access to the next message throu let! binding in actor computation expression.
//type Behavior<'In, 'Out> =
//    | Become of ('In -> Behavior<'In, 'Out>)
//    | Return of 'Out

/// The builder for actor computation expression.
type ActorBuilder() =
    member _.Bind(_: IO<'In>, continuation: 'In -> Effect<'In>) : Effect<'In> =
        upcast Become(fun message -> continuation message)

    member this.Bind(behavior: Effect<'In>, continuation: Effect<'In> -> Effect<'In>) : Effect<'In> =
        match behavior with
        | :? Become<'In> as become -> upcast Become<'In>(fun message -> this.Bind(become.Next message, continuation))
        | returned -> continuation returned

    member _.Bind(asyncInput: Async<'In>, continuation: 'In -> Effect<'Out>) : Effect<'Out> =
        let currentActorCell = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current

        upcast
            AsyncEffect(
                async {
                    let! returned = asyncInput
                    // Here the thread may have changed => set the ThreadStatic singleton for this new thread
                    Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current <- currentActorCell
                    return continuation returned
                }
            )

    member _.Bind(taskInput: Task<'In>, continuation: 'In -> Effect<'Out>) : Effect<'Out> =
        let currentActorCell = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current

        upcast
            TaskEffect(
                task {
                    let! returned = taskInput
                    // Here the thread may have changed => set the ThreadStatic singleton for this new thread
                    Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current <- currentActorCell
                    return continuation returned
                }
            )

    member _.Bind(taskInput: Task, continuation: unit -> Effect<'Out>) : Effect<'Out> =
        let currentActorCell = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current

        upcast
            TaskEffect(
                task {
                    do! taskInput
                    // Here the thread may have changed => set the ThreadStatic singleton for this new thread
                    Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current <- currentActorCell
                    return continuation ()
                }
            )

    member _.ReturnFrom(effect: Effect<'Message>) = effect
    member _.Return(value: Effect<'Message>) : Effect<'Message> = value
    member _.Zero() : Effect<'Message> = upcast Ignore
    member _.Yield value = value

    member _.TryWith(tryExpr: unit -> Effect<'In>, catchExpr: exn -> Effect<'In>) : Effect<'In> =
        try
            match tryExpr () with
            | Become next ->
                upcast
                    Become(fun message ->
                        try
                            next message
                        with error ->
                            catchExpr error)
            | behavior -> behavior
        with error ->
            catchExpr error

    member _.TryFinally(tryExpr: unit -> Effect<'In>, finallyExpr: unit -> unit) : Effect<'In> =
        try
            match tryExpr () with
            | Become next ->
                upcast
                    Become(fun message ->
                        try
                            next message
                        finally
                            finallyExpr ())
            | behavior ->
                finallyExpr ()
                behavior
        with error ->
            finallyExpr ()
            reraise ()

    member this.Using(disposable: #IDisposable, continuation: _ -> Effect<'In>) : Effect<'In> =
        this.TryFinally(
            (fun () -> continuation disposable),
            fun () ->
                if disposable <> null then
                    disposable.Dispose()
        )

    member this.While(condition: unit -> bool, continuation: unit -> Effect<'In>) : Effect<'In> =
        if condition () then
            match continuation () with
            | Become next ->
                Become(fun message ->
                    next message |> ignore
                    this.While(condition, continuation))
                :> Effect<'In>
            | _ -> this.While(condition, continuation)
        else
            Ignore :> Effect<'In>

    member _.For(iterable: 'Iter seq, continuation: 'Iter -> Effect<'In>) : Effect<'In> =
        use e = iterable.GetEnumerator()

        let rec loop () =
            if e.MoveNext() then
                match continuation e.Current with
                | Become fn ->
                    upcast
                        Become(fun m ->
                            fn m |> ignore
                            loop ())
                | _ -> loop ()
            else
                Ignore :> Effect<'In>

        loop ()

    member _.Delay(continuation: unit -> Effect<'In>) = continuation
    member _.Run(continuation: unit -> Effect<'In>) = continuation ()
    member _.Run(continuation: Effect<'In>) = continuation

    member this.Combine(first: unit -> Effect<'In>, second: unit -> Effect<'In>) : Effect<'In> =
        match first () with
        | Become next -> upcast Become(fun message -> this.Combine((fun () -> next message), second))
        | first -> upcast CombinedEffect(first, second ())

    member this.Combine(first: Effect<'In>, second: unit -> Effect<'In>) : Effect<'In> =
        match first with
        | Become next -> upcast Become(fun message -> this.Combine(next message, second))
        | first -> upcast CombinedEffect(first, second ())

    member this.Combine(first: unit -> Effect<'In>, second: Effect<'In>) : Effect<'In> =
        match first () with
        | Become next -> upcast Become(fun message -> this.Combine((fun () -> next message), second))
        | first -> upcast CombinedEffect(first, second)

    member this.Combine(first: Effect<'In>, second: Effect<'In>) : Effect<'In> =
        match first with
        | Become next -> upcast Become(fun message -> this.Combine(next message, second))
        | first -> upcast CombinedEffect(first, second)

/// Builds an actor message handler using an actor expression syntax.
let actor = ActorBuilder()
