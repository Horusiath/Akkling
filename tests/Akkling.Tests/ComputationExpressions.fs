//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <https://www.lightbend.com>
//     Copyright (C) 2013-2024 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2024 Bartosz Sypytkowski and contributors <https://github.com/Horusiath/Akkling>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Tests.ComputationExpression

open Akkling
open Akkling.TestKit
open Akkling.Extensions
open System
open System.Threading.Tasks
open Xunit

[<Fact>]
let ``Actor computation try-with should not overflow the stack`` () =  testDefault <| fun tck -> 
    let complete = TaskCompletionSource<_>()
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop n =
            actor {
                try
                    let! msg = ctx.Receive()
                    if msg = 1
                    then complete.SetResult ()
                    return! loop msg
                with e -> 
                    logException ctx e
                    complete.SetException e
            }
        loop 0)
    for i = 1000000 downto 1 do
        ref <! i

    complete.Task.Wait()

[<Fact>]
let ``Actor computation try-with handles exception`` () =  testDefault <| fun tck -> 
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                try
                    let! msg = ctx.Receive()
                    ctx.Sender() <! msg
                    if msg = 0 || msg = 2 then failwithf "exception-%d" msg
                    return! loop ()
                with e -> 
                    ctx.Sender() <! e.Message
            }
        loop ())
    ref <! 0
    ref <! 1
    ref <! 2
    ref <! 3

    expectMsg tck 0 |> ignore
    expectMsg tck "exception-0" |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck "exception-2" |> ignore
    expectMsg tck 3 |> ignore
    
[<Fact>]
let ``Actor computation try-with after receive handles exception`` () =  testDefault <| fun tck -> 
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                let! msg = ctx.Receive()
                try
                    ctx.Sender() <! msg
                    if msg = 2 then failwithf "exception-%d" msg
                    return! loop ()
                with e -> 
                    ctx.Sender() <! e.Message
            }
        loop ())
    ref <! 0
    ref <! 1
    ref <! 2
    ref <! 3
            
    expectMsg tck 0 |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck "exception-2" |> ignore
    expectMsg tck 3 |> ignore

[<Fact>]
let ``Actor computation try-finally should not overflow the stack`` () =  testDefault <| fun tck -> 
    let complete = TaskCompletionSource<_>()
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop n =
            actor {
                try
                    let! msg = ctx.Receive()
                    return! loop msg
                finally
                    if n = 2
                    then complete.SetResult ()
            }
        loop 0)
    for i = 1000000 downto 1 do
        ref <! i

    complete.Task.Wait()

[<Fact>]
let ``Actor computation try-finally executes finally`` () =  testDefault <| fun tck -> 
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                let mutable reply = None
                try
                    let! (msg : obj) = ctx.Receive()
                    match msg with
                    | PostRestart ex -> ctx.Sender() <! ex.Message
                    | :? int as msg ->
                        reply <- Some msg
                        if msg = 0 || msg = 2 then failwithf "exception-%d" msg
                        return! loop ()
                    | _ -> ()
                finally
                    match reply with
                    | Some msg -> ctx.Sender() <! msg
                    | None -> ()
                    reply <- None
            }
        loop ())
    ref <! box 0
    ref <! box 1
    ref <! box 2
    ref <! box 3

    expectMsg tck 0 |> ignore
    expectMsg tck "exception-0" |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck "exception-2" |> ignore
    expectMsg tck 3 |> ignore
    
[<Fact>]
let ``Actor computation try-finally after receive executes finally`` () =  testDefault <| fun tck -> 
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                let! msg = ctx.Receive()
                try
                    ctx.Sender() <! msg
                    if msg = 2 then failwithf "exception-%d" msg
                    return! loop ()
                finally
                    ctx.Sender() <! msg
            }
        loop ())
    ref <! 0
    ref <! 1
    ref <! 2
    ref <! 3
            
    expectMsg tck 0 |> ignore
    expectMsg tck 0 |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck 3 |> ignore
    expectMsg tck 3 |> ignore

[<Fact>]
let ``Actor computation use should not overflow the stack`` () =  testDefault <| fun tck -> 
    let complete = TaskCompletionSource<_>()
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                let! msg = ctx.Receive()
                use _ = { new IDisposable with member _.Dispose () = () }
                if msg = 1 then complete.SetResult ()
                return! loop ()
            }
        loop ())
    for i = 1000000 downto 1 do
        ref <! i

    complete.Task.Wait()
    
[<Fact>]
let ``Actor computation use disposes objects`` () =  testDefault <| fun tck -> 
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                let! msg = ctx.Receive()
                let sender = ctx.Sender()
                use x = { new IDisposable with
                    member _.Dispose () = sender <! (sprintf "disposed-%d" msg)
                }
                ctx.Sender() <! msg
                if msg = 2 then failwithf "exception-%d" msg
                return! loop ()
            }
        loop ())
    ref <! 0
    ref <! 1
    ref <! 2
    ref <! 3
    
    expectMsg tck 0 |> ignore
    expectMsg tck "disposed-0" |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck "disposed-1" |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck "disposed-2" |> ignore
    expectMsg tck 3 |> ignore
    expectMsg tck "disposed-3" |> ignore
    
[<Fact>]
let ``Actor computation use before receive disposes objects`` () =  testDefault <| fun tck -> 
    let ref = spawn tck "actor" (props <| fun ctx ->
        let rec loop () =
            actor {
                use _ = { new IDisposable with
                    member _.Dispose () = ctx.Sender() <! "disposed"
                }
                let! msg = ctx.Receive()
                ctx.Sender() <! msg
                return! loop ()
            }
        loop ())
    ref <! 0
    ref <! 1
    ref <! 2
    ref <! 3
    
    expectMsg tck 0 |> ignore
    expectMsg tck "disposed" |> ignore
    expectMsg tck 1 |> ignore
    expectMsg tck "disposed" |> ignore
    expectMsg tck 2 |> ignore
    expectMsg tck "disposed" |> ignore
    expectMsg tck 3 |> ignore
    expectMsg tck "disposed" |> ignore

[<Fact>]
let ``Actor try-with should generate tail call`` () =  testDefault <| fun tck -> 
    let complete = TaskCompletionSource<_>()
    let ref = spawn tck "actor" (props <| actorOf (fun msg ->
        try
            if msg = 1 then complete.SetResult ()
            ignored ()
        with e ->
            complete.SetException e
            ignored ()))
    for i = 1000000 downto 1 do
        ref <! i

    complete.Task.Wait()