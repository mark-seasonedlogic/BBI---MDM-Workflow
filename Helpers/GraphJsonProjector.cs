using BBIHardwareSupport.MDM.Models.Graph;
using BBIHardwareSupport.MDM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Helpers
{

    public static class GraphJsonProjector
    {
        private static string LabelOf(string? name, NodeId id)
    => string.IsNullOrWhiteSpace(name) ? id.ToString() : name;


        public static object FromVm(GraphEditorViewModel vm)
        {
            var nodes = new List<object>();
            var edges = new List<object>();

            if (vm.SelectedSmartGroup is not null)
            {
                var sg = vm.SelectedSmartGroup;
                nodes.Add(new
                {
                    data = new
                    {
                        id = $"SG:{sg.Id}",
                        label = LabelOf(sg.Name, sg.Id),   // ← was sg.Label
                        type = "SmartGroup"
                    }
                });

                foreach (var a in vm.AssignedArtifacts)
                {
                    nodes.Add(new
                    {
                        data = new
                        {
                            id = $"A:{a.Id}",
                            label = LabelOf(a.Name,a.Id),   // ← was a.Label
                            type = "Artifact"
                        }
                    });
                    edges.Add(new
                    {
                        data = new
                        {
                            id = $"E:ASSIGN:{sg.Id}->{a.Id}",
                            source = $"SG:{sg.Id}",
                            target = $"A:{a.Id}",
                            label = "AssignedTo"
                        }
                    });
                }

                // Optional: preview/available nodes
                foreach (var a in vm.AvailableArtifacts.Take(50))
                {
                    nodes.Add(new
                    {
                        data = new
                        {
                            id = $"A:{a.Id}",
                            label = LabelOf(a.Name,a.Id),   // ← was a.Label
                            type = "Artifact"
                        },
                        classes = "available"
                    });
                }

                // Pending changes
                static string Key(NodeId id) => id.ToString(); // ensure NodeId.ToString() returns a stable value

                foreach (var ch in vm.ChangeSet)
                {
                    var cls = ch.Add ? "pendingAdd" : "pendingRemove";

                    var from = Key(ch.From); // ← was ch.SourceId
                    var to = Key(ch.To);   // ← was ch.TargetId

                    edges.Add(new
                    {
                        data = new
                        {
                            id = $"E:P:{ch.Type}:{from}->{to}",
                            source = $"SG:{from}",
                            target = $"A:{to}",
                            label = ch.Type.ToString()
                        },
                        classes = cls
                    });
                }

            }

            return nodes.Concat<object>(edges).ToArray();
        }
    }

}
