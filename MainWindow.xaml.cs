using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.UI.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Views;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace BBIHardwareSupport.MDM.IntuneConfigManager
{
    public sealed partial class MainWindow : Window
    {
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
                }
            }
        }
    }
}
