namespace System
open System.Reflection

[<assembly: AssemblyTitle("Akkling.Persistence")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "Akkling.Persistence"
    let [<Literal>] AssemblyProduct = "Akkling"
    let [<Literal>] AssemblyDescription = "F# wrapper library for Akka.NET"
