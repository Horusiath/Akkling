// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
// Any binary output / copied to bin/projectName/projectName.dll will
// automatically be added as a binary to generate API docs for.
// for binaries output to root bin folder please add the filename only to the
// referenceBinaries list below in order to generate documentation for the binaries.
// (This is the original behaviour of ProjectScaffold prior to multi project support)
let referenceBinaries = []
// Web site location for the generated documentation
let website = "/Akkling"

let githubLink = "http://github.com/Horusiath/Akkling"

// Specify more information about your project
let info =
    [ "project-name", "Akkling"
      "project-author", "Bartosz Sypytkowski"
      "project-summary", "F# wrapper library for Akka.NET"
      "project-github", githubLink
      "project-nuget", "http://nuget.org/packages/Akkling" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#I "../../packages/FAKE/tools/"
#load "../../packages/FSharp.Formatting/FSharp.Formatting.fsx"
#r "NuGet.Core.dll"
#r "FakeLib.dll"

open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let bin = __SOURCE_DIRECTORY__ @@ "../../bin"
let content = __SOURCE_DIRECTORY__ @@ "../content"
let output = __SOURCE_DIRECTORY__ @@ "../output"
let files = __SOURCE_DIRECTORY__ @@ "../files"
let templates = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/FSharp.Formatting/"
let docTemplate = "docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRootsAll =
    new System.Collections.Generic.Dictionary<string, string list>()

layoutRootsAll.Add("en", [ templates; formatting @@ "templates"; formatting @@ "templates/reference" ])

subDirectories (directoryInfo templates)
|> Seq.iter (fun d ->
    let name = d.Name

    if name.Length = 2 || name.Length = 3 then
        layoutRootsAll.Add(
            name,
            [ templates @@ name
              formatting @@ "templates"
              formatting @@ "templates/reference" ]
        ))

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
    CopyRecursive files output true |> Log "Copying file: "
    ensureDirectory (output @@ "content")

    CopyRecursive (formatting @@ "styles") (output @@ "content") true
    |> Log "Copying styles and scripts: "

let binaries =
    let manuallyAdded = referenceBinaries |> List.map (fun b -> bin @@ b)

    let conventionBased =
        directoryInfo bin
        |> subDirectories
        |> Array.map (fun d -> d.FullName @@ (sprintf "%s.dll" d.Name))
        |> List.ofArray

    conventionBased @ manuallyAdded

let libDirs =
    let conventionBasedbinDirs =
        directoryInfo bin
        |> subDirectories
        |> Array.map (fun d -> d.FullName)
        |> List.ofArray

    conventionBasedbinDirs @ [ bin ]

// Build API reference from XML comments
let buildReference () =
    CleanDir(output @@ "reference")

    MetadataFormat.Generate(
        binaries,
        output @@ "reference",
        layoutRootsAll.["en"],
        parameters = ("root", root) :: info,
        sourceRepo = githubLink @@ "tree/master",
        sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
        publicOnly = true,
        libDirs = libDirs
    )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =

    // First, process files which are placed in the content root directory.

    Literate.ProcessDirectory(
        content,
        docTemplate,
        output,
        replacements = ("root", root) :: info,
        layoutRoots = layoutRootsAll.["en"],
        generateAnchors = true,
        processRecursive = false
    )

    // And then process files which are placed in the sub directories
    // (some sub directories might be for specific language).

    let subdirs =
        Directory.EnumerateDirectories(content, "*", SearchOption.TopDirectoryOnly)

    for dir in subdirs do
        let dirname = (new DirectoryInfo(dir)).Name

        let layoutRoots =
            // Check whether this directory name is for specific language
            let key = layoutRootsAll.Keys |> Seq.tryFind (fun i -> i = dirname)

            match key with
            | Some lang -> layoutRootsAll.[lang]
            | None -> layoutRootsAll.["en"] // "en" is the default language

        Literate.ProcessDirectory(
            dir,
            docTemplate,
            output @@ dirname,
            replacements = ("root", root) :: info,
            layoutRoots = layoutRoots,
            generateAnchors = true
        )

// Generate
copyFiles ()
#if HELP
buildDocumentation ()
#endif
#if REFERENCE
buildReference ()
#endif
