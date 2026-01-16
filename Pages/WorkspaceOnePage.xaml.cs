using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using BBIHardwareSupport.MDM.UI.Helpers;
using BBIHardwareSupport.MDM.UI.ViewModels.Helpers;
using BBIHardwareSupport.MDM.ViewModels;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services;
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
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Profile;


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
        private readonly IWorkspaceOneProfileExportService _profileExportService;
        // Parameterless constructor used by XAML/Frame.Navigate
        public WorkspaceOnePage()
            : this(App.Services.GetRequiredService<WorkspaceOneViewModel>())
        {
            _profileExportService = App.Services.GetRequiredService<IWorkspaceOneProfileExportService>();
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
        private async Task ShowMessage(string title, string content, string closeButtonText = "OK")
        {
            var msg = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText,
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
            //ContextMenuHelper.AttachObjectJsonRightClickHandler(WorkspaceOneDeviceListView, this.XamlRoot);

            await _viewModel.OnLoadedAsync();
        }
        private void WorkspaceOneDeviceListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is not ListView listView) return;
            if (e.OriginalSource is not DependencyObject original) return;

            // 1) Try to find the ListViewItem container (best)
            var container = FindAncestor<ListViewItem>(original);

            // 2) Determine the item (DataContext) using multiple fallbacks
            object? item = container?.Content;

            // Some templates don't set Content the way you expect; try DataContext
            item ??= container?.DataContext;

            // Fallback: use the DataContext of the original source if it's a FrameworkElement
            if (item is null && e.OriginalSource is FrameworkElement fe)
                item = fe.DataContext;

            if (item is null)
                return; // right-click wasn't on an item

            var flyout = new MenuFlyout();


            // Always keep existing functionality: View JSON for any type
            var viewJsonItem = new MenuFlyoutItem { Text = "View JSON" };
            viewJsonItem.Click += (_, __) =>
            {
                var json = JsonConvert.SerializeObject(item, Formatting.Indented);
                var viewer = new JsonViewerWindow(json);
                viewer.Activate();
            };
            flyout.Items.Add(viewJsonItem);

            // Only add this extra option when the clicked item is a ProfileSummary
            if (item is WorkspaceOneProfileSummary profile)
            {
                flyout.Items.Add(new MenuFlyoutSeparator());

                var viewDetailsJsonItem = new MenuFlyoutItem { Text = "View Payload Details JSON" };
                viewDetailsJsonItem.Click += async (_, __) =>
                {
                    try
                    {
                        var export = await _profileExportService.GetExportAsync(profile, ct: CancellationToken.None);
                        ShowJsonDialog(JObject.FromObject(export.PayloadDetails));
                    }
                    catch (Exception ex)
                    {
                        ShowMessage("Error", ex.Message, "OK");
                    }
                };

                flyout.Items.Add(viewDetailsJsonItem);
            }

            flyout.ShowAt(container, e.GetPosition(container));
        }

        private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            DependencyObject? current = start;
            while (current is not null)
            {
                if (current is T typed) return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private async void ShowJsonDialog(JObject device)
        {
            var json = JsonConvert.SerializeObject(device, Formatting.Indented);
            var viewer = new JsonViewerWindow(json);
            viewer.Activate();
        }

    }

}
