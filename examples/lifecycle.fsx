#r "../src/Akkling/bin/Debug/Akka.dll"
#r "../src/Akkling/bin/Debug/Wire.dll"
#r "../src/Akkling/bin/Debug/Newtonsoft.Json.dll"
#r "../src/Akkling/bin/Debug/FsPickler.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.dll"
#r "../src/Akkling/bin/Debug/FSharp.PowerPack.Linq.dll"
#r "../src/Akkling/bin/Debug/Akkling.dll"

open System
open Akkling
open Akka.Actor

let system = System.create "basic-sys" <| Configuration.defaultConfig()

let aref = spawn system "hello-actor" <| fun m ->
    let rec loop () = actor {
        let! (msg: obj) = m.Receive ()
        match msg with
        | :? LifecycleEvent as e ->
            match e with
            | PreStart -> printfn "Actor %A has started" m.Self
            | PostStop -> printfn "Actor %A has stopped" m.Self
        | x -> printfn "%A" x
        return! loop ()
    }
    loop ()

let sref = aref.Switch ()
sref <! "ok"
aref.Switch () <! PoisonPill.Instance