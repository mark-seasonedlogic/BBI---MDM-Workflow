using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.GraphDS;
namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public interface IWorkspaceOneGraphService
    {
        // Load base graph objects for the selected OG/scope.
        Task<IReadOnlyList<GraphNode>> GetNodesAsync(CancellationToken ct);
        Task<IReadOnlyList<GraphEdge>> GetEdgesAsync(CancellationToken ct);

        // Helpers — convenience for common lookups.
        Task<IReadOnlyList<GraphNode>> GetSmartGroupsAsync(CancellationToken ct);
        Task<IReadOnlyList<GraphNode>> GetArtifactsForSmartGroupAsync(NodeId smartGroupId, CancellationToken ct);
        Task<IReadOnlyList<GraphNode>> GetAllArtifactsAsync(CancellationToken ct); // profiles + apps + policies

        // Planning & execution
        Task<ImpactReport> PreviewAsync(IEnumerable<PendingChange> changes, CancellationToken ct);
        Task<ApplyResult> ApplyAsync(IEnumerable<PendingChange> changes, CancellationToken ct);
    }
}
