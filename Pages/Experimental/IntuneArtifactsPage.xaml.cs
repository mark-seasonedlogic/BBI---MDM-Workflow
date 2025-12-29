using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class IntuneArtifactsPage : Page
    {
        public MainViewModel ViewModel { get; }

        public IntuneArtifactsPage()
        {
            this.InitializeComponent();

            // Get ViewModel from the DI container
            ViewModel = App.Services.GetRequiredService<MainViewModel>();
            DataContext = ViewModel;
        }
    }
}
