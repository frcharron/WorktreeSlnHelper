namespace RepositorySolutionScanner

open System.IO
open Scanner
open Action

module RepositoryInstance =
    type Repository = {
        RepositoryName: string
        Solutions: SolutionsInstance.Solution array
    }
    with 
        static member ScanRepositories(containRepoPath: string) (customAction: CustonSolutionAction) = 
            let repositories =
                Directory.GetDirectories(containRepoPath, "*", SearchOption.TopDirectoryOnly)
                |> Array.filter (fun directory -> 
                    match GitHelper.GitDirectory.GetRepositoryType(directory) with
                    | GitHelper.GitDirectory.Repository _
                    | GitHelper.GitDirectory.Worktree _ -> true
                    | GitHelper.GitDirectory.NotAGitRespository -> false
                )
            repositories 
            |> Array.map (fun repository ->
                {
                    RepositoryName = Path.GetFileName(repository)
                    Solutions = Scanner.StartScan repository customAction 
                }
            )
