#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../src/Akkling/bin/Debug/System.Collections.Immutable.dll"

open System
open Akkling
open Akka.Actor

let system = System.create "basic-sys" <| Configuration.defaultConfig()

let behavior (m:Actor<_>) =
    let rec loop () = actor {
        let! msg = m.Receive ()
        match msg with
        | "stop" -> return Stop
        | "unhandle" -> return Unhandled
        | x ->
            printfn "%s" x
            return! loop ()
    }
    loop ()

// 1. First approach - using explicit behavior loop
let helloRef = spawnAnonymous system (props behavior)

// 2. Second approach - using implicits
let helloBehavior _ = function
    | "stop" -> stop ()
    | "unhandle" -> unhandled ()
    | x -> printfn "%s" x |> ignored

helloRef <! "ok"        // "ok"
helloRef <! "unhandle"
helloRef <! "ok"        // "ok"

// 3. Using receiver combinators
//    - <|> executes right side always when left side was unhandled
//    - <&> executes right side only when left side was handled
let allwaysHandled _ n = printfn "always handled: %s" n |> ignored
let onlyWhenHanled _ n = printfn "combined behavior: %s" n |> ignored
let orRef = spawn system "or-actor" <| props (actorOf2 (helloBehavior <|> allwaysHandled))
let andRef = spawn system "and-actor" <| props (actorOf2 (helloBehavior <&> onlyWhenHanled))

orRef <! "ok"           // "ok"
orRef <! "unhandle"     // "always handled: unhandle"

andRef <! "ok"          // "ok"
                        // "combined behavior: ok"
andRef <! "unhandle"

// 4. Stateful actors

// 4.1. using explicit loop
let stateRef = spawnAnonymous system <| props(fun ctx ->
    let rec loop state = actor {
       let! n = ctx.Receive()
       match n with
       | "print" -> printfn "Current state: %A" state
       | other -> return! loop (other::state)
    }
    loop [])

stateRef <! "a"
stateRef <! "b"
stateRef <! "print"

// 4.2. more implicit
let rec looper state = function
    | "print" -> printfn "Current state: %A" state |> ignored
    | other -> become (looper (other::state))

let stateRef2 = spawnAnonymous system <| props(actorOf (looper []))

stateRef2 <! "a"
stateRef2 <! "b"
stateRef2 <! "print"
