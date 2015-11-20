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

let (|LifecycleEvent|_|) (message: obj) : LifecycleEvent option =
    match message with
    | :? LifecycleEvent as e -> Some e
    | _ -> None

[<Struct>]
type CombinedEffect (x: Effect, y: Effect) =
    interface Effect with
        member this.OnApplied(context : ExtActor<'Message>, message : 'Message) = 
            x.OnApplied(context, message)
            y.OnApplied(context, message)

let inline (@) (x: Effect) (y: Effect) : Effect = CombinedEffect(x, y) :> Effect

let implicit (/) (x:ActorPath) (y:string) = ActorPath.op_Division(x, y)
