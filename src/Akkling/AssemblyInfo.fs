namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Akkling")>]
[<assembly: AssemblyProductAttribute("Akkling")>]
[<assembly: AssemblyDescriptionAttribute("F# wrapper library for Akka.NET")>]
[<assembly: AssemblyVersionAttribute("0.2")>]
[<assembly: AssemblyFileVersionAttribute("0.2")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.2"
