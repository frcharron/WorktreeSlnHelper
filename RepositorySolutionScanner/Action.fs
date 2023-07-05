namespace RepositorySolutionScanner

open System
open System.IO
open Newtonsoft.Json
open ExecuteHelper

module Action =
    type Command = {
        RelativeWorkingDirectory : string
        Application : string
        Arguments : string
        Framework : string option
    }
    with
        member x.Execute (output: Text.StringBuilder option) =
            ExecuteStreamOutput x.RelativeWorkingDirectory x.Application x.Arguments output
        static member Create (wd: string) (app: string) (arg: string) = 
            {
                RelativeWorkingDirectory = wd
                Application = app
                Arguments = arg
                Framework = None
            }

    type SolutionAct = {
        Guid : string
        BuildCmd : Command option
        RebuildCmd : Command option
        PublishCmd : Command option
    }
    with
        static member Create (guid: string) =
            {
                Guid = guid
                BuildCmd = None
                RebuildCmd = None
                PublishCmd = None
            }

    type ProjectAct = {
        SolutionGuid : string
        RunCmd : Command
    }
    with
        static member Create (guid: string) (run: Command) =
            {
                SolutionGuid = guid
                RunCmd = run
            }

    type CustonSolutionAction = {
        CustonSolutionAction : SolutionAct array
        CustomProjectAction : ProjectAct array
    }
    with 
        member x.TryFindCustomAction (guid: string) =
            x.CustonSolutionAction
            |> Array.tryFind (fun solAct -> guid.Equals (solAct.Guid, System.StringComparison.CurrentCultureIgnoreCase) ),

            x.CustomProjectAction
            |> Array.tryFind (fun projAct -> guid.Equals (projAct.SolutionGuid, System.StringComparison.CurrentCultureIgnoreCase) )

    let ParsingCustomSolutionFile(filepath: string) = 
        if File.Exists filepath then
            use fileStream = new StreamReader (filepath)
            let jsonConfig = fileStream.ReadToEnd()
            JsonConvert.DeserializeObject<CustonSolutionAction>(jsonConfig)
        else
            {
                CustonSolutionAction = Array.empty
                CustomProjectAction = Array.empty
            }
