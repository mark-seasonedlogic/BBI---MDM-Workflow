using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    public class WorkspaceOneProfileDetails
    {
        public int ProfileId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Platform { get; set; } // e.g., Android, iOS
        public string ProfileType { get; set; } // e.g., Restrictions, Wi-Fi
        public string ProfileScope { get; set; } // e.g., Production, Test
        public int Version { get; set; }
        public string AssignmentType { get; set; } // Auto or Manual
        public bool IsActive { get; set; }
        public bool IsManaged { get; set; }
        public string AllowRemoval { get; set; } // e.g., "WithAuthorization"

        // Organizational context
        public int OrganizationGroupId { get; set; }
        public string OrganizationGroupName { get; set; }

        // Assignment information
        public List<WorkspaceOneSmartGroupReference> AssignedSmartGroups { get; set; } = new();
        public List<WorkspaceOneSmartGroupReference> ExcludedSmartGroups { get; set; } = new();

        // Optional raw payload from Workspace ONE
        public object Payload { get; set; }

        // For Git/package tracking
        public string PackageTag { get; set; } // e.g., "android-base-package"
        public string CommitHash { get; set; } // optional for Git traceability

        // Timestamps or audit metadata
        public DateTime? LastModified { get; set; }
        public string LastModifiedBy { get; set; }
    }

}
