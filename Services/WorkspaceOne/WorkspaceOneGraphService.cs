using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.Models.Graph;
using System.Text.Json;
using System.Threading;
using System.Net.Http;

namespace BBIHardwareSupport.MDM.Services.WorkspaceOne
{


    public sealed class WorkspaceOneGraphService : IWorkspaceOneGraphService
    {
        private readonly HttpClient _http;

        public WorkspaceOneGraphService(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<GraphNode>> GetSmartGroupsAsync(CancellationToken ct)
        {
            using var resp = await _http.GetAsync("api/mdm/smartgroups/search", ct);
            resp.EnsureSuccessStatusCode();
            using var s = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

            var list = new List<GraphNode>();

            // Adjust to your actual payload shape:
            //  - some tenants return { "SmartGroups": [ ... ] }
            //  - others return [ ... ]
            var root = doc.RootElement;
            var arr = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : root.GetProperty("SmartGroups").EnumerateArray();

            foreach (var sg in arr)
            {
                var id = sg.GetProperty("Id").GetString() ?? sg.GetProperty("SmartGroupId").GetString()!;
                var name = sg.TryGetProperty("Name", out var nm) ? nm.GetString() : id;
                list.Add(new GraphNode(new NodeId(NodeKind.SmartGroup, id!), name ?? id!));
            }
            return list;
        }

        public async Task<IReadOnlyList<GraphNode>> GetArtifactsForSmartGroupAsync(NodeId smartGroupId, CancellationToken ct)
        {
            // Example: profiles assigned to SG
            using var resp = await _http.GetAsync($"api/mdm/smartgroups/{smartGroupId.Id}/profiles", ct);
            resp.EnsureSuccessStatusCode();
            using var s = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);

            var list = new List<GraphNode>();
            foreach (var p in doc.RootElement.EnumerateArray())
            {
                var id = p.GetProperty("Id").GetString()!;
                var name = p.TryGetProperty("Name", out var nm) ? nm.GetString() : id;
                list.Add(new GraphNode(new NodeId(NodeKind.Profile, id), name ?? id));
            }

            // TODO same pattern for Apps / Policies if you want mixed artifacts:
            //  - call apps endpoint
            //  - call compliance/policies endpoint
            //  - add with NodeKind.App / NodeKind.Policy

            return list;
        }

        public async Task<IReadOnlyList<GraphNode>> GetAllArtifactsAsync(CancellationToken ct)
        {
            var list = new List<GraphNode>();

            // Profiles
            using (var r = await _http.GetAsync("api/mdm/profiles/search", ct))
            {
                r.EnsureSuccessStatusCode();
                using var s = await r.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
                foreach (var p in doc.RootElement.EnumerateArray())
                {
                    var id = p.GetProperty("Id").GetString()!;
                    var name = p.TryGetProperty("Name", out var nm) ? nm.GetString() : id;
                    list.Add(new GraphNode(new NodeId(NodeKind.Profile, id), name ?? id));
                }
            }

            // Apps (example; adjust endpoint/shape)
            using (var r = await _http.GetAsync("api/mam/apps/search", ct))
            {
                if (r.IsSuccessStatusCode)
                {
                    using var s = await r.Content.ReadAsStreamAsync(ct);
                    using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
                    foreach (var a in doc.RootElement.EnumerateArray())
                    {
                        var id = a.GetProperty("Id").GetString()!;
                        var name = a.TryGetProperty("ApplicationName", out var nm) ? nm.GetString()
                                  : a.TryGetProperty("Name", out nm) ? nm.GetString()
                                  : id;
                        list.Add(new GraphNode(new NodeId(NodeKind.App, id), name ?? id));
                    }
                }
            }

            // Policies (if applicable)
            // Add NodeKind.Policy similarly

            return list;
        }

        public Task<ImpactReport> PreviewAsync(IEnumerable<PendingChange> changes, CancellationToken ct)
            => Task.FromResult(new ImpactReport(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));

        public Task<ApplyResult> ApplyAsync(IEnumerable<PendingChange> changes, CancellationToken ct)
            => Task.FromResult(new ApplyResult(true, "Not implemented (server)"));
    }

}
