using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Represents the artifacts assigned to a SmartGroup within Workspace ONE.
    /// </summary>
    public class WorkspaceOneSmartGroupAssignment
    {
        /// <summary>
        /// The ID of the associated SmartGroup.
        /// </summary>
        public string SmartGroupId { get; set; }

        /// <summary>
        /// Product provisioning profiles assigned to this SmartGroup.
        /// </summary>
        public List<string> ProductIds { get; set; }

        /// <summary>
        /// Profile IDs (device configuration profiles) assigned.
        /// </summary>
        public List<string> ProfileIds { get; set; }

        /// <summary>
        /// Application IDs assigned to the SmartGroup.
        /// </summary>
        public List<string> ApplicationIds { get; set; }

        /// <summary>
        /// Compliance policy IDs (if used) assigned to the SmartGroup.
        /// </summary>
        public List<string> CompliancePolicyIds { get; set; }

        /// <summary>
        /// Optional: Any other assigned resources such as scripts, sensors, or custom settings.
        /// </summary>
        public Dictionary<string, List<string>> ExtendedAssignments { get; set; }
    }
}
