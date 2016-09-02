namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Akkling.Streams")>]
[<assembly: AssemblyProductAttribute("Akkling")>]
[<assembly: AssemblyDescriptionAttribute("F# wrapper library for Akka.NET")>]
[<assembly: AssemblyVersionAttribute("0.3.0")>]
[<assembly: AssemblyFileVersionAttribute("0.3.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.3.0"
    let [<Literal>] InformationalVersion = "0.3.0"
