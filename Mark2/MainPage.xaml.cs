using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;

namespace Mark2
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        Survey survey;
        string resultCSV = null;

        public MainPage()
        {
            InitializeComponent();
            survey = new Survey();
            startButton.IsEnabled = false;
            saveButton.IsEnabled = false;

            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchViewSize = new Size(500, 320);
            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchWindowingMode = 
                Windows.UI.ViewManagement.ApplicationViewWindowingMode.PreferredLaunchViewSize;
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 320));
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            survey.folder = null;
            folderPathTextBlock.Text = "";

            try
            {
                var picker = new Windows.Storage.Pickers.FolderPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    folderPathTextBlock.Text = folder.Path;
                    survey.folder = folder;
                }
            }
            catch (Exception exception)
            {
                folderPathTextBlock.Text = "";
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Error"),
                    Content = resourceLoader.GetString("OpenFolderError") + "\n" + exception.ToString(),
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();
            }

            /*
            if (survey.items.Count == 0)
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Error"),
                    Content = resourceLoader.GetString("FolderNoImageError"),
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();

                survey.folder = null;
                folderPathTextBlock.Text = "";
            }
            */

            if (survey.folder != null && survey.csv != null)
            {
                startButton.IsEnabled = true;
            }
            else
            {
                startButton.IsEnabled = false;
            }
        }

        private async void OpenCsvButton_Click(object sender, RoutedEventArgs e)
        {
            survey.csv = null;
            csvPathTextBlock.Text = "";

            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add(".csv");
                var csv = await picker.PickSingleFileAsync();
                if (csv != null)
                {
                    survey.csv = csv;
                    await survey.SetupPositions();
                    csvPathTextBlock.Text = csv.Path;
                }
            }
            catch
            {
                var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("Error"),
                    Content = resourceLoader.GetString("OpenCsvError"),
                    CloseButtonText = "OK"
                };
                await errorDialog.ShowAsync();
            }

            if (survey.folder != null && survey.csv != null)
            {
                startButton.IsEnabled = true;
            }
            else
            {
                startButton.IsEnabled = false;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            survey.areaThreshold = areaThresholdSlider.Value / 100.0;
            survey.colorThreshold = colorThresholdSlider.Value / 100.0;

            if (survey.folder == null || survey.csv == null)
            {
                return;
            }

            await survey.SetupOutputFolders();

            Task taskMain = new Task(async () =>
            {
                // startButton.IsEnabled = false;
                System.Diagnostics.Debug.WriteLine("Recognizing");

                await survey.Recognize(async (i, max) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        progressBar.Value = (100.0 / (double)(max)) * (i + 1);
                    });
                });

                System.Diagnostics.Debug.WriteLine("Finished");

                resultCSV = survey.resultBuffer;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (resultCSV != null)
                    {
                        startButton.IsEnabled = true;
                        saveButton.IsEnabled = true;
                    }
                });
            });
            taskMain.Start();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("CSV File", new List<string>() { ".csv" });
            picker.SuggestedFileName = "result";
            Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                await Windows.Storage.FileIO.WriteTextAsync(file, this.resultCSV);
            }
        }
    }
}
