namespace System
open System.Reflection

[<assembly: AssemblyTitle("Akkling.Streams")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "Akkling.Streams"
    let [<Literal>] AssemblyProduct = "Akkling"
    let [<Literal>] AssemblyDescription = "F# wrapper library for Akka.NET"
