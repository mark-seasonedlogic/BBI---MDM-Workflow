using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Models.Graph;

public enum NodeKind { SmartGroup, Profile, App, Policy, Device, User }
public enum RelType { AssignedTo, ExcludedFrom }

public sealed record NodeId(NodeKind Kind, string Id); // Id = WS1 id string
public sealed record GraphNode(NodeId Id, string Name, string? Meta = null);
public sealed record GraphEdge(NodeId From, NodeId To, RelType Type);

public sealed record PendingChange(
    bool Add,          // true = add link, false = remove link
    RelType Type,
    NodeId From,
    NodeId To,
    string? Reason = null);

public sealed record ImpactReport(
    IReadOnlyList<string> DevicesGaining,
    IReadOnlyList<string> DevicesLosing,
    IReadOnlyList<string> Warnings);

public sealed record ApplyResult(bool Success, string? Message = null);
