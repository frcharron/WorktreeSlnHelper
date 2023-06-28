using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;
using RepositorySolutionScanner;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using System.IO;

namespace SolutionsToolbar
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SolutionFileSelectCommand
    {
        private class Solutions {
            private SolutionsInstance.Solution[] listSolutions;
            private DateTime latestUpdate = DateTime.MinValue;
            private string latestDirectoryPath = "";

            public SolutionsInstance.Solution[] GetSolutions(string directoryRoot) {
                if (latestDirectoryPath != directoryRoot 
                        || listSolutions == null 
                        || listSolutions.Length == 0 
                        || (DateTime.UtcNow - latestUpdate) > TimeSpan.FromMinutes(5)) {
                    var custom = RepositorySolutionScanner.Action.ParsingCustomSolutionFile("CustomAction.json");
                    listSolutions = Scanner.StartScan(GitHelper.GetTopLevelDirectory(directoryRoot), custom);
                    latestUpdate = DateTime.UtcNow;
                    latestDirectoryPath = directoryRoot;
                }
                return listSolutions;
            }
        }

        private DTE dte;

        private Solutions listSolutionFiles = new Solutions();
        private SolutionsInstance.Solution currentSelectedSolution = null;
        private string currentSelectedFramework;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int SolutionListCommandId = 4183;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int SolutionSelectionCommandId = 4182;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int FrameworkSelectionCommandId = 4184;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int FrameworkListCommandId = 4185;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7c5421ef-54d3-4f13-b043-5bd988794dfc");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFileSelectCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SolutionFileSelectCommand(AsyncPackage package, OleMenuCommandService commandService, DTE dte)
        {
            this.dte = dte;

            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, SolutionSelectionCommandId);
            var menuItem = new OleMenuCommand(new EventHandler(this.ExecuteSolutionSelection), menuCommandID);
            commandService.AddCommand(menuItem);

            var menuListID = new CommandID(CommandSet, SolutionListCommandId);
            var menuListItem = new OleMenuCommand(new EventHandler(this.ExecuteSolutionList), menuListID);
            commandService.AddCommand(menuListItem);

            var frameworkCommandID = new CommandID(CommandSet, FrameworkSelectionCommandId);
            var frameworkItem = new OleMenuCommand(new EventHandler(this.ExecuteFrameworkSelection), frameworkCommandID);
            commandService.AddCommand(frameworkItem);

            var frameworkListID = new CommandID(CommandSet, FrameworkListCommandId);
            var frameworkListItem = new OleMenuCommand(new EventHandler(this.ExecuteFrameworkList), frameworkListID);
            commandService.AddCommand(frameworkListItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SolutionFileSelectCommand Instance
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
            // Switch to the main thread - the call to AddCommand in SolutionFileSelectCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new SolutionFileSelectCommand(package, commandService, await package.GetServiceAsync(typeof(DTE)) as DTE);

        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void ExecuteSolutionSelection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero)
                {
                    if (currentSelectedSolution == null)
                    {
                        var solutionPath = dte.Solution.FullName;
                        if (!String.IsNullOrEmpty(solutionPath)){
                            var solutions = listSolutionFiles.GetSolutions(Path.GetDirectoryName(solutionPath));
                            if (solutions.Length > 0)
                                currentSelectedSolution = solutions[0];
                            else
                                return;
                        }
                        else
                            return;
                    }
                        // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    Marshal.GetNativeVariantForObject(currentSelectedSolution.Name, vOut);
                }

                else if (newChoice != null)
                {
                    // new value was selected or typed in
                    // see if it is one of our items
                    var solutions = listSolutionFiles.GetSolutions(Path.GetDirectoryName(dte.Solution.FullName));
                    var newSolution = SolutionsInstance.Solution.TryFindSolutionByName(newChoice, solutions);

                    if (newSolution != null)
                    {
                        currentSelectedSolution = newSolution.Value;
                    }
                    else
                    {
                        throw (new ArgumentException("ParamNotValidStringInList")); // force an exception to be thrown
                    }
                }
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw (new ArgumentException("EventArgsRequired")); // force an exception to be thrown
            }
        }

        private void ExecuteSolutionList(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (inParam != null)
                {
                    throw (new ArgumentException("No param")); // force an exception to be thrown
                }
                else if (vOut != IntPtr.Zero)
                {
                    Marshal.GetNativeVariantForObject(SolutionsInstance.Solution.ExtractSolutionsName(listSolutionFiles.GetSolutions(Path.GetDirectoryName(dte.Solution.FullName))), vOut);
                }
                else
                {
                    throw (new ArgumentException("No output")); // force an exception to be thrown
                }
            }
        }

        private void ExecuteFrameworkSelection(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero)
                {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    if (currentSelectedSolution != null)
                    {
                        if (currentSelectedSolution.GetFramework.Length > 0) {
                            if (String.IsNullOrEmpty(currentSelectedFramework))
                            {
                                currentSelectedFramework = currentSelectedSolution.GetFramework[0];
                            }
                        }
                        else
                        {
                            currentSelectedFramework = "";
                        }
                        Marshal.GetNativeVariantForObject(currentSelectedFramework, vOut);
                    }
                    else
                        return;
                }

                else if (newChoice != null)
                {
                    if (currentSelectedSolution != null && currentSelectedSolution.GetFramework.Contains(newChoice))
                    {
                        currentSelectedFramework = newChoice;
                    }
                    else
                    {
                        throw (new ArgumentException("ParamNotValidStringInList")); // force an exception to be thrown
                    }
                }
            }
            else
            {
                // We should never get here; EventArgs are required.
                throw (new ArgumentException("EventArgsRequired")); // force an exception to be thrown
            }
        }

        private void ExecuteFrameworkList(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                object inParam = eventArgs.InValue;
                IntPtr vOut = eventArgs.OutValue;

                if (inParam != null)
                {
                    throw (new ArgumentException("No param")); // force an exception to be thrown
                }
                else if (vOut != IntPtr.Zero)
                {
                    if (currentSelectedSolution != null)
                    {
                        Marshal.GetNativeVariantForObject(currentSelectedSolution.GetFramework, vOut);
                    }
                    else {
                        Marshal.GetNativeVariantForObject("", vOut);
                    }
                }
                else
                {
                    throw (new ArgumentException("No output")); // force an exception to be thrown
                }
            }
        }
    }
}
