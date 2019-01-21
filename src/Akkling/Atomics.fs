//-----------------------------------------------------------------------
// <copyright file="Atomics.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Util
open Akka.Util.Internal

type AtomicRef<'a when 'a: not struct> = AtomicReference<'a>
type AtomicInt = AtomicCounter
type AtomicInt64 = AtomicCounterLong
type AtomicBool = AtomicBoolean

[<RequireQualifiedAccess>]
module Atomic =

    let inline ref (value: 'a): AtomicRef<'a> = AtomicRef(value)
    let inline int (value: int): AtomicInt = AtomicInt(value)
    let inline int64 (value: int64): AtomicInt64 = AtomicInt64(value)
    let inline bool (value: bool): AtomicBool = AtomicBool(value)

    let inline inc (atom: #IAtomicCounter<'a>): 'a = atom.GetAndIncrement()
        