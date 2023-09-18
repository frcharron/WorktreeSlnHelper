using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace WorktreeMenu
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CommandWorktreeList
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0200;
        public const uint cmdidMRUList = 0x200;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public const string guidTestCommandPackageCmdSet = "256e5347-7680-497b-a9f3-74b15dc81a42";
        public static readonly Guid CommandSet = new Guid(guidTestCommandPackageCmdSet);

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        private int numMRUItems = 4;
        private ArrayList mruList;

        private void InitializeMRUList()
        {
            if (null == this.mruList)
            {
                this.mruList = new ArrayList();
                if (null != this.mruList)
                {
                    for (int i = 0; i < this.numMRUItems; i++)
                    {
                        this.mruList.Add(string.Format(CultureInfo.CurrentCulture,
                            "Item {0}", i + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandWorktreeList"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CommandWorktreeList(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            InitializeMRUList();
            for (int i = 0; i < this.numMRUItems; i++)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId + i);
                var menuItem = new OleMenuCommand(new EventHandler(OnMRUExec), menuCommandID);
                menuItem.BeforeQueryStatus += new EventHandler(OnMRUQueryStatus);
                commandService.AddCommand(menuItem);
            }
        }

        private void OnMRUQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                int MRUItemIndex = menuCommand.CommandID.ID - CommandId;
                if (MRUItemIndex >= 0 && MRUItemIndex < this.mruList.Count)
                {
                    menuCommand.Text = this.mruList[MRUItemIndex] as string;
                }
            }
        }

        private void OnMRUExec(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (null != menuCommand)
            {
                int MRUItemIndex = menuCommand.CommandID.ID - CommandId;
                if (MRUItemIndex >= 0 && MRUItemIndex < this.mruList.Count)
                {
                    string selection = this.mruList[MRUItemIndex] as string;
                    for (int i = MRUItemIndex; i > 0; i--)
                    {
                        this.mruList[i] = this.mruList[i - 1];
                    }
                    this.mruList[0] = selection;
                    System.Windows.Forms.MessageBox.Show(
                        string.Format(CultureInfo.CurrentCulture,
                                      "Selected {0}", selection));
                }
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static CommandWorktreeList Instance
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
            // Switch to the main thread - the call to AddCommand in cmdidMRUList's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CommandWorktreeList(package, commandService);
        }

        public static async Task Initialize(Package package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new CommandWorktreeList(package, commandService);
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
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "cmdidMRUList";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
