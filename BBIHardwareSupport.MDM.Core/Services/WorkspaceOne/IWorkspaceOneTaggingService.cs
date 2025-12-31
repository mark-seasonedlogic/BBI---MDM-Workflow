using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public interface IWorkspaceOneTaggingService
    {
        /// <summary>
        /// Runs the time-zone tag audit logic (equivalent of Invoke-WS1TimeZoneTagAudit without Apply).
        /// Produces one plan item per restaurant code.
        /// </summary>
        Task<List<WorkspaceOneTimeZoneTagPlanItem>> InvokeTimeZoneTagAuditAsync(
            TimeZoneTagAuditRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the current set of DeviceIds that are members of the given tag
        /// (equivalent of Get-WS1DevicesForTag in PowerShell).
        /// </summary>
        Task<HashSet<string>> GetDevicesForTagAsync(
            int tagId,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds the specified devices to the tag, batching into smaller chunks
        /// (equivalent of Add-WS1DevicesToTagBulk in PowerShell).
        /// </summary>
        Task BulkAddDevicesToTagAsync(
            int tagId,
            IReadOnlyCollection<string> deviceIds,
            int batchSize,
            CancellationToken cancellationToken = default);
    }
}
