using BBIHardwareSupport.MDM.IntuneConfigManager.Pages;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace BBIHardwareSupport.MDM.IntuneConfigManager
{
    public sealed partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainViewModel ViewModel { get; }
        public bool IsLoading { get; set; }
        public string StatusMessage { get; set; } = string.Empty;

        public MainWindow()
        {
            this.InitializeComponent();

            if (ContentFrame is null)
            {
                throw new InvalidOperationException(
                    "ContentFrame is null. Check MainWindow.xaml: <Frame x:Name=\"ContentFrame\" /> " +
                    "and make sure it is not inside a DataTemplate and the x:Name matches exactly.");
            }

            // Use the DI container to resolve MainViewModel
            ViewModel = App.Services.GetRequiredService<MainViewModel>();
            RootGrid.DataContext = ViewModel;
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.IsLoading))
                {
                    Debug.WriteLine($"[FOOTER DEBUG] IsLoading changed → {ViewModel.IsLoading}");
                }

                if (e.PropertyName == nameof(ViewModel.StatusMessage))
                {
                    Debug.WriteLine($"[FOOTER DEBUG] StatusMessage changed → {ViewModel.StatusMessage}");
                }
            };

            /*            if (Content is FrameworkElement rootElement)
                        {
                            rootElement.DataContext = ViewModel;
                        }
                        _serviceProvider = App.Services;
                        // Set initial page
                        try
                        {
                            var p = new WorkspaceOnePage();
                            ContentFrame.Content = p; // optional: display it
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("WorkspaceOnePage ctor failed:");
                            System.Diagnostics.Debug.WriteLine(ex.ToString());
                            throw;
                        }

                        try
                        {
                            ContentFrame.Navigate(typeof(WorkspaceOnePage));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("NAV FAILED:");
                            System.Diagnostics.Debug.WriteLine(ex.ToString());

                            if (ex is Microsoft.UI.Xaml.Markup.XamlParseException xpe)
                            {
                                System.Diagnostics.Debug.WriteLine("XamlParseException: " + xpe.Message);
                                System.Diagnostics.Debug.WriteLine("Inner: " + xpe.InnerException?.ToString());
                            }

                            throw;
                        }
                     */

            ContentFrame.Navigate(typeof(WorkspaceOnePage));
            StatusMessage = ViewModel.StatusMessage;
            IsLoading = ViewModel.IsLoading;
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
                    case "WorkspaceOnePage":
                        ContentFrame.Navigate(typeof(WorkspaceOnePage));
                        break;

                    case "OemConfigurationManagerPage":
                        ContentFrame.Navigate(typeof(OemConfigurationManagerPage));
                        break;

                    case "WS1AndroidBatteryPage":
                        ContentFrame.Navigate(typeof(WS1AndroidBatteryPage));
                        break;
                }
            }
        }
    }
}
