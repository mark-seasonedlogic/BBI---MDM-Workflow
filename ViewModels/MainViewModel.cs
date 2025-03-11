using BBIHardwareSupport.MDM.IntuneConfigManager.Services;
using BBIHardwareSupport.MDM.IntuneConfigManager.ViewModels.Helpers;
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
    public ICommand CreateBranchCommand { get; }
    public ICommand CommitChangesCommand { get; }
    public ICommand GetDiffCommand { get; }
    public ICommand CreateTagCommand { get; }
    public ICommand GetCommitHistoryCommand { get; }
    public ICommand SelectRepositoryCommand { get; }
    public ICommand GetUserProfileCommand { get; }

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
