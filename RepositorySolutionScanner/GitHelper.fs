namespace RepositorySolutionScanner

open System
open System.IO
open ExecuteHelper


module GitHelper =
    type ProcessResult = { 
        ExitCode : int; 
        StdOut : string; 
        StdErr : string 
    }

    //let executeProcess (workingDir: string) (processName: string) (processArgs: string) =
    //    let psi = new Diagnostics.ProcessStartInfo(processName, processArgs) 
    //    psi.UseShellExecute <- false
    //    psi.RedirectStandardOutput <- true
    //    psi.RedirectStandardError <- true
    //    psi.CreateNoWindow <- true        
    //    psi.WorkingDirectory <- workingDir
    //    let proc = Diagnostics.Process.Start(psi) 
    //    let output = new Text.StringBuilder()
    //    let error = new Text.StringBuilder()
    //    proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
    //    proc.ErrorDataReceived.Add(fun args -> error.Append(args.Data) |> ignore)
    //    proc.BeginErrorReadLine()
    //    proc.BeginOutputReadLine()
    //    proc.WaitForExit()
    //    { 
    //        ExitCode = proc.ExitCode
    //        StdOut = output.ToString()
    //        StdErr = error.ToString() 
    //    }

    let GetTopLevelDirectory (directoryPath: string) = 
        let out = new Text.StringBuilder()
        let _ = executeProcess directoryPath "git" "rev-parse --show-toplevel" (Some out)
        out.ToString()

    let ShowAllBranch (directoryPath: string) =
        let out = new Text.StringBuilder()
        let _ = executeProcess (GetTopLevelDirectory directoryPath) "git" "branch" (Some out)
        out.ToString()

    type GitDirectory = 
        | Repository of repositoryPath: string * originUri: string * parentBranch: string option * branch: string
        | Worktree of repositoryPath: string * worktreePath: string * parentBranch: string * branch: string
        | NotAGitRespository
    with
        member x.Fetch() = 
            ()
        member x.FastMerge() = 
            match x with
            | Repository _
            | Worktree _ -> ()
            | _ -> failwithf "Unable to fast merge directory because isn't a git directory" 
        member x.Delete () = 
            match x with
            | Repository (repositoryPath, _, _, _) -> 
                let out = new Text.StringBuilder()
                executeProcess (Path.GetDirectoryName(repositoryPath)) "rm" (sprintf "-rf %s" repositoryPath) (Some out) |> ignore
            | Worktree (repositoryPath, worktreePath, _, _) -> 
                let out = new Text.StringBuilder()
                executeProcess repositoryPath "git" (sprintf "worktree remove %s" worktreePath) (Some out) |> ignore
            | _ -> 
                failwithf "Unable to delete directory because isn't a git directory" 
        static member CreateWorktree (parentRepository: string) (branchSource: string) (worktreeName: string) = 
            let out = new Text.StringBuilder()
            let worktreeDestination = Path.Combine(Path.GetDirectoryName(parentRepository), sprintf "WT_%s" worktreeName)
            let branchName = sprintf "user/frcharron/%s" worktreeName
            let result = executeProcess parentRepository "git" (sprintf "add -b %s %s %s" branchName worktreeDestination branchSource) (Some out) |> ignore
            if result.Equals 0 then
                executeProcess parentRepository "git" (sprintf "config --global --add safe.directory %s" worktreeDestination) (Some out) |> ignore
                Some (Worktree (parentRepository, worktreeDestination, branchSource, branchName))
            else
                None
        static member GetRepositoryType (directoryPath: string) =
            let pathResult = GetTopLevelDirectory(directoryPath)
            if pathResult.Length = 0 then
                NotAGitRespository
            else 
                let path = Path.Combine(pathResult, ".git")
                let out = new Text.StringBuilder()
                let outBranch = new Text.StringBuilder()
                if Directory.Exists (path) then
                    let _ = executeProcess pathResult "git" "config --get remote.origin.url" (Some out)
                    let _ = executeProcess pathResult "git" "branch --show-current" (Some outBranch)
                    Repository (pathResult, out.ToString(), None, outBranch.ToString())
                elif File.Exists (path) then
                    let resultBranch = executeProcess pathResult "git" "branch --show-current" (Some out)
                    use fileStream = new StreamReader (path)
                    let gitdir = fileStream.ReadLine().Replace("gitdir: ", "")
                    let repoPath = gitdir.Split(".git")[0]
                    Worktree (repoPath, pathResult, "", out.ToString())
                else
                    NotAGitRespository