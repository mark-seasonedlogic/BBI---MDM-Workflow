using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using BBIHardwareSupport.MDM.UI.Helpers;
using BBIHardwareSupport.MDM.UI.ViewModels.Helpers;
using BBIHardwareSupport.MDM.ViewModels;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;


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

        // Parameterless constructor used by XAML/Frame.Navigate
        public WorkspaceOnePage()
            : this(App.Services.GetRequiredService<WorkspaceOneViewModel>())
        {
        }
        private void EnsureLoginTile()
        {
            if (_viewModel.UITileItems == null) return;

            // Use Tag/Id if you have one; Title works if it's unique/stable
            if (_viewModel.UITileItems.Any(t => t.Title == "Sign in to Workspace ONE"))
                return;

            _viewModel.UITileItems.Insert(0, new UITileItem
            {
                Title = "Sign in to Workspace ONE",
                Description = "Authenticate to enable Workspace ONE actions",
                ImagePath = "ms-appx:///Assets/Login.png",
                ExecuteCommand = new AsyncRelayCommand(async () => await PromptWorkspaceOneLoginAsync())
            });
        }

        public WorkspaceOnePage(WorkspaceOneViewModel viewModel)
        {
            this.InitializeComponent();
            this.DataContext = _viewModel = viewModel;
            _viewModel.LoginRequested = () => PromptWorkspaceOneLoginAsync(refreshAfterLogin: true);

            this.Loaded += WorkspaceOnePage_Loaded;
        }
        public async Task<bool> PromptWorkspaceOneLoginAsync(bool refreshAfterLogin = true)
        {
            var dialog = new WorkspaceOneLoginDialog { XamlRoot = this.XamlRoot };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return false;

            var creds = dialog.EnteredCredentials;

            var stored = await _viewModel.SetCredentialsAsync(creds.Username, creds.Password, creds.ApiKey);
            if (!stored)
            {
                await ShowLoginFailedMessage();
                return false;
            }

            var validated = await _viewModel.ValidateLoginAsync(creds.Username);
            if (!validated)
            {
                await ShowLoginFailedMessage();
                return false;
            }

            // ✅ This is the missing piece: rebuild tiles / init view state
            if (refreshAfterLogin)
            {
                await _viewModel.OnLoadedAsync();   // or a smaller method like BuildTiles(), if you have it
                EnsureLoginTile();                  // reinsert if OnLoadedAsync rebuilds/clears the list
            }

            return true;
        }

        private async Task ShowLoginFailedMessage()
        {
            var msg = new ContentDialog
            {
                Title = "Workspace ONE Login Failed",
                Content = "Credentials were not accepted by Workspace ONE. Please try again.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await msg.ShowAsync();
        }

        private async void WorkspaceOnePage_Loaded(object sender, RoutedEventArgs e)
        {
            // Make sure the login tile is always seen
            EnsureLoginTile();
            if(!_viewModel.IsAuthenticated)
            {// Prompt for login first
                if (!await PromptWorkspaceOneLoginAsync(refreshAfterLogin: false))
                    return;
            }
            ContextMenuHelper.AttachObjectJsonRightClickHandler(WorkspaceOneDeviceListView, this.XamlRoot);

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
