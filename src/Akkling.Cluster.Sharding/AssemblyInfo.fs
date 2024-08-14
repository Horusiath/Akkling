namespace System
open System.Reflection

[<assembly: AssemblyTitle("Akkling.Cluster.Sharding")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] AssemblyTitle = "Akkling.Cluster.Sharding"
    let [<Literal>] AssemblyProduct = "Akkling"
    let [<Literal>] AssemblyDescription = "F# wrapper library for Akka.NET"
