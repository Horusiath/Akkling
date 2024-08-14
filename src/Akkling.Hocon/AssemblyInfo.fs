namespace System
open System.Reflection

[<assembly: AssemblyTitle("Akkling.Hocon")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "Akkling.Hocon"
    let [<Literal>] AssemblyProduct = "Akkling"
    let [<Literal>] AssemblyDescription = "F# wrapper library for Akka.NET"
