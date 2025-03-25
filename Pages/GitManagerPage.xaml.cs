using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class GitManagerPage : Page
    {
        public MainViewModel ViewModel { get; }

        public GitManagerPage()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
        }
    }
}
