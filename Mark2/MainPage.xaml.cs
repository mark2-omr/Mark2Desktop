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

namespace Mark2
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        IReadOnlyList<Windows.Storage.StorageFile> fileList;
        Windows.Storage.StorageFolder folder;
        Windows.Storage.StorageFile csv;

        String folderToken;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                // folderToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(folder);
                folderPathTextBlock.Text = folder.Path;
            }
        }

        private async void OpenCsvButton_Click(object sender, RoutedEventArgs e)
        {
            Windows.Storage.Pickers.FileOpenPicker picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".csv");
            csv = await picker.PickSingleFileAsync();
            if (csv != null)
            {
                csvPathTextBlock.Text = csv.Path;
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (folder != null && csv != null)
            {
                Survey survey = new Survey(folder, csv);
                await Task.Run(() => survey.SetupItems());
                await Task.Run(() => survey.SetupPositions());
            }
        }
    }
}
