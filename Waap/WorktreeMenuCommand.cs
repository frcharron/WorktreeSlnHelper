﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using RepositorySolutionScanner;
using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;

using Task = System.Threading.Tasks.Task;


namespace Waap
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class WorktreeMenuCommand
    {
        private DTE dte;
        private readonly RepositorySolutionScanner.Action.CustonSolutionAction action;
        private RepositoryInstance.Repository[] repositories;
        private readonly OleMenuCommandService cmdService;
        private ArrayList wtMenues;
        private ArrayList mruList;
        private ArrayList repositoryList;
        private CreateWorktree form;
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;
        public const int cmdidTestSubCmd = 0x0105;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("23495837-b876-4111-9ade-31c77e59af5b");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorktreeMenuCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private WorktreeMenuCommand(AsyncPackage package, OleMenuCommandService commandService, DTE dte)
        {
            this.dte = dte;

            this.action = RepositorySolutionScanner.Action.ParsingCustomSolutionFile("CustomAction.json");
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            this.cmdService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            this.wtMenues = new ArrayList();
            this.mruList = new ArrayList();
            repositoryList = new ArrayList();

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            cmdService.AddCommand(menuItem);

            RefreshWorktreeCommandService();
        }

        private void RefreshWorktreeCommandService() {
            var t = new Task(() => {
                StatusBarHelper.SetOperationOn(package, "Scan Repositories and Worktrees");
                this.InitMRUMenu(cmdService);
                StatusBarHelper.SetOperationOff(package);
            }
            );
            t.Start();
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static WorktreeMenuCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in WorktreeMenuCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new WorktreeMenuCommand(package, commandService, await package.GetServiceAsync(typeof(DTE)) as DTE);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            using (form = new CreateWorktree())
            {
                foreach (RepositoryInstance.Repository repository in repositoryList)
                {
                    if (repository.GitAttributs.IsRepository)
                    {
                        form.AddLocalRepositoryDirectories(repository);
                    }
                }
                form.ShowDialog();
            }
            RefreshWorktreeCommandService();
        }

        private int baseMRUID = (int)WaapPackage.cmdidMRUList;
        private void InitMRUMenu(OleMenuCommandService mcs)
        {
            repositories = RepositoryInstance.Repository.ScanRepositories(@"C:\git\", action);
            mruList.Clear();
            repositoryList.Clear();
            if (repositories != null)
            {
                if (null != this.mruList)
                {
                    foreach (var repository in repositories)
                    {
                        if (repository.GitAttributs.IsWorktree)
                        {
                            var parent = repository.RepositoryPath;
                            if (parent != null)
                            {

                            }
                            this.mruList.Add(string.Format(CultureInfo.CurrentCulture, repository.RepositoryName));
                        }
                        else if (repository.GitAttributs.IsRepository)
                        {
                            repositoryList.Add(repository);
                        }
                    }
                }
            }
            foreach (OleMenuCommand mc in wtMenues)
            {
                mcs.RemoveCommand(mc);
            }
            wtMenues.Clear();
            for (int i = 0; i < this.mruList.Count; i++)
            {
                var cmdID = new CommandID(CommandSet, this.baseMRUID + i);
                var mc = new OleMenuCommand(new EventHandler(OnMRUExec), cmdID);
                mc.BeforeQueryStatus += new EventHandler(OnMRUQueryStatus);
                mcs.AddCommand(mc);
                wtMenues.Add(mc);
            }
        }

        private void OnMRUQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                int MRUItemIndex = menuCommand.CommandID.ID - this.baseMRUID;
                if (MRUItemIndex >= 0 && MRUItemIndex < this.mruList.Count)
                {
                    menuCommand.Text = this.mruList[MRUItemIndex] as string;
                }
                menuCommand.Visible = true;
            }
        }

        private void OnMRUExec(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                int MRUItemIndex = menuCommand.CommandID.ID - this.baseMRUID;
                if (MRUItemIndex >= 0 && MRUItemIndex < this.mruList.Count)
                {
                    string selection = this.mruList[MRUItemIndex] as string;
                    for (int i = MRUItemIndex; i > 0; i--)
                    {
                        this.mruList[i] = this.mruList[i - 1];
                    }
                    this.mruList[0] = selection;
                    var repository = Array.Find(repositories, repo => repo.RepositoryName.Equals(selection) );
                    if (repository != null) {
                        using (OpenSession form = new OpenSession(selection, repository.Solutions))
                        {
                            form.ShowDialog();
                            var file = form.GetSelectedSolution();
                            if (file != null)
                            {
                                EnvDTE80.DTE2 dte2 = dte as EnvDTE80.DTE2;
                                if (dte2.Solution.IsOpen)
                                    dte2.Solution.Close(true);
                                dte2.Solution.Open(Path.Combine(file.Path, file.Name + file.FileType));
                            }
                        }
                    }
                }
            }
        }
    }
}
