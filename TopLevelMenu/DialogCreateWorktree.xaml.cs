using RepositorySolutionScanner;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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


namespace TopLevelMenu
{
    /// <summary>
    /// Interaction logic for DialogCreateWorktree.xaml
    /// </summary>
    public partial class DialogCreateWorktree : System.Windows.Controls.UserControl
    {
        public ArrayList repositories;
        public DialogCreateWorktree()
        {
            InitializeComponent(); 
            repositories = new ArrayList();
        }

        private void LocalRepositoryDirectories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (RepositoryInstance.Repository repository in repositories)
            {
                if (repository.RepositoryPath.Equals((sender as System.Windows.Controls.ComboBox).SelectedItem))
                {
                    this.LocalRepositoryBranch.ItemsSource = (repository.GitAttributs.ListBranch().Split(' ')).Where(x => !string.IsNullOrEmpty(x) && !x.Contains("->")).Distinct().ToArray();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GitHelper.GitDirectory.CreateWorktree(BranchPrefix.Text, (string)LocalRepositoryDirectories.SelectedItem, (string)LocalRepositoryBranch.SelectedItem, BranchName.Text);
        }
    }
    public class LengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;

            if (text != null)
            {
                return text.Length > 0;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
