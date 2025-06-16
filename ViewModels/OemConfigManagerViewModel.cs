using BBIHardwareSupport.MDM.IntuneConfigManager.Helpers;
using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels
{
    public partial class OemConfigManagerViewModel : INotifyPropertyChanged
    {
        private readonly IGraphIntuneConfigurationService _configService;
        private readonly IGraphAuthService _authService;
        private readonly IGraphIntuneManagedAppService _managedAppService;

        public ObservableCollection<string> OemConfigs { get; } = new();
        //public ICommand LoadOemConfigsCommand { get; }
        //public ICommand TestFindOemConfigCommand { get; }

        [RelayCommand]
        public async Task TestFindOemConfigAsync()
        {
            var inputName = await UiDialogHelper.PromptForTextAsync("Enter App Name (Display Name or Package ID):");
            if (string.IsNullOrWhiteSpace(inputName))
                return;

            // Step 1: Find the mobile app object by name
            var app = await _managedAppService.GetManagedAppByNameAsync(inputName);
            if (app == null)
            {
                await UiDialogHelper.ShowMessageAsync("❌ No app found with that name.");
                return;
            }

            var appId = app["id"]?.ToString();
            var appDisplayName = app["displayName"]?.ToString() ?? "(unnamed app)";

            // Step 2: Search all configs that target this app ID
            var config = await _configService.FindManagedAppConfigurationByTargetedAppAsync(appId);
            if (config != null)
            {
                await UiDialogHelper.ShowMessageAsync(
                    $"✅ Config found for app:\n\nApp: {appDisplayName}\nApp ID: {appId}\nConfig ID: {config["id"]}"
                );
            }
            else
            {
                await UiDialogHelper.ShowMessageAsync(
                    $"❌ No configuration found for app '{appDisplayName}' (ID: {appId})."
                );
            }
        }

        [RelayCommand]
        public async Task TestCloneOemConfigAsync()
        {
            var inputName = await UiDialogHelper.PromptForTextAsync("Enter Source Configuration ID:");
            if (string.IsNullOrWhiteSpace(inputName))
                return;
            // Step 1: Search all configs that target this app ID
            var config = await _configService.GetManagedAppConfigurationByIdAsync(inputName);
            if (config != null)
            {
                await UiDialogHelper.ShowMessageAsync(
                    $"✅ Config found for ID: {config["id"]}:\n\nConfig Name: {config["displayname"]}"
                );
            }
            else
            {
                await UiDialogHelper.ShowMessageAsync(
                    $"❌ No configuration found for ID '{inputName}')."
                );
                return;
            }
            // Step 2: Clone the configuration
            var newConfig = await _configService.CloneManagedAppConfigurationAsync(config,"BBI - Olo Expo Master");
            if (newConfig == null)
            {
                await UiDialogHelper.ShowMessageAsync("❌ Unable to clone configuration!");
                return;
            }
            else
            {
                await UiDialogHelper.ShowMessageAsync("✅ Successfully cloned: {config[\"displayname\"]}\n\nNew Config ID: {config[\"id\"]}\nNew Config Name: {config[\"displayname\"]}");
                return;
            }

 
        }

        public OemConfigManagerViewModel(
            IGraphIntuneConfigurationService configService,
            IGraphAuthService authService,
            IGraphIntuneManagedAppService managedAppService)
        {
            _configService = configService;
            _authService = authService;
            _managedAppService = managedAppService;
            //LoadOemConfigsCommand = new RelayCommand(async () => await LoadOemConfigsAsync());
            //TestFindOemConfigCommand = new RelayCommand(async () => await TestFindOemConfigAsync());
        }
        [RelayCommand]
        private async Task LoadOemConfigsAsync()
        {
            var configs = await _configService.GetOemConfigurationsAsync();
            OemConfigs.Clear();
            foreach (var config in configs)
            {
                OemConfigs.Add(config["displayName"]?.ToString() ?? "<no name>");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}