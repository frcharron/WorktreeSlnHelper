namespace RepositorySolutionScanner

open System.IO
open Action
open ExecuteHelper
open System
open System.Threading.Tasks
open System.Xml

module SolutionsInstance =
    type Project = {
        Name : string
        OutputPath : string
        ProjectFilePath : string
        Framework : string array
        Configuration : string array
        CustomCommand : Action.ProjectAct option
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
        override x.ToString() =
            sprintf "\n\t\tName: %s\n\t\tPath: %s\n" x.Name x.OutputPath
        static member Default =
            {
                Name = ""
                OutputPath = ""
                ProjectFilePath = ""
                Framework = Array.empty
                Configuration = Array.empty
                CustomCommand = None
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
                        Configuration = Array.empty
                        CustomCommand = command
                    }
                    |> Some
                else None
            with _ ->
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
        member x.CanBuild = true
        member x.CanRun = x.RunningProject.IsSome
        member x.CanPublish = true
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
                    match (framework, x.RunningProject) with
                    | (Some framework, Some project) when Array.contains framework x.GetFramework ->
                        //sprintf "--framework %s " framework
                        Execute x.Path "dotnet" (sprintf "build %s --framework %s -c Debug" project.ProjectFilePath framework) output
                    | _ ->
                        Execute x.Path "dotnet" (sprintf "build %s.sln -c Debug" x.Name) output
            )
        member x.Rebuild(framework: string option) (output) =
            Task.Run(fun () -> 
                match x.CustomCommand with
                | Some command when command.RebuildCmd.IsSome ->
                    command.RebuildCmd.Value.Execute None
                | _ ->
                    match (framework, x.RunningProject) with
                    | (Some framework, Some project) when Array.contains framework x.GetFramework ->
                        //sprintf "--framework %s " framework
                        Execute x.Path "dotnet" (sprintf "build %s --framework %s -c Debug --no-incremental" project.ProjectFilePath framework) output
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
                    match (framework, x.RunningProject) with
                    | (Some framework, Some project) when Array.contains framework x.GetFramework ->
                        Execute x.Path "dotnet" (sprintf "publish %s --framework %s -c publish  --output %s" project.ProjectFilePath framework publishDir) output
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
                    let line = fileStream.ReadLine()
                    if line.Contains "Project(" then
                        let definition = line.Split '='
                        let property = definition[1].Split ','
                        let project =
                            match solution.RunningProject with
                            | Some project when project.Name.Length = 0 ->
                                Project.Create (Path.Combine(solution.Path,property[1].Replace("\"","").TrimStart().TrimEnd())) project.CustomCommand
                            | None ->
                                Project.Create (Path.Combine(solution.Path,property[1].Replace("\"","").TrimStart().TrimEnd())) None
                            | _ -> solution.RunningProject

                        parsing fileStream {solution with RunningProject = project}
                    else if line.Contains "SolutionGuid" then
                        let definition = line.Split '='
                        let guid =  definition[1].Replace(" ", "").Replace("{", "").Replace("{", "")
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
                    else
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