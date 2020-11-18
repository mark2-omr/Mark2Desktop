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

        private async void OpenCsvButton_Click(object sender, RoutedEventArgs e)
        {
            survey.csvPath = null;
            csvPathTextBlock.Text = "";

            var picker = new OpenFileDialog();

            try
            {
                if (picker.ShowDialog() == true)
                {
                    survey.csvPath = picker.FileName;
                    await survey.SetupPositions();
                    csvPathTextBlock.Text = survey.csvPath;
                }
            } catch
            {
                MessageBox.Show("Error");
            }
            
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            survey.areaThreshold = areaThresholdSlider.Value / 100.0;
            survey.colorThreshold = colorThresholdSlider.Value / 100.0;

            if (survey.folderPath == null || survey.csvPath == null)
            {
                return;
            }

            await survey.SetupOutputFolders();
            startButton.IsEnabled = false;

            Task taskMain = new Task(async () =>
            {
                System.Diagnostics.Debug.WriteLine("Recognizing");

                await survey.Recognize( (i, max) =>
                {
                     Dispatcher.Invoke(() => {
                        progressBar.Value = (100.0 / (double)(max)) * (i + 1);
                     });
                });

                System.Diagnostics.Debug.WriteLine("Finished");

                Dispatcher.Invoke(() => {
                    startButton.IsEnabled = true;
                    saveButton.IsEnabled = true;
                });

                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                //{
                //    // if (resultCSV != null)
                //    {
                //        startButton.IsEnabled = true;
                //        saveButton.IsEnabled = true;
                //        SaveCsv();
                //    }
                //});
            });
            taskMain.Start();
        }
    }
}
