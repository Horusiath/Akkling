module examples

[<EntryPoint>]
let main argv =
    printfn "akkling examples application"
    examples_basic.run()

    printfn "Press enter to quit..."
    System.Console.ReadLine() |> ignore
    0 // return an integer exit code
