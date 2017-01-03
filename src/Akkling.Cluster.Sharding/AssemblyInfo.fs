namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Akkling.Cluster.Sharding")>]
[<assembly: AssemblyProductAttribute("Akkling")>]
[<assembly: AssemblyDescriptionAttribute("F# wrapper library for Akka.NET")>]
[<assembly: AssemblyVersionAttribute("0.4.1")>]
[<assembly: AssemblyFileVersionAttribute("0.4.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.4.1"
    let [<Literal>] InformationalVersion = "0.4.1"
