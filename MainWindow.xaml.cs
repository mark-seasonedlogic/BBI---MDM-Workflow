using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.UI.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using System;
using Microsoft.Extensions.DependencyInjection;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using BBIHardwareSupport.MDM.IntuneConfigManager.Pages;

namespace BBIHardwareSupport.MDM.IntuneConfigManager
{
    public sealed partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();

            // Use the DI container to resolve MainViewModel
            ViewModel = App.Services.GetRequiredService<MainViewModel>();

            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }
            _serviceProvider = App.Services;
            // Set initial page
            ContentFrame.Navigate(typeof(GitManagerPage));
        }

        private async void ShowHelp(object sender, RoutedEventArgs e)
        {
            var messageDialog = new ContentDialog
            {
                Title = "Help",
                Content = "This application allows you to manage Git repositories and Intune configurations.",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot // Ensure correct window context
            };

            await messageDialog.ShowAsync();
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                switch (selectedItem.Tag)
                {
                    case "GitManager":
                        ContentFrame.Navigate(typeof(GitManagerPage));
                        break;
                    case "IntuneArtifacts":
                        ContentFrame.Navigate(typeof(IntuneArtifactsPage));
                        break;
                    case "Settings":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                    case "WS1AndroidBatteryPage":
                        ContentFrame.Navigate(typeof(WS1AndroidBatteryPage));
                        break;
                    case "IntuneGroupsPage":
                        var groupPage = _serviceProvider.GetRequiredService<IntuneGroupsPage>();
                        ContentFrame.Content = groupPage;
                        break;
                    case "DeviceConfigurationsPage":
                        var page = _serviceProvider.GetRequiredService<OemConfigurationManagerPage>();
                        ContentFrame.Content = page;
                        break;
                    case "SchemaExtensionAdminPage":
                        var adminPage = _serviceProvider.GetRequiredService <SchemaExtensionAdminPage>();
                        ContentFrame.Content = adminPage;
                        break;
                    case "WorkspaceOnePage":
                        var ws1Page = _serviceProvider.GetRequiredService<WorkspaceOnePage>();
                        ContentFrame.Content = ws1Page;
                        break;
                    case "WS1DeviceManagementPage":
                        var ws1DevicePage = _serviceProvider.GetRequiredService<WS1DeviceManagementPage>();
                        ContentFrame.Content = ws1DevicePage;
                        break;
                    case "WS1SmartGroupsPage":
                        var ws1SmartGroupsPage = _serviceProvider.GetRequiredService<WS1SmartGroupsPage>();
                        ContentFrame.Content = ws1SmartGroupsPage;
                        break;
                    case "WS1ProfilesPage":
                        var ws1ProfilesPage = _serviceProvider.GetRequiredService<WS1ProfilesPage>();
                        ContentFrame.Content = ws1ProfilesPage;
                        break;
                    case "WS1ApplicationsPage":
                        var ws1AppsPage = _serviceProvider.GetRequiredService<WS1ApplicationsPage>();
                        ContentFrame.Content = ws1AppsPage;
                        break;
                    case "WS1CompliancePoliciesPage":
                        var ws1CompliancePoliciesPage = _serviceProvider.GetRequiredService<WS1CompliancePoliciesPage>();
                        ContentFrame.Content = ws1CompliancePoliciesPage;
                        break;


                }
            }
        }
    }
}
