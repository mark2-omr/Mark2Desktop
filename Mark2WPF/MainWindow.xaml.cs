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
using NPOI.XSSF.UserModel;

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

        private async void StartButton_Click(object sender, RoutedEventArgs e)
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

        private async void SaveCsv()
        {
            DateTime dateTime = DateTime.Now;
            //var picker = new Windows.Storage.Pickers.FileSavePicker();
            //picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            //picker.FileTypeChoices.Add("Excel File", new List<string>() { ".xlsx" });
            //picker.SuggestedFileName = $"result_{dateTime.ToString("yyyyMMdd_HHmmss")}";
            //Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();

            string outputPath = null;
            var picker = new SaveFileDialog();
            picker.DefaultExt = ".xlsx";
            picker.FileName = $"result_{dateTime.ToString("yyyyMMdd_HHmmss")}";

            if (picker.ShowDialog() == true)
            {
                outputPath = picker.FileName;

            } else
            {
                return;
            }



            byte[] fileBytes = null;
            var workbook = new XSSFWorkbook();
            var worksheet = workbook.CreateSheet("Sheet5");

            var i = 0;
            foreach (var resultRow in survey.resultRows)
            {
                var row = worksheet.CreateRow(i);
                var j = 0;
                foreach (var value in resultRow)
                {
                    var cell = row.CreateCell(j);
                    var cellStyle = workbook.CreateCellStyle();
                    cellStyle.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;

                    if (i > 1 && value.Length == 0)
                    {
                        cellStyle.FillForegroundColor = NPOI.SS.UserModel.IndexedColors.Gold.Index;
                        cell.CellStyle = cellStyle;
                    }
                    else if (j > 1 && value.Contains(";"))
                    {
                        cellStyle.FillForegroundColor = NPOI.SS.UserModel.IndexedColors.Coral.Index;
                        cell.CellStyle = cellStyle;
                    }

                    if (value.Length > 0 && value.All(char.IsDigit))
                    {
                        int v = 0;
                        if (Int32.TryParse(value, out v))
                        {
                            cell.SetCellValue(v);
                        }
                        else
                        {
                            cell.SetCellValue(value);
                        }
                    }
                    else
                    {
                        cell.SetCellValue(value);
                    }

                    j++;
                }
                i++;
            }

            using (var stream = new MemoryStream())
            {
                workbook.Write(stream);
                fileBytes = stream.ToArray();
            }

            if (outputPath != null)
            {
                //await Windows.Storage.FileIO.WriteBytesAsync(file, fileBytes);
                FileStream fs = new FileStream(outputPath, FileMode.Create);
                fs.Write(fileBytes);

                fs.Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveCsv();
        }
    }
}
