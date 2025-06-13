using Microsoft.UI.Xaml.Controls;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels;
using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Views
{
    public sealed partial class GitManagerPage : Page
    {
        public GitManagerViewModel ViewModel { get; }

        public GitManagerPage()
        {
            this.InitializeComponent();

            // Get ViewModel from the DI container
            ViewModel = App.Services.GetRequiredService<GitManagerViewModel>();
            DataContext = ViewModel;
        }
        private async void CreateBranchButton_Click(object sender, RoutedEventArgs e)
        {
            var path = await UiDialogHelper.PromptForFolderAsync(this.XamlRoot);
            if (!string.IsNullOrWhiteSpace(path))
            {
                ViewModel.RepositoryRootPath = path;
            }
            var name = await UiDialogHelper.ShowInputDialogAsync(this.XamlRoot, "Enter Branch Name", "Branch Name");
            if (!string.IsNullOrWhiteSpace(path))
            {
                ViewModel.BranchName = name;
                ViewModel.CreateBranchCommand.Execute(null);
            }
        }
 


    }
}
