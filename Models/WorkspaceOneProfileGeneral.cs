using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{ 
public sealed class WorkspaceOneProfileGeneral
{
    [JsonProperty("ProfileId")]
    public int ProfileId { get; set; }

    [JsonProperty("Name")]
    public string? Name { get; set; }

    [JsonProperty("Version")]
    public int Version { get; set; }

    [JsonProperty("ProfileScope")]
    public string? ProfileScope { get; set; }

    [JsonProperty("AssignmentType")]
    public string? AssignmentType { get; set; }

    [JsonProperty("IsActive")]
    public bool IsActive { get; set; }

    [JsonProperty("IsManaged")]
    public bool IsManaged { get; set; }

    [JsonProperty("ManagedLocationGroupID")]
    public int ManagedLocationGroupId { get; set; }

    [JsonProperty("ProfileUuid")]
    public string? ProfileUuid { get; set; }

    [JsonProperty("AssignedSmartGroups")]
    public List<WorkspaceOneSmartGroupReference>? AssignedSmartGroups { get; set; }

    [JsonProperty("ExcludedSmartGroups")]
    public List<WorkspaceOneSmartGroupReference>? ExcludedSmartGroups { get; set; }
}


}