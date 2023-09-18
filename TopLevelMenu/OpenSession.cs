using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

namespace TopLevelMenu
{
    public partial class OpenSession : Form
    {
        private string m_LocalRepositoryPath;
        public OpenSession(string worktreeDescription, string localRepositoryPath)
        {
            m_LocalRepositoryPath = localRepositoryPath;
            InitializeComponent();
            dialogOpenSolution1.form = this;
            var files = new SolutionFiles();
            files.LoadFiles();
            dialogOpenSolution1.ShowItems(files.items);
            dialogOpenSolution1.WorktreeName.Content = worktreeDescription;
        }
    }
}
