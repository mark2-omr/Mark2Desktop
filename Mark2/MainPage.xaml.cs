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

using Microsoft.Advertising.WinRT.UI;

namespace Mark2
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        Survey survey;
        string resultCSV = null;

        InterstitialAd interstitialAd = null;
        string appId = "9nrjc7500p6m";
        string adUnitId = "1100063063";

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

            interstitialAd = new InterstitialAd();
            interstitialAd.AdReady += InterstitialAd_AdReady;
            interstitialAd.ErrorOccurred += InterstitialAd_ErrorOccurred;
            interstitialAd.Completed += InterstitialAd_Completed;
            interstitialAd.Cancelled += InterstitialAd_Cancelled;
            interstitialAd.RequestAd(AdType.Video, appId, adUnitId);
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
            if (InterstitialAdState.Ready == interstitialAd.State)
            {
                interstitialAd.Show();
            }
            else
            {
                interstitialAd.RequestAd(AdType.Video, appId, adUnitId);
            }

            survey.threshold = thresholdSlider.Value / 100.0;

            Windows.UI.WindowManagement.AppWindow appWindow = await Windows.UI.WindowManagement.AppWindow.TryCreateAsync();

            Frame appWindowFrame = new Frame();
            appWindowFrame.Navigate(typeof(ProgressPage));
            Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowFrame);

            ProgressPage progressPage = (ProgressPage)appWindowFrame.Content;
            progressPage.appWindow = appWindow;
            progressPage.survey = survey;

            if (survey.folder == null || survey.csv == null)
            {
                return;
            }

            await survey.SetupOutputFolders();

            appWindow.RequestSize(new Size(400, 100));
            await appWindow.TryShowAsync();

            Task taskMain = new Task(async () =>
            {
                System.Diagnostics.Debug.WriteLine("Recognizing");

                await survey.Recognize(async (i, max) =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        progressPage.setProgress((100.0 / (double)(max)) * (i + 1));
                    });  
                });

                System.Diagnostics.Debug.WriteLine("Finished");

                resultCSV = survey.resultBuffer;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    if (resultCSV != null)
                    {
                        saveButton.IsEnabled = true;
                    }
                    await appWindow.CloseAsync();
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

        void InterstitialAd_AdReady(object sender, object e)
        {
            // Your code goes here.
        }

        void InterstitialAd_ErrorOccurred(object sender, AdErrorEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e);
        }

        void InterstitialAd_Completed(object sender, object e)
        {
            System.Diagnostics.Debug.WriteLine("Request Ad Complete");
            interstitialAd.RequestAd(AdType.Video, appId, adUnitId);
        }

        void InterstitialAd_Cancelled(object sender, object e)
        {
            System.Diagnostics.Debug.WriteLine("Request Ad Cancelled");
            interstitialAd.RequestAd(AdType.Video, appId, adUnitId);
        }
    }
}
