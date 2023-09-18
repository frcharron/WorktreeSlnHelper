using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;

namespace TopLevelMenu
{
    public class SolutionFiles
    {
        public ObservableCollection<SolutionFile> items = new ObservableCollection<SolutionFile>();
        public class SolutionFile
        {
            public string Name { get; set; }
            public string Filename { get { return this.Name + "." + this.FileType; } }
            public string Path { get; set; }
            public string FileType { get; set; }
            public bool? IsCanBuild { get; set; }
            public bool? IsCanRun { get; set; }
            public override string ToString() { if (this.Name != null) return System.IO.Path.GetFileNameWithoutExtension(this.Name); else return ""; }
        }

        public void LoadFiles()
        {
            items.Add(new SolutionFile()
            {
                Name = "Evil Solution",
                Path = "C://SecretFiles//Nothing...",
                FileType = "SLN",
                IsCanBuild = true,
                IsCanRun = false
            }
            );
            items.Add(new SolutionFile()
            {
                Name = "Most Evil Solution",
                Path = "C://SecretFiles//Nothing...",
                FileType = "SLN",
                IsCanBuild = true,
                IsCanRun = false
            }
            );
            items.Add(new SolutionFile()
            {
                Name = "Good Solution",
                Path = "C://SecretFiles//Nothing...",
                FileType = "SLN",
                IsCanBuild = true,
                IsCanRun = false
            }
            );
        }
    }
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

        public SolutionFiles.SolutionFile GetSelectedSolution() => dialogOpenSolution1.File;
    }
}
