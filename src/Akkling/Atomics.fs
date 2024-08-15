//-----------------------------------------------------------------------
// <copyright file="Atomics.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

open Akka.Util
open Akka.Util.Internal

type AtomicRef<'a when 'a: not struct> = AtomicReference<'a>
type AtomicInt = AtomicCounter
type AtomicInt64 = AtomicCounterLong
type AtomicBool = AtomicBoolean

module AtomicOperators =

    /// Returns values from atomic ref
    let inline (!) (atom: AtomicRef<_>) = atom.Value

    /// Atomically inserts `value` into an `atom` and returns previously hold value back.
    let inline (:=) (atom: AtomicRef<'a>) (value: 'a) = atom.GetAndSet(value)

    /// Encapsulates `value` into a new atomic reference cell.
    let inline atom (value: 'a) : AtomicRef<'a> = AtomicRef<'a>(value)

[<RequireQualifiedAccess>]
module Atomic =

    open AtomicOperators

    let inline ref (value: 'a) : AtomicRef<'a> = AtomicRef(value)
    let inline int (value: int) : AtomicInt = AtomicInt(value)
    let inline int64 (value: int64) : AtomicInt64 = AtomicInt64(value)
    let inline bool (value: bool) : AtomicBool = AtomicBool(value)

    let inline inc (atom: #IAtomicCounter<'a>) : 'a = atom.GetAndIncrement()

    /// Compare-and-set operation: inserts a `value` inside of an atom
    /// if current atom's value is referentially equal to `expected`.
    /// Returns true if value has been swapped.
    let inline cas (expected: 'a) (value: 'a) (atom: AtomicRef<'a>) = atom.CompareAndSet(expected, value)

    /// Performs an update operation, modifying a content of an `atom`
    /// in a lock-free, thread-safe manner.
    let update (fn: 'a -> 'a) (atom: AtomicRef<'a>) =
        let rec loop fn atom =
            let current = !atom
            let newVal = fn current
            if cas current newVal atom then () else loop fn atom

        loop fn atom
