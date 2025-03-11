using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;

namespace BBIHardwareSupport.MDM.IntuneConfigManager
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            // Set DataContext on the root container
            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }
        }
    }
}
