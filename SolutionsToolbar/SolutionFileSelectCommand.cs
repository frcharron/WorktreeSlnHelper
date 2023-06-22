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

namespace SolutionsToolbar
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SolutionFileSelectCommand
    {
        private string[] dropDownComboChoices = { "Apple", "Orange", "Pears", "Bananas" };
        private string currentDropDownComboChoice = "Apple";

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandListId = 4183;

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4182;

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
        private SolutionFileSelectCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new OleMenuCommand(new EventHandler(this.Execute), menuCommandID);
            commandService.AddCommand(menuItem);

            var menuListID = new CommandID(CommandSet, CommandListId);
            var menuListItem = new OleMenuCommand(new EventHandler(this.ExecuteList), menuListID);
            commandService.AddCommand(menuListItem);
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
            Instance = new SolutionFileSelectCommand(package, commandService);
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
            OleMenuCmdEventArgs eventArgs = e as OleMenuCmdEventArgs;

            if (eventArgs != null)
            {
                string newChoice = eventArgs.InValue as string;
                IntPtr vOut = eventArgs.OutValue;

                if (vOut != IntPtr.Zero)
                {
                    // when vOut is non-NULL, the IDE is requesting the current value for the combo
                    Marshal.GetNativeVariantForObject(currentDropDownComboChoice, vOut);
                }

                else if (newChoice != null)
                {
                    // new value was selected or typed in
                    // see if it is one of our items
                    bool validInput = false;
                    int indexInput = -1;
                    for (indexInput = 0; indexInput < dropDownComboChoices.Length; indexInput++)
                    {
                        if (string.Compare(dropDownComboChoices[indexInput], newChoice, StringComparison.CurrentCultureIgnoreCase) == 0)
                        {
                            validInput = true;
                            break;
                        }
                    }

                    if (validInput)
                    {
                        currentDropDownComboChoice = dropDownComboChoices[indexInput];
                        VsShellUtilities.ShowMessageBox(
                            this.package,
                            currentDropDownComboChoice,
                            "MyDropDownCombo",
                            OLEMSGICON.OLEMSGICON_INFO,
                            OLEMSGBUTTON.OLEMSGBUTTON_OK,
                            OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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

        private void ExecuteList(object sender, EventArgs e)
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
                    Marshal.GetNativeVariantForObject(dropDownComboChoices, vOut);
                }
                else
                {
                    throw (new ArgumentException("No output")); // force an exception to be thrown
                }
            }
        }
    }
}
