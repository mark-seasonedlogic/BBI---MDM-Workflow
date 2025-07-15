using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Represents a product provisioning profile in Workspace ONE.
    /// </summary>
    public class WorkspaceOneProduct
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Platform { get; set; }
        public string Status { get; set; }
        public string ManagedByOrganizationGroupId { get; set; }
        public string ManagedByOrganizationGroupName { get; set; }
        public int TotalAssigned { get; set; }
        public int Compliant { get; set; }
        public int InProgress { get; set; }
        public int Failed { get; set; }
        public string ActivationType { get; set; }
        public int Version { get; set; }
        public string DevicePolicyUuid { get; set; }
        public string ProductETag { get; set; }

        /// <summary>
        /// List of SmartGroups this product is assigned to.
        /// </summary>
        public List<SmartGroupReference> SmartGroups { get; set; }
    }

    /// <summary>
    /// Represents a SmartGroup assigned to a Workspace ONE product.
    /// </summary>
    public class SmartGroupReference
    {
        public int SmartGroupId { get; set; }
        public string Name { get; set; }
    }
}
