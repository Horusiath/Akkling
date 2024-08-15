namespace System

open System.Reflection

[<assembly: AssemblyTitle("Akkling.TestKit")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    [<Literal>]
    let AssemblyTitle = "Akkling.TestKit"

    [<Literal>]
    let AssemblyProduct = "Akkling"

    [<Literal>]
    let AssemblyDescription = "F# wrapper library for Akka.NET"
