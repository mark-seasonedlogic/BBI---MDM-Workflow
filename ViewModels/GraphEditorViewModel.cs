using BBIHardwareSupport.MDM.Models.Graph;
using BBIHardwareSupport.MDM.Services.Graph;
using BBIHardwareSupport.MDM.Services.WorkspaceOne;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace BBIHardwareSupport.MDM.ViewModels;

public sealed partial class GraphEditorViewModel : ObservableObject
{
    private readonly IWorkspaceOneGraphService _svc;

    public ObservableCollection<GraphNode> SmartGroups { get; } = new();
    public ObservableCollection<GraphNode> AssignedArtifacts { get; } = new();
    public ObservableCollection<GraphNode> AvailableArtifacts { get; } = new();
    public ObservableCollection<PendingChange> ChangeSet { get; } = new();

    [ObservableProperty] private GraphNode? selectedSmartGroup;
    [ObservableProperty] private GraphNode? selectedAssignedArtifact;
    [ObservableProperty] private GraphNode? selectedAvailableArtifact;
    [ObservableProperty] private ImpactReport? previewResult;
    [ObservableProperty] private bool isBusy;

    public IRelayCommand ReloadCommand { get; }
    public IRelayCommand AddAssignCommand { get; }
    public IRelayCommand AddExcludeCommand { get; }
    public IRelayCommand RemoveLinkCommand { get; }
    public IAsyncRelayCommand PreviewCommand { get; }
    public IAsyncRelayCommand ApplyCommand { get; }
    public IRelayCommand ClearChangesCommand { get; }

    public GraphEditorViewModel(IWorkspaceOneGraphService svc)
    {
        _svc = svc;

        ReloadCommand = new RelayCommand(async () => await ReloadAsync());
        AddAssignCommand = new RelayCommand(() => AddLink(RelType.AssignedTo), () => CanAddLink());
        AddExcludeCommand = new RelayCommand(() => AddLink(RelType.ExcludedFrom), () => CanAddLink());
        RemoveLinkCommand = new RelayCommand(RemoveLink, () => SelectedAssignedArtifact is not null && SelectedSmartGroup is not null);

        PreviewCommand = new AsyncRelayCommand(async () =>
        {
            IsBusy = true;
            try { PreviewResult = await _svc.PreviewAsync(ChangeSet, CancellationToken.None); }
            finally { IsBusy = false; }
        });

        ApplyCommand = new AsyncRelayCommand(async () =>
        {
            IsBusy = true;
            try
            {
                var res = await _svc.ApplyAsync(ChangeSet, CancellationToken.None);
                if (res.Success) { ChangeSet.Clear(); await ReloadAsync(); }
            }
            finally { IsBusy = false; }
        });

        ClearChangesCommand = new RelayCommand(() => ChangeSet.Clear());
    }

    public async Task ReloadAsync()
    {
        IsBusy = true;
        try
        {
            SmartGroups.Clear();
            AssignedArtifacts.Clear();
            AvailableArtifacts.Clear();
            PreviewResult = null;

            var sgs = await _svc.GetSmartGroupsAsync(CancellationToken.None);
            foreach (var s in sgs) SmartGroups.Add(s);

            if (SelectedSmartGroup is not null)
                await LoadArtifactsForSelectedAsync();
        }
        finally { IsBusy = false; }
    }

    partial void OnSelectedSmartGroupChanged(GraphNode? value)
    {
        _ = LoadArtifactsForSelectedAsync();
    }

    private async Task LoadArtifactsForSelectedAsync()
    {
        AssignedArtifacts.Clear();
        AvailableArtifacts.Clear();

        if (SelectedSmartGroup is null) return;

        var current = await _svc.GetArtifactsForSmartGroupAsync(SelectedSmartGroup.Id, CancellationToken.None);
        foreach (var a in current) AssignedArtifacts.Add(a);

        var all = await _svc.GetAllArtifactsAsync(CancellationToken.None);
        var currentIds = current.Select(x => x.Id).ToHashSet();
        foreach (var a in all.Where(a => !currentIds.Contains(a.Id))) AvailableArtifacts.Add(a);
    }

    private bool CanAddLink()
        => SelectedSmartGroup is not null && SelectedAvailableArtifact is not null;

    private void AddLink(RelType type)
    {
        if (SelectedSmartGroup is null || SelectedAvailableArtifact is null) return;
        ChangeSet.Add(new PendingChange(Add: true, type, SelectedSmartGroup.Id, SelectedAvailableArtifact.Id));
    }

    private void RemoveLink()
    {
        if (SelectedSmartGroup is null || SelectedAssignedArtifact is null) return;
        // We stage a remove for both AssignedTo and ExcludedFrom. In real life, you’d know which kind it is.
        ChangeSet.Add(new PendingChange(Add: false, RelType.AssignedTo, SelectedSmartGroup.Id, SelectedAssignedArtifact.Id));
        ChangeSet.Add(new PendingChange(Add: false, RelType.ExcludedFrom, SelectedSmartGroup.Id, SelectedAssignedArtifact.Id));
    }
}
