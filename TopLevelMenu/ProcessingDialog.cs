using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TopLevelMenu
{
    public partial class ProcessingDialog : Form
    {
        public ProcessingDialog()
        {
            InitializeComponent();
            this.BackColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            this.ForeColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey);
        }

        public void SetMessage(string message) { 
            lblMessage.Text = message;
        }

        public void CloseDialog() {
            this.Invoke(new MethodInvoker(this.Close));
        }

        public void StartDialog(string message) {
            var t = new Task(() => {
                SetMessage(message);
                ShowDialog();
            }
            );
            t.Start();
        }
    }
}
