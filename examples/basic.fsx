#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"

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