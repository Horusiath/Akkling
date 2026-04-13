//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2026 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2026 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2026 Bartosz Sypytkowski, Vagif Abilov and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Tests

open Xunit

let equals (expected: 'a) (value: 'a) = Assert.Equal<'a>(expected, value)
let success = ()

let (|String|_|) (message: obj) =
    match message with
    | :? string as x -> Some x
    | _ -> None
