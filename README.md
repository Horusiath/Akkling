# Akkling

This is the experimental fork of Akka.FSharp library, introducing new features such as typed actor refs, and also simplifying existing Akka.FSharp API. The main reason for splitting from official API is to be able to introduce new (also experimental), but possibly breaking changes outside existing Akka release cycle.

**Read [wiki pages](https://github.com/Horusiath/Akkling/wiki/Table-of-contents) for more info.**

## Get's started

For more examples check [examples](https://github.com/Horusiath/Akkling/tree/master/examples) section.

Obligatory hello world example:

```fsharp
open Akkling

use system = System.create "my-system" <| Configuration.defaultConfig()
let aref = spawn system "printer" <| props(actorOf (fun m -> printfn "%s" m |> ignored))

aref <! "hello world"
aref <! 1 // ERROR: we have statically typed actors here
```

Another example using stateful actors:

```fsharp
open Akkling

use system = System.create "my-system" <| Configuration.defaultConfig()

type Message =
    | Hi
    | Greet of string

let rec greeter lastKnown = function
    | Hi -> printfn "Who sent Hi? %s?" lastKnown |> ignored
    | Greet(who) ->
        printfn "%s sends greetings"
        become (greeter who)

let aref = spawn system "printer" <| props(actorOf (greeter "Unknown"))

aref <! Greet "Tom"
aref <! Greet "Jane"
aref <! Hi
```

## Maintainer(s)

- [@Horusiath](https://github.com/Horusiath)