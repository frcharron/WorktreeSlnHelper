namespace RepositorySolutionScanner

open System

module ExecuteHelper =
    let executeProcess (workingDir: string) (processName: string) (processArgs: string) (outputBuilder : Text.StringBuilder option) =
        let psi = new Diagnostics.ProcessStartInfo(processName, processArgs) 
        psi.WorkingDirectory <- workingDir
        match outputBuilder with
        | Some _ ->
            psi.UseShellExecute <- false
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            psi.CreateNoWindow <- true  
        | None ->
            psi.UseShellExecute <- true
        let proc = Diagnostics.Process.Start(psi) 
        match outputBuilder with
        | Some output ->
            proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
            proc.ErrorDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
            proc.BeginErrorReadLine()
            proc.BeginOutputReadLine()
            proc.WaitForExit()
            proc.ExitCode
        | None -> 
            0
        
