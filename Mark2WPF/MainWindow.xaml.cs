using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mark2;
using Microsoft.Win32;
using System.IO;

namespace Mark2WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Survey survey;
        
        public MainWindow()
        {
            InitializeComponent();
            survey = new Survey();
        }

        private void ImageFolderButton_Click(object sender, RoutedEventArgs e)
        {
            survey.folderPath = null;
            imageFolderPathTextBlock.Text = "";

            var picker = new OpenFileDialog();
            picker.FileName = "Select Folder";
            picker.CheckFileExists = false;

            if (picker.ShowDialog() == true)
            {
                survey.folderPath = System.IO.Path.GetDirectoryName(picker.FileName);
                imageFolderPathTextBlock.Text = survey.folderPath;
            }
        }
    }
}
