using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace TopLevelMenu
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TestCommand
    {
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
        /// Initializes a new instance of the <see cref="TestCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private TestCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);

            CommandID subCommandID = new CommandID(CommandSet, cmdidTestSubCmd);
            MenuCommand subItem = new MenuCommand(new EventHandler(SubItemCallback), subCommandID);
            commandService.AddCommand(subItem);

            this.InitMRUMenu(commandService);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TestCommand Instance
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
            // Switch to the main thread - the call to AddCommand in TestCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new TestCommand(package, commandService);
        }

        public static DialogResult ShowMe(string title, string promptText, ref string value)
        {
            //'value' is to return Some value
            Form form = new Form();
            //Label label = new Label();
            //ListView lv = new ListView();

            //form.Text = title;
            //label.Text = promptText;

            //label.SetBounds(9, 20, 600, 24);
            //label.ForeColor = Color.White;
            //label.Font = new Font("Calibri Light", 24, FontStyle.Regular);
            //label.BackColor = System.Drawing.Color.Transparent;
            //lv.SetBounds(9, 100, 600, 300);
            //lv.BackColor = System.Drawing.Color.DarkGray;

            //lv.Items.Add("bob");

            //label.AutoSize = true;

            //form.ClientSize = new Size(800, 451);
            //form.Controls.AddRange(new Control[] { label, lv });
            //form.FormBorderStyle = FormBorderStyle.FixedDialog;
            //form.StartPosition = FormStartPosition.CenterScreen;
            //form.MinimizeBox = false;
            //form.MaximizeBox = false;
            //var assembly = Assembly.GetExecutingAssembly();
            //var resourceName = assembly.GetManifestResourceNames().Single(x => x.Contains("wp6734564.png"));
            //using (var stream = assembly.GetManifestResourceStream(resourceName))
            //{
            //    if (stream != null)
            //    {
            //        form.BackgroundImage = Image.FromStream(stream);
            //        form.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            //    }
            //}

            DialogResult dialogResult = form.ShowDialog();
            return dialogResult;
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
            string title = "TestCommand";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void SubItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsUIShell uiShell = this.package.GetService<SVsUIShell, IVsUIShell>();
            Guid clsid = Guid.Empty;
            int result;
            uiShell.ShowMessageBox(
                0,
                ref clsid,
                "TestCommand",
                string.Format(CultureInfo.CurrentCulture,
                "Inside TestCommand.SubItemCallback()",
                this.ToString()),
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0,
                out result);
        }

        private int numMRUItems = 4;
        private int baseMRUID = (int)TopLevelMenuPackage.cmdidMRUList;
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

        private void InitMRUMenu(OleMenuCommandService mcs)
        {
            InitializeMRUList();
            for (int i = 0; i < this.numMRUItems; i++)
            {
                var cmdID = new CommandID(CommandSet, this.baseMRUID + i);
                var mc = new OleMenuCommand(
                    new EventHandler(OnMRUExec), cmdID);
                mc.BeforeQueryStatus += new EventHandler(OnMRUQueryStatus);
                mcs.AddCommand(mc);
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
            }
        }

        private void OnMRUExec(object sender, EventArgs e)
        {
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
                    string value = string.Empty;
                    Form form = new OpenSession("master", "C:\\git\\Genetec.Softwire_master");
                    form.ShowDialog();
                }
            }
        }
    }
}
