using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using RepositorySolutionScanner;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Navigation;
using System.Windows.Resources;

namespace Waap
{
    public partial class OpenSession : Form
    {
        public OpenSession(string worktreeDescription, SolutionsInstance.Solution[] solution)
        {
            InitializeComponent();
            dialogOpenSolution1.form = this;
            var files = new SolutionFiles();
            files.LoadFiles(solution);
            dialogOpenSolution1.ShowItems(files.items);
            dialogOpenSolution1.WorktreeName.Text = worktreeDescription;
        }

        public SolutionFiles.SolutionFile GetSelectedSolution() => dialogOpenSolution1.File;
    }
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
            public System.Drawing.Image Image { get; set; }
            public override string ToString() { if (this.Name != null) return System.IO.Path.GetFileNameWithoutExtension(this.Name); else return ""; }
        }

        public void LoadFiles(SolutionsInstance.Solution[] solutions)
        {
            Assembly _assembly = Assembly.GetExecutingAssembly();

            Stream _imageStream = _assembly.GetManifestResourceStream("Waap.Resources.sln.png");
            var img = System.Drawing.Image.FromStream(_imageStream);
            foreach (var solution in solutions)
            {
                items.Add(new SolutionFile()
                {
                    Name = solution.Name,
                    Path = solution.Path,
                    FileType = ".sln",
                    IsCanBuild = false,
                    IsCanRun = false,
                    Image = img
            });
            }
        }
    }
}
