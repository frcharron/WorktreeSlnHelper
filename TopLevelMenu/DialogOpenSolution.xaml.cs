using EnvDTE;
using Microsoft.ServiceHub.Resources;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static TopLevelMenu.SolutionFiles;
using Image = System.Drawing.Image;

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
    /// <summary>
    /// Interaction logic for DialogOpenSolution.xaml
    /// </summary>
    public partial class DialogOpenSolution : System.Windows.Controls.UserControl
    {
        public Form form = null;
        public DialogOpenSolution()
        {
            InitializeComponent();
        }

        public void ShowItems(ObservableCollection<SolutionFile> items) => SolutionList.ItemsSource = items;

        private void ListViewItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //var item = sender as ListViewItem;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            form.Close();
        }
    }
}
