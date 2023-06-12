// See https://aka.ms/new-console-template for more information
using RepositorySolutionScanner;

class TestClass
{
    static void Main(string[] args)
    {
        var custom = RepositorySolutionScanner.Action.ParsingCustomSolutionFile("CustomAction.json");
        var solutions = Scanner.StartScan(GitHelper.GetTopLevelDirectory(@"C:\git\Genetec.Softwire_master\Source\SMC.Core"), custom);
        if (solutions != null)
        {
            foreach (var solution in solutions)
            {
                Console.WriteLine("_____________________________________________________________");
                Console.WriteLine(solution);
                if (solution.Guid.Contains( "02F09B95-F6E8-47BA-948F-9EC41FDD6D6E"))
                {
                    var output = new System.Text.StringBuilder();
                    Console.WriteLine("OPEN_____________________________________________________________");
                    solution.Open();
                    Console.WriteLine("BUILD_____________________________________________________________OPEN");
                    solution.Build("net7.0", output);
                    Console.WriteLine("REBUILD_____________________________________________________________BUILD");
                    solution.Rebuild(null, output);
                    Console.WriteLine("RUN_____________________________________________________________REBUILD");
                    solution.Run("net7.0");
                    Console.WriteLine("PUBLISH_____________________________________________________________RUN");
                    solution.Publish("net7.0", output);
                    Console.WriteLine("_____________________________________________________________PUBLISH");
                }
                Console.WriteLine("_____________________________________________________________");
            }
        }

        var repositories = RepositoryInstance.Repository.ScanRepositories(@"C:\git\", custom);
        if (repositories != null)
        {
            foreach (var repository in repositories)
            {
                Console.WriteLine(repository.RepositoryName);
                Console.WriteLine(repository.Solutions.Length);
            }
        }
    }
}


