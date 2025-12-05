using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.Models.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace BBIHardwareSupport.MDM.Services.WorkspaceOne
{
    /// <summary>
    /// Bridges Workspace ONE data services (SmartGroups, Profiles, Apps, Policies)
    /// to the GraphEditor’s IWorkspaceOneGraphService interface.
    /// </summary>
    public sealed class WorkspaceOneGraphServiceAdapter : IWorkspaceOneGraphService
    {
        private readonly IWorkspaceOneSmartGroupsService _smartGroups;
        // Plug in other WS1 services as you build them:
        private readonly IWorkspaceOneProfileService? _profiles;   // optional for now
        //private readonly IWorkspaceOneAppsService? _apps;           // optional for now
        //private readonly IWorkspaceOnePoliciesService? _policies;   // optional for now

        public WorkspaceOneGraphServiceAdapter(
            IWorkspaceOneSmartGroupsService smartGroups,
            IWorkspaceOneProfileService? profiles = null)
            /*Add other services as they are implemented:
            IWorkspaceOneAppsService? apps = null,
            IWorkspaceOnePoliciesService? policies = null
            */
        {
            _smartGroups = smartGroups;
            _profiles = profiles;
            //_apps = apps;
            //_policies = policies;
        }

        // -------- Base graph (whole set) --------

        public async Task<IReadOnlyList<GraphNode>> GetNodesAsync(CancellationToken ct)
        {
            var nodes = new List<GraphNode>();

            // Smart Groups
            var sgs = await _smartGroups.GetAllSmartGroupsAsync();
            nodes.AddRange(sgs.Select(sg =>
                new GraphNode(new NodeId(NodeKind.SmartGroup, sg.Id ?? string.Empty),
                              string.IsNullOrWhiteSpace(sg.Name) ? (sg.Id ?? "SmartGroup") : sg.Name,
                              Meta: sg.Description)));

            // Profiles / Apps / Policies can be added here once those services are wired.
            // e.g., nodes.AddRange(await MapProfilesAsync(ct)); etc.

            return nodes;
        }

        public async Task<IReadOnlyList<GraphEdge>> GetEdgesAsync(CancellationToken ct)
        {
            // When you have per‑SG assignment endpoints (profiles/apps/policies),
            // iterate smart groups and build edges here.
            // For now, return empty (your VM does not use this call yet).
            return new List<GraphEdge>();
        }

        // -------- Convenience used by your VM --------

        public async Task<IReadOnlyList<GraphNode>> GetSmartGroupsAsync(CancellationToken ct)
        {
            var sgs = await _smartGroups.GetAllSmartGroupsAsync();
            return sgs.Select(sg =>
                new GraphNode(new NodeId(NodeKind.SmartGroup, sg.Id ?? string.Empty),
                              string.IsNullOrWhiteSpace(sg.Name) ? (sg.Id ?? "SmartGroup") : sg.Name,
                              Meta: sg.Description))
                      .ToList();
        }

        public async Task<IReadOnlyList<GraphNode>> GetArtifactsForSmartGroupAsync(NodeId smartGroupId, CancellationToken ct)
        {
            // TODO: call your WS1 endpoints for “profiles/apps/policies by smart group”
            // and map to GraphNode with proper NodeKind.
            // Return empty until those services are ready.
            return new List<GraphNode>();
        }

        public async Task<IReadOnlyList<GraphNode>> GetAllArtifactsAsync(CancellationToken ct)
        {
            // TODO: combine results from Profiles/Apps/Policies services
            // Return empty for now so the left pane still works.
            return new List<GraphNode>();
        }

        // -------- Planning & execution --------

        public Task<ImpactReport> PreviewAsync(IEnumerable<PendingChange> changes, CancellationToken ct)
        {
            // Implement when you add a preview endpoint; otherwise stub
            return Task.FromResult(new ImpactReport(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        }

        public Task<ApplyResult> ApplyAsync(IEnumerable<PendingChange> changes, CancellationToken ct)
        {
            // Implement when you add assign/unassign endpoints; otherwise stub
            return Task.FromResult(new ApplyResult(true, "Not implemented"));
        }
    }
}
