namespace RepositorySolutionScanner

open System

module ExecuteHelper =
    
    let Execute (workingDir: string) (processName: string) (processArgs: string) (outputConsole: (Diagnostics.DataReceivedEventArgs -> unit)) =
        let processExec = new Diagnostics.Process()
        processExec.StartInfo.WorkingDirectory <- workingDir
        processExec.StartInfo.FileName <- processName
        processExec.StartInfo.Arguments <- processArgs
        processExec.StartInfo.UseShellExecute <- false
        processExec.StartInfo.RedirectStandardOutput <- true
        processExec.StartInfo.RedirectStandardError <- true
        processExec.StartInfo.CreateNoWindow <- true
        processExec.OutputDataReceived.Add(outputConsole)
        processExec.Start() |> ignore
        processExec.BeginOutputReadLine()
        processExec.WaitForExit()

    let ExecuteProcessAsync (workingDir: string) (processName: string) (processArgs: string) (outputConsole: (Diagnostics.DataReceivedEventArgs -> unit)) = async {
        Execute workingDir processName processArgs outputConsole
    }

    let ExecuteStreamOutput (workingDir: string) (processName: string) (processArgs: string) (outputBuilder : Text.StringBuilder option) =
        match outputBuilder with
        | Some output ->
            Execute workingDir processName processArgs (fun args -> output.Append(args.Data) |> ignore) |> ignore
        | None ->
            let psi = new Diagnostics.ProcessStartInfo(processName, processArgs) 
            psi.WorkingDirectory <- workingDir
            psi.UseShellExecute <- true
            let proc = Diagnostics.Process.Start(psi)
            proc.WaitForExit()
        
