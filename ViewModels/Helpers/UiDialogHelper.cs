// Helpers/UiDialogHelper.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Helpers
{
    public static class UiDialogHelper
    {
        public static async Task<string?> PromptForFolderAsync(XamlRoot xamlRoot)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;

            // Required for WinUI 3 apps
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }

        public static async Task<string?> ShowInputDialogAsync(XamlRoot xamlRoot, string title, string placeholder)
        {
            var inputBox = new TextBox { PlaceholderText = placeholder };
            var dialog = new ContentDialog
            {
                Title = title,
                Content = inputBox,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = xamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? inputBox.Text : null;
        }
        public static async Task<string> PromptForTextAsync(string prompt)
        {
            var inputBox = new TextBox { AcceptsReturn = false };
            var dialog = new ContentDialog
            {
                Title = prompt,
                Content = inputBox,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                XamlRoot = App.MainWindow.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? inputBox.Text : null;
        }
        public static async Task ShowMessageAsync(string message, string title = "Information")
        {
            var textBox = new TextBox
            {

                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                //VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                //HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinHeight = 200,
                MaxHeight = 400
            };
            textBox.Text = message;
            var scrollViewer = new ScrollViewer
            {
                Content = textBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 400
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = scrollViewer,
                PrimaryButtonText = "Close",
                XamlRoot = App.MainWindow.Content.XamlRoot // or your view's XamlRoot
            };

            await dialog.ShowAsync();
        }



    }
}
