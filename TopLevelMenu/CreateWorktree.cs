using Microsoft.VisualStudio.PlatformUI;
using RepositorySolutionScanner;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Waap
{
    public partial class CreateWorktree : Form
    {
        public CreateWorktree()
        {
            InitializeComponent();
            dialogCreateWorktree1.form = this;
        }

        public void AddLocalRepositoryDirectories(RepositoryInstance.Repository repo) { 
            dialogCreateWorktree1.LocalRepositoryDirectories.Items.Add(repo.RepositoryPath);
            dialogCreateWorktree1.repositories.Add(repo);
        }
        public void CleaRLocalRepositoryDirectories() => dialogCreateWorktree1.LocalRepositoryDirectories.Items.Clear();
        public void AddLocalRepositoryBranches(string branch) => dialogCreateWorktree1.LocalRepositoryBranch.Items.Add(branch);
        public void ClearLocalRepositoryBranches() => dialogCreateWorktree1.LocalRepositoryBranch.Items.Clear();
    }
}
