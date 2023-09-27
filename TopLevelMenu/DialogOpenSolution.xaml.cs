using EnvDTE;
using Microsoft.ServiceHub.Resources;
using Microsoft.VisualStudio.PlatformUI;
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
    /// <summary>
    /// Interaction logic for DialogOpenSolution.xaml
    /// </summary>
    public partial class DialogOpenSolution : System.Windows.Controls.UserControl
    {
        private SolutionFile file;
        public Form form = null;
        public SolutionFile File {
            get {return this.file;} 
        }
        public DialogOpenSolution()
        {
            InitializeComponent();
            this.Background = ColorHelper.ToWpfBrush(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey));
            this.Foreground = ColorHelper.ToWpfBrush(VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowTextColorKey));
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

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            file = SolutionList.SelectedItem as SolutionFile;
            form.Close();
        }
    }
}
