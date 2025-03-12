using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly GraphAuthHelper _graphAuthHelper;
    private string _groupId;
    private ObservableCollection<string> _artifacts;
    private string _branchName;
    private string _repositoryPath;
    private string _diffOutput;
    private string _statusMessage;

    public event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel()
    {
        _graphAuthHelper = new GraphAuthHelper();
        _artifacts = new ObservableCollection<string>();

        GetDeviceConfigurationsCommand = new RelayCommand(async () => await GetDeviceConfigurations());
        GetAppsCommand = new RelayCommand(async () => await GetApps());
        GetIntunePoliciesCommand = new RelayCommand(async () => await GetIntunePoliciesAsync());
        GetIntunePoliciesJsonCommand = new RelayCommand(async () => await GetIntunePoliciesJsonAsync());
        GetAllIntunePoliciesJsonCommand = new RelayCommand(async () => await GetAllIntunePoliciesJsonAsync());


        CreateBranchCommand = new RelayCommand(() => CreateBranch());
        CommitChangesCommand = new RelayCommand(() => CommitChanges());
        GetDiffCommand = new RelayCommand(() => GetDiff());
        CreateTagCommand = new RelayCommand(() => CreateTag());
        GetCommitHistoryCommand = new RelayCommand(() => GetCommitHistory());
        SelectRepositoryCommand = new RelayCommand(() => SelectRepository());
        GetUserProfileCommand = new RelayCommand(async () => await GetUserProfile());
    }

    public string BranchName
    {
        get => _branchName;
        set { _branchName = value; OnPropertyChanged(); }
    }

    public string RepositoryPath
    {
        get => _repositoryPath;
        set { _repositoryPath = value; OnPropertyChanged(); }
    }

    public string DiffOutput
    {
        get => _diffOutput;
        set { _diffOutput = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public string GroupId
    {
        get => _groupId;
        set { _groupId = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> Artifacts
    {
        get => _artifacts;
        set { _artifacts = value; OnPropertyChanged(); }
    }

    public ICommand GetDeviceConfigurationsCommand { get; }
    public ICommand GetAppsCommand { get; }
    public ICommand GetIntunePoliciesCommand { get; }  // New Command
    public ICommand CreateBranchCommand { get; }
    public ICommand CommitChangesCommand { get; }
    public ICommand GetDiffCommand { get; }
    public ICommand CreateTagCommand { get; }
    public ICommand GetCommitHistoryCommand { get; }
    public ICommand SelectRepositoryCommand { get; }
    public ICommand GetUserProfileCommand { get; }
    public ICommand GetIntunePoliciesJsonCommand { get; }
    public ICommand GetAllIntunePoliciesJsonCommand { get; }
    private async Task GetAllIntunePoliciesJsonAsync()
    {
        Artifacts.Clear();
        Artifacts.Add("Fetching all Intune policies (JSON)...");

        try
        {
            var compliancePoliciesJson = await _graphAuthHelper.GetDeviceCompliancePoliciesJsonAsync();
            var configurationProfilesJson = await _graphAuthHelper.GetDeviceConfigurationProfilesJsonAsync();
            var appProtectionPoliciesJson = await _graphAuthHelper.GetAppProtectionPoliciesJsonAsync();
            var endpointSecurityPoliciesJson = await _graphAuthHelper.GetEndpointSecurityPoliciesJsonAsync();

            Artifacts.Add("📌 Compliance Policies:");
            Artifacts.Add(compliancePoliciesJson);

            Artifacts.Add("\n⚙️ Device Configuration Profiles:");
            Artifacts.Add(configurationProfilesJson);

            Artifacts.Add("\n🛡️ App Protection Policies:");
            Artifacts.Add(appProtectionPoliciesJson);

            Artifacts.Add("\n🔐 Endpoint Security Policies:");
            Artifacts.Add(endpointSecurityPoliciesJson);
        }
        catch (Exception ex)
        {
            Artifacts.Add($"❌ Error fetching policies: {ex.Message}");
        }
    }

    private async Task GetDeviceConfigurations()
    {
        if (string.IsNullOrWhiteSpace(GroupId)) return;

        var configurations = await _graphAuthHelper.GetDeviceConfigurationsForGroupAsync(GroupId);
        Artifacts.Clear();
        foreach (var config in configurations)
        {
            Artifacts.Add($"Device Configuration: {config}");
        }
    }
    private async Task GetIntunePoliciesJsonAsync()
    {
        Artifacts.Clear();
        Artifacts.Add("Fetching Intune policies (JSON)...");

        try
        {
            var compliancePoliciesJson = await _graphAuthHelper.GetDeviceCompliancePoliciesJsonAsync();
            var configurationProfilesJson = await _graphAuthHelper.GetDeviceConfigurationProfilesJsonAsync();

            Artifacts.Add("📌 Compliance Policies (JSON):");
            Artifacts.Add(compliancePoliciesJson);

            Artifacts.Add("\n⚙️ Device Configuration Profiles (JSON):");
            Artifacts.Add(configurationProfilesJson);
        }
        catch (Exception ex)
        {
            Artifacts.Add($"❌ Error fetching policies: {ex.Message}");
        }
    }

    private async Task GetApps()
    {
        if (string.IsNullOrWhiteSpace(GroupId)) return;

        var apps = await _graphAuthHelper.GetAppsForGroupAsync(GroupId);
        Artifacts.Clear();
        foreach (var app in apps)
        {
            Artifacts.Add($"App: {app}");
        }
    }

    /// <summary>
    /// Fetches Intune Compliance Policies and Device Configuration Profiles.
    /// </summary>
    private async Task GetIntunePoliciesAsync()
    {
        Artifacts.Clear();
        Artifacts.Add("Fetching Intune policies...");

        try
        {
            var compliancePolicies = await _graphAuthHelper.GetDeviceCompliancePoliciesAsync();
            Artifacts.Add("📌 Compliance Policies:");
            foreach (var policy in compliancePolicies)
            {
                Artifacts.Add(policy);
            }

            var configProfiles = await _graphAuthHelper.GetDeviceConfigurationProfilesAsync();
            Artifacts.Add("\n⚙️ Device Configuration Profiles:");
            foreach (var profile in configProfiles)
            {
                Artifacts.Add(profile);
            }
        }
        catch (Exception ex)
        {
            Artifacts.Add($"❌ Error fetching policies: {ex.Message}");
        }
    }

    private void CreateBranch() { /* Implement logic */ }
    private void CommitChanges() { /* Implement logic */ }
    private void GetDiff() { /* Implement logic */ }
    private void CreateTag() { /* Implement logic */ }
    private void GetCommitHistory() { /* Implement logic */ }
    private void SelectRepository() { /* Implement logic */ }


    private async Task GetUserProfile() { /* Implement logic */ }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
