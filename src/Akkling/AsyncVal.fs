//-----------------------------------------------------------------------
// <copyright file="AsyncVal.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

namespace Akkling

/// A common lightweight abstraction over either an asynhronous computation 
/// with value deferred in time, or a synchronous computation with a value 
/// available right away.
[<Struct>]
type AsyncVal<'T> =
    | Synchronous of value:'T
    | Asynchronous of cont:Async<'T>
    static member Zero = Synchronous Unchecked.defaultof<'T>

[<RequireQualifiedAccess>]
module AsyncVal =

    let inline ofAsync a = Asynchronous a
    let inline ofValue v = Synchronous v
    
    let inline toAsync av =
        match av with
        | Synchronous v  -> async.Return v
        | Asynchronous a -> a

    let inline get av =
        match av with
        | Synchronous v  -> v
        | Asynchronous a -> Async.RunSynchronously(a)

    let inline empty<'T> = AsyncVal<'T>.Zero

    let bind (binder: 'T -> AsyncVal<'U>) (av: AsyncVal<'T>) =
        match av with
        | Synchronous v -> binder v
        | Asynchronous a -> async {
            let! v = a
            let bound = binder v
            match bound with
            | Synchronous v'  -> return v'
            | Asynchronous a' -> return! a' } |> Asynchronous

 
 type AsyncValBuilder() =
    member __.Zero () = AsyncVal.empty
    member __.Return v = Synchronous v
    member __.ReturnFrom (v: AsyncVal<_>) = v
    member __.ReturnFrom (a: Async<_>) = Asynchronous a
    member __.Bind (v: AsyncVal<'T>, binder: 'T -> AsyncVal<'U>) = 
        AsyncVal.bind binder v
    member x.Bind (a: Async<'T>, binder: 'T -> AsyncVal<'U>) = 
        async {
            let! value = a
            let bound = binder value
            match bound with
            | Synchronous v'  -> return v'
            | Asynchronous a' -> return! a' } |> Asynchronous

[<AutoOpen>]
module AsyncValEx =

    let asyncVal = AsyncValBuilder()
    
    type Microsoft.FSharp.Control.AsyncBuilder with

        member x.ReturnFrom (v: AsyncVal<'T>) =
            match v with
            | Synchronous v  -> async.Return v
            | Asynchronous a -> async.ReturnFrom a

        member x.Bind (v: AsyncVal<'T>, binder) =
            match v with
            | Synchronous v  -> async.Bind(async.Return v, binder)
            | Asynchronous a -> async.Bind(a, binder)
