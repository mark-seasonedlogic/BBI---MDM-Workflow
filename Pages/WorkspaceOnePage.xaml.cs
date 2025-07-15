using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using BBIHardwareSupport.MDM.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using BBIHardwareSupport.MDM.UI.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WorkspaceOnePage : Page
    {
        private readonly WorkspaceOneViewModel _viewModel;

        public WorkspaceOnePage(WorkspaceOneViewModel viewModel)
        {
            this.InitializeComponent();
            this.DataContext = _viewModel = viewModel;

            this.Loaded += WorkspaceOnePage_Loaded;
        }

        private async void WorkspaceOnePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Prompt for login first
            if (!_viewModel.IsAuthenticated)
            {
                var dialog = new WorkspaceOneLoginDialog
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var creds = dialog.EnteredCredentials;
                   var isSuccess =  await _viewModel.SetCredentialsAsync(creds.Username, creds.Password, creds.ApiKey);
                    
                }
                else
                {
                    // Optionally navigate away or disable the page
                    return;
                }
            }
            // wireup context menu for list view
            ContextMenuHelper.AttachObjectJsonRightClickHandler(WorkspaceOneDeviceListView, this.XamlRoot);
            // Now call non-UI initialization
            await _viewModel.OnLoadedAsync();
        }
        private void WorkspaceOneDeviceListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is ListView listView && e.OriginalSource is FrameworkElement fe)
            {
                if (fe.DataContext is JObject device)
                {
                    var flyout = new MenuFlyout();

                    var viewJsonItem = new MenuFlyoutItem { Text = "View JSON" };
                    viewJsonItem.Click += (s, args) => ShowJsonDialog(device);
                    flyout.Items.Add(viewJsonItem);

                    flyout.ShowAt(fe, e.GetPosition(fe));
                }
            }
        }

        private async void ShowJsonDialog(JObject device)
        {
            var json = JsonConvert.SerializeObject(device, Formatting.Indented);
            var viewer = new JsonViewerWindow(json);
            viewer.Activate();
        }

    }

}
