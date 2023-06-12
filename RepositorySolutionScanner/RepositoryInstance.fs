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
