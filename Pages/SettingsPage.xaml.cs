using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class SettingsPage : Page
    {
        public MainViewModel ViewModel { get; }

        public SettingsPage()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }
    }
}
