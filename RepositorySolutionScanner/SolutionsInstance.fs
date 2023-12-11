namespace RepositorySolutionScanner

open System.IO
open Action
open ExecuteHelper
open System
open System.Threading.Tasks
open System.Xml
open FSharp.Text.RegexProvider

module SolutionsInstance =
    type BuildTools =
        | MSBuild
        | Dotnet
    with 
        member x.CanBuild = 
            match x with
            | MSBuild -> true
            | Dotnet -> true
        member x.CanRebuild = 
            match x with
            | MSBuild -> true
            | Dotnet -> true
        member x.CanPublish = 
            match x with
            | MSBuild -> false
            | Dotnet -> true
        member x.GetBuildCommand (projectPath: string) (configuration: string) (customArg: string) =
            match x with
            | MSBuild -> "msbuild.exe", $"/p:Configuration={configuration} {projectPath}"
            | Dotnet -> "dotnet",  $"build {projectPath} {customArg} -c {configuration}"
        member x.GetRebuildCommand (projectPath: string) (configuration: string) (customArg: string) =
            match x with
            | MSBuild -> "msbuild.exe", $"/p:Configuration={configuration} {projectPath} -t:rebuild"
            | Dotnet -> "dotnet", $"build {projectPath} {customArg} -c {configuration} --no-incremental"
        member x.GetPublishCommand (projectPath: string) (configuration: string) (customArg: string) (publishDirectory: string) = 
            match x with
            | MSBuild -> "msbuild.exe", ""
            | Dotnet -> "dotnet", $"publish {projectPath} {customArg} -c {configuration} --output {publishDirectory}"

    type Project = {
        Name : string
        OutputPath : string
        ProjectFilePath : string
        Framework : string array
        CustomCommand : Action.ProjectAct option
        BuildCommand : BuildTools
    }
    with 
        member x.Run(framework: string option) =
            match x.CustomCommand with
            | Some command ->
                    command.RunCmd.Execute(None)
            | _ ->
                let outputPath = 
                    match framework with
                    | Some framework when Array.contains framework x.Framework ->
                        Path.Combine(x.OutputPath, framework)
                    | _ ->
                        Path.Combine(x.OutputPath)
                ExecuteStreamOutput outputPath (Path.Combine(outputPath, (x.Name+".exe"))) "" None
        member x.Build (framework: string option) (output) =
            let arg = 
                match framework with
                | Some framework when Array.contains framework x.Framework ->
                    $"--framework {framework}"
                | _ -> 
                    ""
            let cmd, args = (x.BuildCommand.GetBuildCommand (x.ProjectFilePath) ("Debug") (arg))
            Execute x.ProjectFilePath cmd args output
        member x.Rebuild (framework: string option) (output) =
            let arg = 
                match framework with
                | Some framework when Array.contains framework x.Framework ->
                    $"--framework {framework}"
                | _ -> 
                    ""
            let cmd, args = (x.BuildCommand.GetRebuildCommand (x.ProjectFilePath) ("Debug") (arg))
            Execute x.ProjectFilePath cmd args output
        member x.Publish (framework: string option) (outputDirectory: string) (output) =
            let arg = 
                match framework with
                | Some framework when Array.contains framework x.Framework ->
                    $"--framework {framework}"
                | _ -> 
                    ""
            let cmd, args = (x.BuildCommand.GetPublishCommand (x.ProjectFilePath) ("Publish") (arg) (outputDirectory))
            Execute x.ProjectFilePath cmd args output
        override x.ToString() =
            sprintf "\n\t\tName: %s\n\t\tPath: %s\n" x.Name x.OutputPath
        static member Default =
            {
                Name = ""
                OutputPath = ""
                ProjectFilePath = ""
                Framework = Array.empty
                CustomCommand = None
                BuildCommand = Dotnet
            }
        static member Create (filename: string) (command: ProjectAct option) =
            try 
                use fileStream = new StreamReader (filename)
                let xmlDoc = new XmlDocument()
                xmlDoc.LoadXml (fileStream.ReadToEnd())
                let isNetExecutable =
                    xmlDoc.SelectNodes("//OutputType")
                    |> Seq.cast<XmlNode>
                    |> Seq.exists (fun (node:XmlNode) -> 
                        node.InnerText.ToString().ToUpper().Equals "EXE")
                let isCExecutable =
                    xmlDoc.SelectNodes("//OutputFile")
                    |> Seq.cast<XmlNode>
                    |> Seq.exists (fun (node:XmlNode) -> 
                        node.InnerText.ToString().Contains ".exe")
                if isNetExecutable || isCExecutable then
                    let outputPaths = 
                        xmlDoc.SelectNodes("//OutputPath")
                        |> Seq.cast<XmlNode>
                        |> Seq.map (fun (node:XmlNode) -> node.InnerText.ToString())
                        |> Seq.toList
                    let targetFramework = 
                        xmlDoc.SelectNodes("//TargetFrameworks")
                        |> Seq.cast<XmlNode>
                        |> Seq.map (fun (node:XmlNode) -> node.InnerText.ToString())
                        |> Seq.map (fun (targets) -> targets.Split(';') |> Array.toSeq)
                        |> Seq.collect id
                        |> Seq.toArray
                    {
                        Name = Path.GetFileNameWithoutExtension(filename)
                        OutputPath = 
                            if outputPaths.Length > 0 then
                                Path.Combine(Path.GetDirectoryName(filename),outputPaths[0])
                            else
                                Path.Combine(Path.GetDirectoryName(filename), "bin", "Debug")
                        ProjectFilePath = filename
                        Framework = targetFramework
                        CustomCommand = command
                        BuildCommand =
                            match Path.GetExtension(filename) with
                            | "vcxproj" -> MSBuild
                            | _ -> Dotnet
                    }
                    |> Some
                else None
            with _ ->
                None

    type ProjectInfoRegex = Regex<"Project\\([^)]*\\) = \"(?<ProjectName>[^\"]*)\", \"(?<ProjectPath>[^\"]*)\", \"(?<ProjectGUID>\\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\\})\"">
    type SolutionInfoRegex = Regex<"SolutionGuid = \"(?<SolutionGUID>\\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\\})\"">

    type SolutionAttribut =
        | ProjectInfo of projectName: string * projectGUID: string * projectRelativePath: string
        | SolutionInfo of solutionGuid: string
        | ConfigurationMapping of solutionCfg: string * projectCfg: string
    with
        static member GetAttribut(solutionStream: StreamReader) =
            let line = solutionStream.ReadLine()
            match ProjectInfoRegex().TryTypedMatch(line), SolutionInfoRegex().TryTypedMatch(line) with
            | Some projet, _ ->
                let definition = line.Split '='
                let property = definition[1].Split ','
                Some(ProjectInfo(projet.ProjectName.Value, projet.ProjectGUID.Value, projet.ProjectPath.Value))
            | _, Some solution ->
                let definition = line.Split '='
                Some(SolutionInfo(solution.SolutionGUID.Value))
            | _ ->
                None      

    type Solution = {
        Guid : string
        Name : string
        File : string
        Path : string
        RunningProject : Project option
        PublishDir : string
        CustomCommand: Action.SolutionAct option
    }
    with 
        member x.CanOpen = true
        member x.CanBuild = 
            match  x.RunningProject with
            | Some project -> project.BuildCommand.CanBuild
            | None -> true
        member x.CanRun = x.RunningProject.IsSome
        member x.CanPublish = 
            match  x.RunningProject with
            | Some project -> project.BuildCommand.CanPublish
            | None -> true
        member x.GetFilePath = (Path.Combine(x.Path,(sprintf "%s.sln" x.Name)))
        member x.GetFramework = 
            match x.RunningProject with
            | Some project -> project.Framework
            | None -> Array.empty
        member x.Open() =
            ExecuteStreamOutput x.Path x.GetFilePath "" None

        member x.Build(framework: string option) (output) =
            Task.Run(fun () -> 
                match x.CustomCommand with
                | Some command when command.BuildCmd.IsSome ->
                    command.BuildCmd.Value.Execute None
                | _ ->
                    match x.RunningProject with
                    | Some project ->
                        //sprintf "--framework %s " framework
                        project.Build framework output
                    | _ ->
                        Execute x.Path "dotnet" (sprintf "build %s.sln -c Debug" x.Name) output
            )
        member x.Rebuild(framework: string option) (output) =
            Task.Run(fun () -> 
                match x.CustomCommand with
                | Some command when command.RebuildCmd.IsSome ->
                    command.RebuildCmd.Value.Execute None
                | _ ->
                    match (x.RunningProject) with
                    | Some project ->
                        //sprintf "--framework %s " framework
                        project.Rebuild framework output
                    | _ ->
                        Execute x.Path "dotnet" (sprintf "build %s.sln -c Debug --no-incremental" x.Name) output
            )
        member x.Clean(framework: string option) (output) =
            Task.Run(fun () -> 
                let commandArg = 
                    match framework with
                    | Some framework when Array.contains framework x.GetFramework ->
                        sprintf "--framework %s " framework
                    | _ ->
                        ""
                Execute x.Path "dotnet" (sprintf "build %s.sln %s" x.Name commandArg) output
            )
        member x.Run(framework: string option) = 
            Task.Run(fun () -> 
                match x.RunningProject with 
                | Some project -> 
                    project.Run(framework)
                | None -> 
                    printf "not define"
            )
        member x.Publish(framework: string option) (output) (directoryUri) = 
            Task.Run(fun () -> 
                match x.CustomCommand with
                | Some command when command.PublishCmd.IsSome ->
                    command.PublishCmd.Value.Execute None
                | _ ->
                    let publishDir =
                        match framework with
                        | Some framework -> Path.Combine(x.PublishDir, framework)
                        | None -> x.PublishDir
                    match x.RunningProject with
                    | Some project ->
                        project.Publish framework publishDir output
                    | _ ->
                        Execute x.Path "dotnet" (sprintf "publish %s.sln -c publish  --output %s" x.Name publishDir) output
                    directoryUri(publishDir) |> ignore
            )
        override x.ToString() =
            let project =
                match x.RunningProject with 
                | Some project -> (project.ToString())
                | None -> ""
            sprintf "Guid: %s\nName: %s\nPath: %s\nRunningProject: %s\n" x.Guid x.Name x.Path project
        static member Default =
            {
                Guid = ""
                Name = ""
                File = ""
                Path = ""
                RunningProject = None
                PublishDir = ""
                CustomCommand = None
            }
        static member Create (filename: string) (customAction: CustonSolutionAction) =
            use fileStream = new StreamReader (filename)
            let publishDir = 
                Path.Combine(Environment.GetEnvironmentVariable("TEMP"), Path.GetFileNameWithoutExtension(filename))

            let rec parsing (fileStream : StreamReader) (solution: Solution) : Solution =
                if fileStream.EndOfStream then
                    solution
                else
                    match SolutionAttribut.GetAttribut(fileStream) with
                    | Some (ProjectInfo (name, guid, path)) -> 
                        let project =
                            match solution.RunningProject with
                                | Some project when project.Name.Length = 0 ->
                                    Project.Create (Path.Combine(path)) project.CustomCommand
                                | None ->
                                    Project.Create (Path.Combine(path)) None
                                | _ -> solution.RunningProject
                        parsing fileStream {solution with RunningProject = project}
                    | Some (SolutionInfo guid) ->
                        let customAct, customProj = 
                            customAction.TryFindCustomAction (guid)
                        let runningProject =
                            match solution.RunningProject, customProj with
                            | Some project, Some _ -> {project with CustomCommand = customProj} |> Some
                            | None, Some _ -> {Project.Default with CustomCommand = customProj} |> Some
                            | _ -> solution.RunningProject
                        parsing fileStream {solution with 
                                                Guid = guid
                                                CustomCommand = customAct
                                                RunningProject = runningProject
                                            }
                    | _ -> 
                        parsing fileStream solution

            let customAct, customProj = 
                customAction.TryFindCustomAction (Path.GetFileNameWithoutExtension(filename))
            parsing fileStream {Solution.Default with 
                                    Guid = Path.GetFileNameWithoutExtension(filename)
                                    Name = Path.GetFileNameWithoutExtension(filename)
                                    File = Path.GetFileName(filename)
                                    Path = Path.GetDirectoryName(filename)
                                    PublishDir = publishDir
                                    CustomCommand = customAct
                                }

        static member ExtractSolutionsName (list: Solution array) =
            list
            |> Array.map (fun solution -> solution.Name)

        static member TryFindSolutionByName (name: string) (list: Solution array) =
            list
            |> Array.tryFind (fun solution -> solution.Name.Equals name)