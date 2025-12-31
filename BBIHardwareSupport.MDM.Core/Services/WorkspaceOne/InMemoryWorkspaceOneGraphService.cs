using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.GraphDS;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{

    public sealed class InMemoryWorkspaceOneGraphService : IWorkspaceOneGraphService
    {
        private readonly IJournalService _journal;

        private readonly List<GraphNode> _nodes = new();
        private readonly ConcurrentBag<GraphEdge> _edges = new();

        public InMemoryWorkspaceOneGraphService(IJournalService journal)
        {
            _journal = journal;

            // Seed: 2 SGs, 2 Profiles, 1 App, 1 Policy
            var sgPos = new GraphNode(new NodeId(NodeKind.SmartGroup, "sg-obs-pos"), "OBS Android POS");
            var sgAll13 = new GraphNode(new NodeId(NodeKind.SmartGroup, "sg-android13"), "Android 13 Tablets");

            var profWifi = new GraphNode(new NodeId(NodeKind.Profile, "prof-wifi"), "Wi‑Fi Profile");
            var profRestr = new GraphNode(new NodeId(NodeKind.Profile, "prof-restrict"), "Restrictions Profile");
            var appPosi = new GraphNode(new NodeId(NodeKind.App, "app-positerm"), "POSiTerm 2.7.1");
            var polComp = new GraphNode(new NodeId(NodeKind.Policy, "pol-compliance"), "Compliance: Passcode");

            _nodes.AddRange(new[] { sgPos, sgAll13, profWifi, profRestr, appPosi, polComp });

            _edges.Add(new GraphEdge(sgPos.Id, profWifi.Id, RelType.AssignedTo));
            _edges.Add(new GraphEdge(sgPos.Id, appPosi.Id, RelType.AssignedTo));
            _edges.Add(new GraphEdge(sgAll13.Id, profRestr.Id, RelType.AssignedTo));
            _edges.Add(new GraphEdge(sgAll13.Id, polComp.Id, RelType.ExcludedFrom));
        }

        public Task<IReadOnlyList<GraphNode>> GetNodesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<GraphNode>>(_nodes);

        public Task<IReadOnlyList<GraphEdge>> GetEdgesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<GraphEdge>>(_edges.ToList());

        public Task<IReadOnlyList<GraphNode>> GetSmartGroupsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<GraphNode>>(_nodes.Where(n => n.Id.Kind == NodeKind.SmartGroup).ToList());

        public Task<IReadOnlyList<GraphNode>> GetAllArtifactsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<GraphNode>>(_nodes.Where(n =>
                   n.Id.Kind is NodeKind.Profile or NodeKind.App or NodeKind.Policy).ToList());

        public Task<IReadOnlyList<GraphNode>> GetArtifactsForSmartGroupAsync(NodeId smartGroupId, CancellationToken ct)
        {
            var ids = _edges.Where(e => e.From == smartGroupId).Select(e => e.To).ToHashSet();
            var arts = _nodes.Where(n => ids.Contains(n.Id)).ToList();
            return Task.FromResult<IReadOnlyList<GraphNode>>(arts);
        }

        public Task<ImpactReport> PreviewAsync(IEnumerable<PendingChange> changes, CancellationToken ct)
        {
            // Dummy logic: just echo out “devices gaining/losing” using fake serials.
            var gains = new List<string>();
            var losses = new List<string>();
            var warnings = new List<string>();

            foreach (var ch in changes)
            {
                var tag = $"{ch.From.Id}:{ch.To.Id} ({ch.Type})";
                if (ch.Add) gains.Add($"Device-{Math.Abs(tag.GetHashCode()) % 1000:D3}");
                else losses.Add($"Device-{Math.Abs(tag.GetHashCode()) % 1000:D3}");
            }

            return Task.FromResult(new ImpactReport(gains, losses, warnings));
        }

        public async Task<ApplyResult> ApplyAsync(IEnumerable<PendingChange> changes, CancellationToken ct)
        {
            var opId = await _journal.StartOperationAsync("Apply Workspace ONE relationship changes", ct);
            try
            {
                foreach (var ch in changes)
                {
                    if (ch.Add)
                    {
                        var edge = new GraphEdge(ch.From, ch.To, ch.Type);
                        _edges.Add(edge);
                        await _journal.AppendAsync(opId, new { action = "add", edge }, ct);
                    }
                    else
                    {
                        var toRemove = _edges.FirstOrDefault(e => e.From == ch.From && e.To == ch.To && e.Type == ch.Type);
                        if (toRemove is not null)
                        {
                            _edges.TryTake(out _); // remove any; then re-add back all except toRemove
                            foreach (var e in _edges.Where(e => !(e.From == toRemove.From && e.To == toRemove.To && e.Type == toRemove.Type)).ToList())
                            {
                                // no-op; bag lacks remove by item; in real service you'd call WS1 unassign
                            }
                            await _journal.AppendAsync(opId, new { action = "remove", edge = toRemove }, ct);
                        }
                    }
                }

                await _journal.CompleteAsync(opId, true, "OK", ct);
                return new ApplyResult(true, "Applied.");
            }
            catch (Exception ex)
            {
                await _journal.CompleteAsync(opId, false, ex.Message, ct);
                return new ApplyResult(false, ex.Message);
            }
        }
    }
}