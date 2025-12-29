using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BBIHardwareSupport.MDM.IntuneConfigManager;
using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
using BBIHardwareSupport.MDM.Services.Authentication;
using Microsoft.Graph.Models;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GraphAuthHelper _graphAuthHelper;
    private string _groupId;
    private string _statusMessage;
    private bool _isLoading;
    private bool _isAutoSyncEnabled;
    private readonly IGraphAuthService _authService;
    private readonly IGraphIntuneDeviceService _managedDeviceService;
    private readonly IGraphADDeviceService _enrolledDeviceService;
    private readonly IGraphADGroupService _groupService;

    
    public MainViewModel(IGraphAuthService authService, IGraphIntuneDeviceService managedDeviceService, IGraphADDeviceService enrolledDeviceService, IGraphADGroupService groupService)
    {
        _authService = authService;
        _managedDeviceService = managedDeviceService;
        _enrolledDeviceService = enrolledDeviceService;
        _groupService = groupService;
        LoadDevicesCommand = new RelayCommand(async () => await LoadDevicesAsync());
        GetDeviceConfigurationsCommand = new RelayCommand(async () => await LoadDeviceConfigurations());
        GetAppsCommand = new RelayCommand(async () => await LoadApps());
        SaveSettingsCommand = new RelayCommand(() => SaveSettings());
        ShowHelpCommand = new RelayCommand(() => ShowHelp());
        ProcessDevicesCommand = new RelayCommand(async () => await ProcessDevicesAsync());
        UpdateUserNameCommand = new RelayCommand(async () => await UpdateDeviceUserNameAsync("deviceId"));
    }
    private async Task UpdateDeviceUserNameAsync(string deviceId)
    {
        var newUserName = "1_9921"; // Could be dynamic
        var token = await _authService.GetAccessTokenAsync();
        await _managedDeviceService.UpdateDeviceUserNameAsync(deviceId, newUserName, token);
    }
    public ICommand LoadDevicesCommand { get; }

    public ICommand ShowHelpCommand { get; }
    public ICommand ProcessDevicesCommand { get; }
    public ObservableCollection<ArtifactModel> Artifacts { get; set; }

    public ICommand GetDeviceConfigurationsCommand { get; }
    public ICommand GetAppsCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand UpdateUserNameCommand { get; }

    private async Task LoadDevicesAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        var managedDevices = await _managedDeviceService.GetDevicesAsync(token);
        Devices = new ObservableCollection<ManagedDevice>(managedDevices);
    }

    public ObservableCollection<ManagedDevice> Devices { get; private set; } = new();

    public string GroupId
    {
        get => _groupId;
        set { _groupId = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    
    private async Task ProcessDevicesAsync()
    {
        IsLoading = true;
        StatusMessage = "Processing devices...";

        try
        {
            var graphAuthHelper = new GraphAuthHelper();
            var updater = new GraphDeviceUpdater(graphAuthHelper);
            //var schemaInfo = await updater.CreateSchemaExtensionAsync();
            await updater.ProcessDevicesAsync();

            StatusMessage = "✅ Devices processed successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error processing devices: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }



    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            System.Diagnostics.Debug.WriteLine($"[MainVM] IsLoading = {value}\n{Environment.StackTrace}");
            OnPropertyChanged();
        }
    }

    public bool IsAutoSyncEnabled
    {
        get => _isAutoSyncEnabled;
        set { _isAutoSyncEnabled = value; OnPropertyChanged(); }
    }


 
    private async void ShowHelp()
    {
        StatusMessage = "Displaying help information...";

        try
        {
            var messageDialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Help",
                Content = "This application allows you to manage Git repositories and Intune configurations.",
                CloseButtonText = "OK",
                XamlRoot = App.MainWindow.Content.XamlRoot // Ensure correct window context
            };

            await messageDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to show help: {ex.Message}";
        }
    }


    
    private async Task LoadDeviceConfigurations()
    {
        if (string.IsNullOrWhiteSpace(GroupId)) return;

        IsLoading = true;
        Artifacts.Clear();
        StatusMessage = "Fetching device configurations...";

        try
        {
            var configurations = await _graphAuthHelper.GetDeviceConfigurationsForGroupAsync(GroupId);
            foreach (var config in configurations)
            {
                Artifacts.Add(new ArtifactModel { Name = config, Type = "Device Configuration" });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadApps()
    {
        if (string.IsNullOrWhiteSpace(GroupId)) return;

        IsLoading = true;
        Artifacts.Clear();
        StatusMessage = "Fetching apps...";

        try
        {
            var apps = await _graphAuthHelper.GetAppsForGroupAsync(GroupId);
            foreach (var app in apps)
            {
                Artifacts.Add(new ArtifactModel { Name = app, Type = "App" });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SaveSettings()
    {
        StatusMessage = $"Settings saved! Auto-Sync: {(IsAutoSyncEnabled ? "Enabled" : "Disabled")}";
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

public class ArtifactModel
{
    public string Name { get; set; }
    public string Type { get; set; }
}
