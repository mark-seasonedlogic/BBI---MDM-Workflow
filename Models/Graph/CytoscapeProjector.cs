using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.GraphDS;
using System.Text.Json;

namespace BBIHardwareSupport.MDM.Models.Graph
{


    public static class CytoscapeProjector
    {
        // Stable key for Cytoscape ids (avoid default record ToString)
        private static string Key(NodeId id) => $"{id.Kind}:{id.Id}";

        // Map NodeKind → string used by JS styling (node[type="..."])
        private static string TypeOf(NodeKind k) => k switch
        {
            NodeKind.SmartGroup => "SmartGroup",
            NodeKind.Profile => "Profile",
            NodeKind.App => "App",
            NodeKind.Policy => "Policy",
            NodeKind.Device => "Device",
            NodeKind.User => "User",
            _ => "Unknown"
        };

        // Map RelType → edge label
        private static string LabelOf(RelType t) => t switch
        {
            RelType.AssignedTo => "AssignedTo",
            RelType.ExcludedFrom => "ExcludedFrom",
            _ => t.ToString()
        };

        public static string ToElementsJson(
            IReadOnlyList<GraphNode> nodes,
            IReadOnlyList<GraphEdge> edges,
            IReadOnlyList<PendingChange>? pending = null)
        {
            var list = new List<object>(nodes.Count + edges.Count + (pending?.Count ?? 0));

            // Nodes
            foreach (var n in nodes)
            {
                list.Add(new
                {
                    data = new
                    {
                        id = Key(n.Id),
                        label = string.IsNullOrWhiteSpace(n.Name) ? n.Id.Id : n.Name,
                        type = TypeOf(n.Id.Kind),
                        meta = n.Meta
                    }
                });
            }

            // Edges (current state)
            foreach (var e in edges)
            {
                var src = Key(e.From);
                var dst = Key(e.To);
                var id = $"E:{LabelOf(e.Type)}:{src}->{dst}";
                list.Add(new
                {
                    data = new { id, source = src, target = dst, label = LabelOf(e.Type) }
                });
            }

            // Pending changes (optional, dashed)
            if (pending is { Count: > 0 })
            {
                foreach (var p in pending)
                {
                    var src = Key(p.From);
                    var dst = Key(p.To);
                    var id = $"E:P:{LabelOf(p.Type)}:{src}->{dst}";
                    list.Add(new
                    {
                        data = new { id, source = src, target = dst, label = LabelOf(p.Type) },
                        classes = p.Add ? "pendingAdd" : "pendingRemove"
                    });
                }
            }

            return JsonSerializer.Serialize(list);
        }
    }

}
