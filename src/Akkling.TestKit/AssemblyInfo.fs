namespace System
open System.Reflection
open System.Runtime.InteropServices

[<assembly: AssemblyTitleAttribute("Akkling.TestKit")>]
[<assembly: AssemblyProductAttribute("Akkling")>]
[<assembly: AssemblyDescriptionAttribute("F# wrapper library for Akka.NET")>]
[<assembly: AssemblyCopyrightAttribute("Copyright © 2015 Bartosz Sypytkowski")>]
[<assembly: ComVisibleAttribute(false)>]
[<assembly: CLSCompliantAttribute(true)>]
[<assembly: AssemblyVersionAttribute("0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.1")>]

do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.1"
