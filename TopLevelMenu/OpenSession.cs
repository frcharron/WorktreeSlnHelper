using RepositorySolutionScanner;
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

        public void LoadFiles(SolutionsInstance.Solution[] solutions)
        {
            foreach (var solution in solutions)
            {
                items.Add(new SolutionFile()
                {
                    Name = solution.Name,
                    Path = solution.Path,
                    FileType = ".sln",
                    IsCanBuild = false,
                    IsCanRun = false
                });
            }
        }
    }
    public partial class OpenSession : Form
    {
        public OpenSession(string worktreeDescription, SolutionsInstance.Solution[] solution)
        {
            InitializeComponent();
            dialogOpenSolution1.form = this;
            var files = new SolutionFiles();
            files.LoadFiles(solution);
            dialogOpenSolution1.ShowItems(files.items);
            dialogOpenSolution1.WorktreeName.Content = worktreeDescription;
        }

        public SolutionFiles.SolutionFile GetSelectedSolution() => dialogOpenSolution1.File;
    }
}
