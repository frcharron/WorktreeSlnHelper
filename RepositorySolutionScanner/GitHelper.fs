namespace RepositorySolutionScanner

open System
open System.IO
open ExecuteHelper



module GitHelper =

    let [<Literal>] VS_DIRECTORY_PATH = ".vs"
    let [<Literal>] PARENT_BRANCH_PATH = "parentBranch.cfg"

    let GetTopLevelDirectory (directoryPath: string) = 
        let out = new Text.StringBuilder()
        ExecuteStreamOutput directoryPath "git" "rev-parse --show-toplevel" (Some out)
        out.ToString()

    type GitDirectory = 
        | Repository of repositoryPath: string * originUri: string * parentBranch: string option * branch: string
        | Worktree of repositoryPath: string * worktreePath: string * parentBranch: string option * branch: string
        | NotAGitRespository
    with
        member x.ListBranch () =
            match x with
            | Repository (repositoryPath, _,_,_)
            | Worktree (repositoryPath, _,_,_) ->
                let out = new Text.StringBuilder()
                ExecuteStreamOutput repositoryPath "git" "branch -r" (Some out) |> ignore
                out.ToString()
            | _ -> failwithf "Unable to fetch directory because isn't a git directory" 
        member x.Fetch() = 
            match x with
            | Repository (repositoryPath, _,_,_)
            | Worktree (repositoryPath, _,_,_) ->
                let out = new Text.StringBuilder()
                ExecuteStreamOutput repositoryPath "git" "fetch --all" (Some out) |> ignore
                out.ToString()
            | _ -> failwithf "Unable to fetch directory because isn't a git directory" 
        member x.FastForwardMerge() =
            let doFFMerge (repositoryPath: string) (parentBranch: string) =
                let out = new Text.StringBuilder()
                ExecuteStreamOutput repositoryPath "git" (sprintf "merge --ff-only %s" parentBranch) (Some out) |> ignore
                out.ToString()
            match x with
            | Repository (gitPath,_,parentBranch,_)
            | Worktree (_,gitPath,parentBranch,_) when parentBranch.IsSome -> doFFMerge gitPath parentBranch.Value
            | _ -> failwithf "Unable to fast merge directory because isn't a git directory" 
        member x.Delete () = 
            match x with
            | Repository (repositoryPath, _, _, _) -> 
                let out = new Text.StringBuilder()
                ExecuteStreamOutput (Path.GetDirectoryName(repositoryPath)) "rm" (sprintf "-rf %s" repositoryPath) (Some out) |> ignore
                out.ToString()
            | Worktree (repositoryPath, worktreePath, _, branch) -> 
                let out = new Text.StringBuilder()
                ExecuteStreamOutput repositoryPath "git" (sprintf "worktree remove %s" worktreePath) (Some out) |> ignore
                ExecuteStreamOutput repositoryPath "git" (sprintf "branch -d %s" branch) (Some out) |> ignore
                out.ToString()
            | _ -> 
                failwithf "Unable to delete directory because isn't a git directory" 
        static member CreateWorktree (branchPrefix: string) (parentRepository: string) (branchSource: string) (worktreeName: string) = 
            let out = new Text.StringBuilder()
            let worktreeDestination = Path.Combine(Path.GetDirectoryName(parentRepository), sprintf "WT_%s" worktreeName)
            let branchName = sprintf "%s%s" branchPrefix worktreeName
            let result = ExecuteStreamOutput parentRepository "git" (sprintf "worktree add -b %s %s %s" branchName worktreeDestination branchSource) (Some out)
            Console.WriteLine(out.ToString());
            if result.Equals 0 then
                ExecuteStreamOutput parentRepository "git" (sprintf "config --global --add safe.directory %s" worktreeDestination) (Some out) |> ignore
                let configPath = Path.Combine(worktreeDestination, VS_DIRECTORY_PATH)
                let configFile = Path.Combine(configPath, PARENT_BRANCH_PATH)
                System.IO.Directory.CreateDirectory(configPath) |> ignore
                //use fileStream = System.IO.File.Create(configFile)
                use fileStream = new StreamWriter(configFile)
                fileStream.Write(branchSource)
                Some (Worktree (parentRepository, worktreeDestination, Some(branchSource), branchName))
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
                    let _ = ExecuteStreamOutput pathResult "git" "config --get remote.origin.url" (Some out)
                    let _ = ExecuteStreamOutput pathResult "git" "branch --show-current" (Some outBranch)
                    Repository (pathResult, out.ToString(), None, outBranch.ToString())
                elif File.Exists (path) then
                    let _ = ExecuteStreamOutput pathResult "git" "branch --show-current" (Some out)
                    let parentFilePath = Path.Combine(pathResult, VS_DIRECTORY_PATH, PARENT_BRANCH_PATH)
                    use fileStream = new StreamReader (path)
                    let gitdir = fileStream.ReadLine().Replace("gitdir: ", "")
                    let stringSeparator: string array = [|".git"|]
                    let repoPath = gitdir.Split(stringSeparator, StringSplitOptions.None)[0]
                    let parent = 
                        if File.Exists(parentFilePath) then
                            Some (File.OpenText(parentFilePath).ReadToEnd())
                        else
                            None
                    Worktree (repoPath, pathResult, parent, out.ToString())
                else
                    NotAGitRespository