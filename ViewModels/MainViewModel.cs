using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BBIHardwareSupport.MDM.IntuneConfigManager;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
using Microsoft.Graph.Models;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GraphAuthHelper _graphAuthHelper;
    private string _groupId;
    private string _branchName;
    private string _repositoryPath;
    private string _statusMessage;
    private bool _isLoading;
    private bool _isAutoSyncEnabled;
    private string _diffOutput;
    private readonly IGraphAuthService _authService;
    private readonly IGraphIntuneDeviceService _managedDeviceService;
    private readonly IGraphADDeviceService _enrolledDeviceService;

    public string DiffOutput
    {
        get => _diffOutput;
        set { _diffOutput = value; OnPropertyChanged(); }
    }
    public MainViewModel(IGraphAuthService authService, IGraphIntuneDeviceService managedDeviceService, IGraphADDeviceService enrolledDeviceService)
    {
        _authService = authService;
        _managedDeviceService = managedDeviceService;
        _enrolledDeviceService = enrolledDeviceService;
        LoadDevicesCommand = new RelayCommand(async () => await LoadDevicesAsync());
        GetDeviceConfigurationsCommand = new RelayCommand(async () => await LoadDeviceConfigurations());
        GetAppsCommand = new RelayCommand(async () => await LoadApps());
        CommitChangesCommand = new RelayCommand(() => CommitChanges());
        SaveSettingsCommand = new RelayCommand(() => SaveSettings());
        GetDiffCommand = new RelayCommand(() => GetDiff());
        CreateBranchCommand = new RelayCommand(() => CreateBranch());
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

    public ICommand GetDiffCommand { get; }
    public ICommand CreateBranchCommand { get; }
    public ICommand ShowHelpCommand { get; }
    public ICommand ProcessDevicesCommand { get; }
    public ObservableCollection<ArtifactModel> Artifacts { get; set; }

    public ICommand GetDeviceConfigurationsCommand { get; }
    public ICommand GetAppsCommand { get; }
    public ICommand CommitChangesCommand { get; }
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

    public string BranchName
    {
        get => _branchName;
        set { _branchName = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsAutoSyncEnabled
    {
        get => _isAutoSyncEnabled;
        set { _isAutoSyncEnabled = value; OnPropertyChanged(); }
    }


    private void GetDiff()
    {
        StatusMessage = "Fetching Git diff...";

        try
        {
            // Example: Simulate diff retrieval
            System.Threading.Thread.Sleep(2000);  // Simulate processing delay

            DiffOutput = "Example Git Diff:\n- Line 1 changed\n+ Line 2 added";
            StatusMessage = "✅ Git diff retrieved successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to get diff: {ex.Message}";
        }
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


    private void CreateBranch()
    {
        StatusMessage = "Creating new Git branch...";

        try
        {
            // Example: Simulate Git branch creation process
            System.Threading.Thread.Sleep(2000);  // Simulate processing delay

            StatusMessage = $"✅ Branch '{BranchName}' created successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to create branch: {ex.Message}";
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
    private void CommitChanges()
    {
        // Example logic for committing changes
        StatusMessage = "Committing changes to Git...";

        try
        {
            // Example: Simulate a commit process
            System.Threading.Thread.Sleep(2000);  // Simulate processing delay

            StatusMessage = "✅ Changes committed successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Commit failed: {ex.Message}";
        }
    }

}

public class ArtifactModel
{
    public string Name { get; set; }
    public string Type { get; set; }
}
