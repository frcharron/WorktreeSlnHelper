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
    public partial class CreateWorktree : Form
    {
        public CreateWorktree()
        {
            InitializeComponent();
        }

        public void AddLocalRepositoryDirectories (string path) => dialogCreateWorktree1.LocalRepositoryDirectories.Items.Add (path);
        public void CleanLocalRepositoryDirectories() => dialogCreateWorktree1.LocalRepositoryDirectories.Items.Clear();
    }
}
