using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using BBIHardwareSupport.MDM.IntuneConfigManager;

namespace BBIHardwareSupport.MDM.UI.Helpers
{
    public static class ContextMenuHelper
    {
        public static void AttachJsonRightClickHandler(ListView listView, XamlRoot xamlRoot)
        {
            if (listView == null || xamlRoot == null)
                throw new ArgumentNullException();

            listView.RightTapped += (sender, e) =>
            {
                if (e.OriginalSource is FrameworkElement fe && fe.DataContext is object dataContext)
                {
                    var flyout = new MenuFlyout();
                    var viewJsonItem = new MenuFlyoutItem { Text = "View JSON" };

                    viewJsonItem.Click += (s, args) =>
                    {
                        string json = SerializeContextToJson(dataContext);
                        ShowJsonDialog(json);
                    };

                    flyout.Items.Add(viewJsonItem);
                    flyout.ShowAt(fe, e.GetPosition(fe));
                }
            };
        }
        public static void AttachObjectJsonRightClickHandler(ListView listView, XamlRoot xamlRoot)
        {
            listView.RightTapped += (s, e) =>
            {
                if (e.OriginalSource is FrameworkElement fe && fe.DataContext is not null)
                {
                    var obj = fe.DataContext;

                    var flyout = new MenuFlyout();
                    var viewJson = new MenuFlyoutItem { Text = "View JSON" };
                    viewJson.Click += async (sender, args) =>
                    {
                        var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                        var jsonViewer = new JsonViewerWindow(json);
                        jsonViewer.Activate();
                    };

                    flyout.Items.Add(viewJson);
                    flyout.ShowAt(fe, e.GetPosition(fe));
                }
            };
        }

        private static void ShowJsonDialog(string json)
        {
            var jsonViewer = new JsonViewerWindow(json);
            jsonViewer.Activate(); // Opens the new window with JSON displayed
        }

        private static void ShowJsonDialog(string json, XamlRoot xamlRoot)
        {
            var dialog = new ContentDialog
            {
                Title = "View JSON",
                Content = new ScrollViewer
                {
                    Content = new TextBox
                    {
                        Text = json,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        FontSize = 12,
                        IsReadOnly = true,
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true
                    },
                    MaxHeight = 500
                },
                CloseButtonText = "Close",
                XamlRoot = xamlRoot
            };

            _ = dialog.ShowAsync();
        }
        private static string SerializeContextToJson(object dataContext)
        {
            if (dataContext is JObject jObject)
            {
                return jObject.ToString(Formatting.Indented);
            }

            return JsonConvert.SerializeObject(dataContext, Formatting.Indented);
        }
    }
}