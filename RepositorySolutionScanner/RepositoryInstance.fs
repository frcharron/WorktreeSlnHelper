namespace RepositorySolutionScanner

open System.IO
open Scanner
open Action

module RepositoryInstance =
    type Repository = {
        RepositoryName: string
        Solutions: SolutionsInstance.Solution array
        GitAttributs: GitHelper.GitDirectory
    }
    with 
        member x.RepositoryPath = 
            match x.GitAttributs with
            | GitHelper.GitDirectory.Repository (path, _, _, _)
            | GitHelper.GitDirectory.Worktree (path, _, _, _) -> path
            | GitHelper.GitDirectory.NotAGitRespository -> ""
        static member ScanRepositories(containRepoPath: string) (customAction: CustonSolutionAction) = 
            Directory.GetDirectories(containRepoPath, "*", SearchOption.TopDirectoryOnly)
            |> Array.map (fun directory ->
                {
                    RepositoryName = Path.GetFileName(directory)
                    Solutions = Scanner.StartScan directory customAction 
                    GitAttributs =  GitHelper.GitDirectory.GetRepositoryType(directory)
                }
            )
            |> Array.filter (fun repo -> 
                match repo.GitAttributs with
                | GitHelper.GitDirectory.Repository _
                | GitHelper.GitDirectory.Worktree _ -> true
                | GitHelper.GitDirectory.NotAGitRespository -> false
            )
        static member TryFind (repositories : Repository array) (repositoryName : string) =
            repositories
            |> Array.tryFind (fun repo -> repo.RepositoryName.Equals repositoryName)
