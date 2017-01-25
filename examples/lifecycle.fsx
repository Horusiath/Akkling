#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Hyperion.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"
#r "../src/Akkling/bin/Debug/System.Collections.Immutable.dll"

open System
open Akkling
open Akka.Actor

let system = System.create "basic-sys" <| Configuration.defaultConfig()

let aref = spawn system "hello-actor" <| props(fun m ->
    let rec loop () = actor {
        let! (msg: obj) = m.Receive ()
        match msg with
        | LifecycleEvent e ->
            match e with
            | PreStart -> printfn "Actor %A has started" m.Self
            | PostStop -> printfn "Actor %A has stopped" m.Self
            | _ -> return Unhandled
        | x -> printfn "%A" x
        return! loop ()
    }
    loop ())

let sref = retype aref
sref <! "ok"
(retype aref) <! PoisonPill.Instance