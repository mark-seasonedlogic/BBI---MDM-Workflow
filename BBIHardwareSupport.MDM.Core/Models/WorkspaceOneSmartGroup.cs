using System;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models
{
    /// <summary>
    /// Represents a SmartGroup in Workspace ONE, including identity, classification,
    /// assignment logic, and metadata relevant to packaging and automation workflows.
    /// </summary>
    public class WorkspaceOneSmartGroup
    {
        /// <summary>
        /// The unique identifier for the SmartGroup.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the SmartGroup.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional description or purpose of the SmartGroup.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A short summary of the membership logic (if any).
        /// Typically a concatenated description of platform, tags, models, etc.
        /// </summary>
        public string MembershipCriteriaSummary { get; set; }

        /// <summary>
        /// Indicates the type of SmartGroup (e.g., 'Criteria', 'Manual', etc.).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Indicates the platform this group targets (e.g., 'Android', 'Apple').
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Indicates the assignment method (e.g., 'Static' or 'Dynamic').
        /// </summary>
        public string AssignmentType { get; set; }

        /// <summary>
        /// Count of devices currently assigned to this SmartGroup.
        /// </summary>
        public int ManagedDeviceCount { get; set; }

        /// <summary>
        /// The Workspace ONE Organization Group ID that owns or manages this SmartGroup.
        /// </summary>
        public string OrganizationGroupId { get; set; }

        /// <summary>
        /// Timestamp of the last modification made to this SmartGroup.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }

        /// <summary>
        /// A list of individual membership criteria that define dynamic group inclusion logic.
        /// </summary>
        public List<WorkspaceOneSmartGroupCriteria> MembershipCriteria { get; set; } = new();

        /// <summary>
        /// Optional metadata for internal packaging, classification, or UI mapping.
        /// </summary>
        public Dictionary<string, object> Tags { get; set; } = new();
    }

    /// <summary>
    /// Represents an individual SmartGroup membership criterion (e.g., model, tag, ownership).
    /// </summary>
    public class WorkspaceOneSmartGroupCriteria
    {
        /// <summary>
        /// The type of criterion (e.g., 'Model', 'Ownership', 'DeviceTag').
        /// </summary>
        public string CriteriaType { get; set; }

        /// <summary>
        /// The value associated with the criterion (e.g., 'TC52', 'POS', 'Corporate-Owned').
        /// </summary>
        public string Value { get; set; }
    }
}
