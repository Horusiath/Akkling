//-----------------------------------------------------------------------
// <copyright file="FsApi.fs" company="Akka.NET Project">
//     Copyright (C) 2009-2015 Typesafe Inc. <http://www.typesafe.com>
//     Copyright (C) 2013-2015 Akka.NET project <https://github.com/akkadotnet/akka.net>
//     Copyright (C) 2013-2015 Bartosz Sypytkowski <gttps://github.com/Horusiath>
// </copyright>
//-----------------------------------------------------------------------

module Akkling.Tests.ComputationExpression

open Akkling
open Akkling.TestKit
open System
open System.Threading.Tasks
open Xunit

[<Fact(Skip = "FIXME: https://github.com/Horusiath/Akkling/issues/54")>]
let ``Actor computation try-with should generate tail call`` () =  testDefault <| fun tck -> 
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