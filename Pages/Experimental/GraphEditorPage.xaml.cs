using BBIHardwareSupport.MDM.Helpers;
using BBIHardwareSupport.MDM.Models.Graph;
using BBIHardwareSupport.MDM.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Pages
    ;

public sealed partial class GraphEditorPage : Page
{
    public GraphEditorViewModel VM { get; private set; }

    public GraphEditorPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, __) =>
        {
            var core = GraphView?.CoreWebView2;
            if (core != null) core.WebMessageReceived -= CoreWebView2_WebMessageReceived;

            // detach CollectionChanged events
            if (VM != null)
            {
                VM.AssignedArtifacts.CollectionChanged -= OnCollectionsChanged;
                VM.AvailableArtifacts.CollectionChanged -= OnCollectionsChanged;
                VM.ChangeSet.CollectionChanged -= OnCollectionsChanged;
                VM.PropertyChanged -= OnVmPropertyChanged;
            }
        };
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GraphEditorViewModel.SelectedSmartGroup) ||
            e.PropertyName is nameof(GraphEditorViewModel.PreviewResult))
        {
            _ = RefreshGraphAsync();
        }
    }

    private void OnCollectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => _ = RefreshGraphAsync();
    private static string BuildElementsJsonFromVm(GraphEditorViewModel vm)
    {
        // nodes: selected SG + assigned + (optionally) a limited set of available
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        if (vm.SelectedSmartGroup is not null)
        {
            var sg = vm.SelectedSmartGroup;
            nodes.Add(sg);

            // Assigned -> edges
            foreach (var a in vm.AssignedArtifacts)
            {
                nodes.Add(a);
                edges.Add(new GraphEdge(sg.Id, a.Id, RelType.AssignedTo));
            }

            // Optional: show a few available artifacts as faint nodes (no edges)
            foreach (var a in vm.AvailableArtifacts.Take(40))
                nodes.Add(a);
        }

        // Pending changes: dashed edges (projector will mark with classes)
        var pending = vm.ChangeSet.ToList();

        return CytoscapeProjector.ToElementsJson(nodes, edges, pending);
    }
    private async Task RefreshGraphAsync()
    {
        if (GraphView?.CoreWebView2 is null) return;

        var json = BuildElementsJsonFromVm(VM);   // ⬅️ uses CytoscapeProjector
        GraphView.CoreWebView2.PostWebMessageAsString(json);
        await Task.CompletedTask;
    }
    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        // Resolve VM from the app's DI container
        VM = App.Services.GetRequiredService<GraphEditorViewModel>();
        DataContext = VM;

        await VM.ReloadAsync(); // your existing flow

        await GraphView.EnsureCoreWebView2Async();
        GraphView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // Load the static html (added in step 3)
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "graph");
        GraphView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "appassets", assetsDir, CoreWebView2HostResourceAccessKind.Allow);

        GraphView.Source = new Uri("https://appassets/index.html");
        // Push data when the page is ready (the html will post a "ready" message)
        VM.PropertyChanged += async (_, args) =>
        {
            if (args.PropertyName is nameof(VM.SelectedSmartGroup) ||
                args.PropertyName is nameof(VM.PreviewResult))
            {
                await TryRenderGraphAsync();
            }
        };

        // also after reload
        _ = TryRenderGraphAsync();
    }
    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Resolve VM (DI or passed-in; adjust if you set DataContext elsewhere)
        if (DataContext is null)
            DataContext = App.Services.GetRequiredService<GraphEditorViewModel>();

        await GraphView.EnsureCoreWebView2Async();
        var core = GraphView.CoreWebView2;

        // 1) Enable menus & DevTools explicitly
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true; // F12
        core.Settings.IsStatusBarEnabled = true;

        // 2) Map virtual host (unpackaged) BEFORE navigating
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "graph");
        core.SetVirtualHostNameToFolderMapping("appassets", assetsDir, CoreWebView2HostResourceAccessKind.Allow);

        // 3) Diagnostics
        core.NavigationCompleted += (_, ev) =>
        {
            System.Diagnostics.Debug.WriteLine($"WV2 NavCompleted: {ev.IsSuccess}, {ev.WebErrorStatus}");
            if (!ev.IsSuccess) core.OpenDevToolsWindow();
        };

        // 4) Handshake with HTML
        core.WebMessageReceived += CoreWebView2_WebMessageReceived;

        // 5) Navigate to index.html
        GraphView.Source = new Uri("https://appassets/index.html");

        // 6) Subscribe to VM changes so the graph auto-refreshes
        VM.PropertyChanged += OnVmPropertyChanged;
        VM.AssignedArtifacts.CollectionChanged += OnCollectionsChanged;
        VM.AvailableArtifacts.CollectionChanged += OnCollectionsChanged;
        VM.ChangeSet.CollectionChanged += OnCollectionsChanged;

        // 7) Load initial data (left pane) – will also trigger refresh when the page is ready
        await VM.ReloadAsync();
    }

    private async Task TryRenderGraphAsync()
    {
        if (GraphView.CoreWebView2 is null) return;

        var elements = GraphJsonProjector.FromVm(VM); // <— helper method below
        var json = JsonSerializer.Serialize(elements);
        GraphView.CoreWebView2.PostWebMessageAsString(json);
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Optionally handle node click messages from JS: e.TryGetWebMessageAsString()
    }


    
}