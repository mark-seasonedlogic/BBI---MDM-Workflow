
namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models
{
    public class WorkspaceOneProfileSummary
    {
        public int ProfileId { get; set; }
        public string ProfileName { get; set; } = string.Empty;
        public string ManagedBy { get; set; } = string.Empty;
        public int OrganizationGroupId { get; set; }
        public string OrganizationGroupUuid { get; set; } = string.Empty;
        public string ProfileStatus { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string AssignmentType { get; set; } = string.Empty;
        public List<WorkspaceOneSmartGroupReference> AssignmentSmartGroups { get; set; } = new();
        public List<WorkspaceOneSmartGroupReference> ExcludedSmartGroups { get; set; } = new();
        public string ProfileType { get; set; } = string.Empty;
        public string ProfileUuid { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }

}
