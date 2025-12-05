// Helpers/UiDialogHelper.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

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
 public static async Task<string?> PromptForFileAsync(
            XamlRoot xamlRoot,
            string? fileTypePattern = "*",
            string? dialogTitle = "Choose a File")
        {
            // 1) Make sure we have a valid window
            if (App.MainWindow is null)
                throw new InvalidOperationException("MainWindow is not initialized.");

            var window = App.MainWindow;

            // 2) Show a simple ContentDialog for context (optional)
            var dialog = new ContentDialog
            {
                Title = dialogTitle,
                Content = "Click OK to choose the file.",
                PrimaryButtonText = "OK",
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();

            // 3) Create and initialize picker with HWND
            var picker = new FileOpenPicker();

            IntPtr hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // 4) Configure file type filter safely
            picker.FileTypeFilter.Clear();

            if (string.IsNullOrWhiteSpace(fileTypePattern) || fileTypePattern == "*")
            {
                picker.FileTypeFilter.Add("*");
            }
            else
            {
                // FileOpenPicker requires entries like ".csv" or "*"
                var ft = fileTypePattern.Trim();
                if (!ft.StartsWith(".") && ft != "*")
                    ft = "." + ft;

                picker.FileTypeFilter.Add(ft);
            }

            // 5) Show picker
            var file = await picker.PickSingleFileAsync();
            return file?.Path;
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
