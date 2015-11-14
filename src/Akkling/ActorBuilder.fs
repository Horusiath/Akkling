//-----------------------------------------------------------------------
// <copyright file="ActorBuilder.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

[<AutoOpen>]
module Akkling.ComputationExpressions

open System

/// Gives access to the next message throu let! binding in actor computation expression.
//type Behavior<'In, 'Out> = 
//    | Become of ('In -> Behavior<'In, 'Out>)
//    | Return of 'Out

/// The builder for actor computation expression.
type ActorBuilder() =
    member __.Bind(_ : IO<'In>, continuation : 'In -> Behavior<'In>) = Become(fun message -> continuation message)
    member this.Bind(behavior : Behavior<'In>, continuation : Effect -> Behavior<'In>) : Behavior<'In> = 
        match behavior with
        | Become next -> Become(fun message -> this.Bind(next message, continuation))
        | Return returned -> continuation returned    
    member __.ReturnFrom behavior = behavior
    member __.Return value = Return value
    member __.Zero () = Return Ignore    
    member __.Yield value = value

    member this.TryWith(tryExpr : unit -> Behavior<'In>, catchExpr : exn -> Behavior<'In>) : Behavior<'In> = 
        try 
            true, tryExpr ()
        with error -> false, catchExpr error
        |> function 
        | true, Become next -> Become(fun message -> this.TryWith((fun () -> next message), catchExpr))
        | _, value -> value    

    member this.TryFinally(tryExpr : unit -> Behavior<'In>, finallyExpr : unit -> unit) : Behavior<'In> = 
        try 
            match tryExpr() with
            | Become next -> Become(fun message -> this.TryFinally((fun () -> next message), finallyExpr))
            | behavior -> 
                finallyExpr()
                behavior
        with error -> 
            finallyExpr()
            reraise()
    
    member this.Using(disposable : #IDisposable, continuation : _ -> Behavior<'In>) : Behavior<'In> = 
        this.TryFinally((fun () -> continuation disposable), fun () -> if disposable <> null then disposable.Dispose())
    
    member this.While(condition : unit -> bool, continuation : unit -> Behavior<'In>) : Behavior<'In> = 
        if condition() then 
            match continuation() with
            | Become next -> 
                Become (fun message -> 
                    next message |> ignore
                    this.While(condition, continuation))
            | _ -> this.While(condition, continuation)
        else Return Ignore
    
    member __.For(iterable : 'Iter seq, continuation : 'Iter -> Behavior<'In>) : Behavior<'In> = 
        use e = iterable.GetEnumerator()
        
        let rec loop() = 
            if e.MoveNext() then 
                match continuation e.Current with
                | Become fn -> 
                    Become(fun m -> 
                        fn m |> ignore
                        loop())
                | _ -> loop()
            else Return Ignore
        loop()
    
    member __.Delay(continuation : unit -> Behavior<'In>) = continuation
    member __.Run(continuation : unit -> Behavior<'In>) = continuation ()
    member __.Run(continuation : Behavior<'In>) = continuation
    
    member this.Combine(first : unit -> Behavior<'In>, second : unit -> Behavior<'In>) : Behavior<'In> = 
        match first () with
        | Become next -> Become(fun message -> this.Combine((fun () -> next message), second))
        | Return _ -> second ()
    
    member this.Combine(first : Behavior<'In>, second : unit -> Behavior<'In>) : Behavior<'In> = 
        match first with
        | Become next -> Become(fun message -> this.Combine(next message, second))
        | Return _ -> second ()
    
    member this.Combine(first : unit -> Behavior<'In>, second : Behavior<'In>) : Behavior<'In> = 
        match first () with
        | Become next -> Become(fun message -> this.Combine((fun () -> next message), second))
        | Return _ -> second
    
    member this.Combine(first : Behavior<'In>, second : Behavior<'In>) : Behavior<'In> = 
        match first with
        | Become next -> Become(fun message -> this.Combine(next message, second))
        | Return _ -> second
        
/// Builds an actor message handler using an actor expression syntax.
let actor = ActorBuilder()