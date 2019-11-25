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

namespace Mark2
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class ProgressPage : Windows.UI.Xaml.Controls.Page
    {
        public Windows.UI.WindowManagement.AppWindow appWindow { get; set; }
        public Survey survey { get; set; }
        public ProgressPage()
        {
            this.InitializeComponent();
        }

        public void setProgress(double value)
        {

            this.progressBar.Value = value;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            survey.StopRecognize = true;
        }
    }
}
