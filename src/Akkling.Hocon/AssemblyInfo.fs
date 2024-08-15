namespace System

open System.Reflection

[<assembly: AssemblyTitle("Akkling.Hocon")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    [<Literal>]
    let AssemblyTitle = "Akkling.Hocon"

    [<Literal>]
    let AssemblyProduct = "Akkling"

    [<Literal>]
    let AssemblyDescription = "F# wrapper library for Akka.NET"
