using System.Collections.Generic;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    /// <summary>
    /// Provides methods to retrieve Workspace ONE SmartGroups and related assignment data.
    /// </summary>
    public interface IWorkspaceOneSmartGroupsService
    {
        /// <summary>
        /// Gets all SmartGroups assigned to a specific device.
        /// </summary>
        /// <param name="deviceId">The Workspace ONE device ID.</param>
        /// <returns>A collection of SmartGroup objects assigned to the device.</returns>
        Task<IEnumerable<WorkspaceOneSmartGroup>> GetSmartGroupsByDeviceIdAsync(string deviceId);

        /// <summary>
        /// Gets detailed information about a SmartGroup by ID.
        /// </summary>
        /// <param name="smartGroupId">The Workspace ONE SmartGroup ID.</param>
        /// <returns>The corresponding SmartGroup object.</returns>
        Task<WorkspaceOneSmartGroup> GetSmartGroupByIdAsync(string smartGroupId);

        /// <summary>
        /// Gets SmartGroup assignment metadata, including linked products, profiles, apps, and compliance policies.
        /// </summary>
        /// <param name="smartGroupId">The Workspace ONE SmartGroup ID.</param>
        /// <returns>A SmartGroup assignment summary.</returns>
        Task<WorkspaceOneSmartGroupAssignment> GetAssignmentsBySmartGroupIdAsync(string smartGroupId);

        /// <summary>
        /// 
        ///
        /// </summary>
        /// <returns></returns>
        Task<List<WorkspaceOneSmartGroup>> GetAllSmartGroupsAsync();
    }
}
