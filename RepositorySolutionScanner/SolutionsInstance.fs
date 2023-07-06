﻿namespace RepositorySolutionScanner

open System.IO
open Action
open ExecuteHelper
open System
open System.Threading.Tasks

module SolutionsInstance =
    type Project = {
        Name : string
        OutputPath : string
        Framework : string array
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
                Framework = Array.empty
                CustomCommand = None
            }
        static member Create (filename: string) (command: ProjectAct option) =
            try 
                use fileStream = new StreamReader (filename)
                let rec parsing (fileStream : StreamReader) (project: Project) : Project option =
                    if fileStream.EndOfStream then
                        if project.Name.Length = 0 then
                            None
                        else
                            Some project
                    else
                        let line = fileStream.ReadLine()
                        if line.Contains "<OutputType>Exe</OutputType>" then
                            parsing fileStream {project with Name = Path.GetFileNameWithoutExtension(filename)}
                        else if line.Contains @"'$(Configuration)|$(Platform)'=='Debug|"then
                            let path = fileStream.ReadLine().Replace(@"<OutputPath>", "").Replace(@"</OutputPath>", "").TrimStart().TrimEnd()
                            parsing fileStream {project with OutputPath = Path.Combine(Path.GetDirectoryName(filename),path)}
                        else if line.Contains "TargetFrameworks" then
                            let framework = 
                                line.Replace(@"<TargetFrameworks>", "").Replace(@"</TargetFrameworks>", "").TrimStart().TrimEnd().Split(';')
                                |> Array.filter(fun contain -> not (contain.Length = 0))
                            parsing fileStream {project with Framework = framework}
                        else
                            parsing fileStream project
                parsing fileStream {Project.Default with 
                                        OutputPath = Path.Combine(Path.GetDirectoryName(filename), "bin", "Debug")
                                        CustomCommand = command
                                    }
            with _ ->
                None

    let cleanSolutionCommand path name arg out = 
        ExecuteStreamOutput path "dotnet" (sprintf "build %s.sln %s" name arg) out

    let build path name framework listFramework customCmd output =
        match customCmd with
        | Some command when command.BuildCmd.IsSome ->
            command.BuildCmd.Value.Execute output
        | _ ->
            let commandArg = 
                match framework with
                | Some framework when Array.contains framework listFramework ->
                    sprintf "--framework %s " framework
                | _ ->
                    ""
            ExecuteStreamOutput path "dotnet" (sprintf "build %s.sln %s-c Debug --force" name commandArg) output

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

        member x.Build(framework: string option) (output: Text.StringBuilder option) =
            Task.Run(fun () -> 
                build x.Path x.Name framework x.GetFramework x.CustomCommand output
            )
        member x.Rebuild(framework: string option) (output: Text.StringBuilder option) =
            Task.Run(fun () -> 
                match x.CustomCommand with
                | Some command when command.RebuildCmd.IsSome ->
                    command.RebuildCmd.Value.Execute output
                | _ ->
                    let outputLocal = new Text.StringBuilder() |> Some //That will block the command until completed
                    let commandArg = 
                        match framework with
                        | Some framework when Array.contains framework x.GetFramework ->
                            sprintf "--framework %s " framework
                        | _ ->
                            ""
                    let _ = cleanSolutionCommand x.Path x.Name commandArg outputLocal
                    cleanSolutionCommand x.Path x.Name commandArg output
                    build x.Path x.Name framework x.GetFramework x.CustomCommand output
            )
        member x.Clean(framework: string option) (output) =
            Task.Run(fun () -> 
                let commandArg = 
                    match framework with
                    | Some framework when Array.contains framework x.GetFramework ->
                        sprintf "--framework %s " framework
                    | _ ->
                        ""
                cleanSolutionCommand x.Path x.Name commandArg output
            )
        member x.Run(framework: string option) = 
            Task.Run(fun () -> 
                match x.RunningProject with 
                | Some project -> 
                    project.Run(framework)
                | None -> 
                    printf "not define"
            )
        member x.Publish(framework: string option) (output: Text.StringBuilder option) = 
            Task.Run(fun () -> 
                match x.CustomCommand with
                | Some command when command.PublishCmd.IsSome ->
                    command.PublishCmd.Value.Execute output
                | _ ->
                    let frameworkCmd, publishDir =
                        match framework with
                        | Some framework -> (sprintf "--framework %s" framework), Path.Combine(x.PublishDir, framework)
                        | None -> "", x.PublishDir
                    if  not (System.IO.Directory.Exists(publishDir)) then
                        System.IO.Directory.CreateDirectory publishDir |> ignore
                    ExecuteStreamOutput x.Path "dotnet" (sprintf "publish %s.sln -c Release --force --output %s %s" x.Name publishDir frameworkCmd) None
                    ExecuteStreamOutput x.Path "explorer.exe" publishDir None |> ignore
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
                    if line.Contains "Project(" && (solution.RunningProject.IsNone) then
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