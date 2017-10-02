//-----------------------------------------------------------------------
// <copyright file="Extensions.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.Extensions

open Akka.Actor

let private asLifecycleEvent (message : obj) =
    match message with
    | :? LifecycleEvent as e -> Some e
    | _ -> None

let (|LifecycleEvent|_|) (message: obj) : LifecycleEvent option =
    message |> asLifecycleEvent

let (|PreStart|_|) (message : obj) : unit option =
    message |> asLifecycleEvent 
    |> Option.bind (function PreStart -> Some () | _ -> None )

let (|PostStop|_|) (message : obj) : unit option =
    message |> asLifecycleEvent 
    |> Option.bind (function PostStop -> Some () | _ -> None )

let (|PreRestart|_|) (message : obj) : (exn * obj) option=
    message |> asLifecycleEvent 
    |> Option.bind (function PreRestart (c, m) -> Some (c, m) | _ -> None )

let (|PostRestart|_|) (message : obj) : exn option=
    message |> asLifecycleEvent 
    |> Option.bind (function PostRestart c -> Some c | _ -> None )

[<Struct>]
type CombinedEffect<'Message> (x: Effect<'Message>, y: Effect<'Message>) =
    interface Effect<'Message> with
        member this.WasHandled() = x.WasHandled() && y.WasHandled()
        member this.OnApplied(context : ExtActor<'Message>, message : 'Message) = 
            x.OnApplied(context, message)
            y.OnApplied(context, message)

let inline (<@>) (x: Effect<'Message>) (y: Effect<'Message>) : Effect<'Message> = CombinedEffect(x, y) :> Effect<'Message>
