using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class IntuneArtifactsPage : Page
    {
        public MainViewModel ViewModel { get; }

        public IntuneArtifactsPage()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }
    }
}
