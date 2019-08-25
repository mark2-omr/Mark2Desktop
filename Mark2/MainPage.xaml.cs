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
using System.Threading.Tasks;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

using System.Threading;

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
            saveButton.IsEnabled = false;

            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchViewSize = new Size(500, 320);
            Windows.UI.ViewManagement.ApplicationView.PreferredLaunchWindowingMode = 
                Windows.UI.ViewManagement.ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                survey.folder = folder;
                survey.SetupItems();

                // folderToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                folderPathTextBlock.Text = folder.Path;
            }
        }

        private async void OpenCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");
            var csv = await picker.PickSingleFileAsync();
            if (csv != null)
            {
                survey.csv = csv;
                survey.SetupPositions();

                csvPathTextBlock.Text = csv.Path;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            survey.threshold = thresholdSlider.Value / 100.0;

            Windows.UI.WindowManagement.AppWindow appWindow = await Windows.UI.WindowManagement.AppWindow.TryCreateAsync();

            Frame appWindowFrame = new Frame();
            appWindowFrame.Navigate(typeof(ProgressPage));
            Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowFrame);

            ProgressPage progressPage = (ProgressPage)appWindowFrame.Content;
            progressPage.appWindow = appWindow;
            progressPage.survey = survey;

            if (survey.folder != null && survey.csv != null)
            {
                appWindow.RequestSize(new Size(800, 300));
                await appWindow.TryShowAsync();

                System.Diagnostics.Debug.WriteLine("Recognize");

                Task taskMain = new Task(async () =>
                {
                    System.Diagnostics.Debug.WriteLine("Recognizing");

                    await survey.Recognize(async(i, max) =>
                    {
                        await Task.Run(async () =>
                        {
                            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                progressPage.setProgress((100.0 / (double)(max)) * (i + 1));
                            });
                        });
                    });

                    System.Diagnostics.Debug.WriteLine("finished");
                    resultCSV = survey.resultBuffer;

                    await Task.Run(async () =>
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            if (resultCSV != null)
                            {
                                saveButton.IsEnabled = true;
                            }
                            await appWindow.CloseAsync();
                        });
                    });

                });

                taskMain.Start();
            }
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
