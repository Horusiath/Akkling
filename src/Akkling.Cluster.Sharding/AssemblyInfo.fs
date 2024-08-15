namespace System

open System.Reflection

[<assembly: AssemblyTitle("Akkling.Cluster.Sharding")>]
[<assembly: AssemblyProduct("Akkling")>]
[<assembly: AssemblyDescription("F# wrapper library for Akka.NET")>]
do ()

module internal AssemblyVersionInformation =
    [<Literal>]
    let AssemblyTitle = "Akkling.Cluster.Sharding"

    [<Literal>]
    let AssemblyProduct = "Akkling"

    [<Literal>]
    let AssemblyDescription = "F# wrapper library for Akka.NET"
