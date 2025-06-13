using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System;
using System.IO;
namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class WS1AndroidBatteryPage : Page
    {
        public WS1BatteryDrainViewModel ViewModel { get; set; }

        public WS1AndroidBatteryPage()
        {
            this.InitializeComponent();

            ViewModel = new WS1BatteryDrainViewModel();
            DataContext = ViewModel;

            // REMOVE unsupported features for RC5.3
            BatteryChart.TooltipPosition = TooltipPosition.Top;
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PreviousPage();
            BatteryChart.Series = ViewModel.Series;
            BatteryChart.XAxes = ViewModel.XAxes;
            BatteryChart.YAxes = ViewModel.YAxes;
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NextPage();
            BatteryChart.Series = ViewModel.Series;
            BatteryChart.XAxes = ViewModel.XAxes;
            BatteryChart.YAxes = ViewModel.YAxes;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string folderPath = await PromptForFolderAsync();
            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                await ViewModel.LoadBatteryFilesAsync(folderPath);

                BatteryChart.Series = ViewModel.Series;
                BatteryChart.XAxes = ViewModel.XAxes;
                BatteryChart.YAxes = ViewModel.YAxes;
                //BatteryChart.TooltipTextFormatter = ViewModel.TooltipFormatter;

            }


        }




        public async Task<string> PromptForFolderAsync()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        // Required to show the dialog in WinUI 3
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

}
}
