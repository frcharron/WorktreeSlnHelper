namespace RepositorySolutionScanner

open System.IO
open SolutionsInstance
open Action

module Scanner =
    let GetFiles(repository: string) : FileInfo array =
        let wd = new DirectoryInfo(repository);
        wd.GetFiles("*.sln", SearchOption.AllDirectories)

    let StartScanAsync (filerepository: string) (customAction: CustonSolutionAction) = async {
        return 
            GetFiles(filerepository)
            |> Array.map (fun file ->
                Solution.Create file.FullName customAction
            )
    }

    let StartScan (filerepository: string) (customAction: CustonSolutionAction) =
        StartScanAsync filerepository customAction
        |> Async.RunSynchronously
        


