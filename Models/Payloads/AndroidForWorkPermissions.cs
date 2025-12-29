using Newtonsoft.Json;
using System.Collections.Generic;
using BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Workspace ONE "AndroidForWorkPermissions" section from profile details (v2).
    /// Distinct from the "Permission.plural" payload type.
    /// </summary>
    public sealed class AndroidForWorkPermissions
    {
        [JsonProperty("MasterRuntimePermission")]
        public string? MasterRuntimePermission { get; set; }

        [JsonProperty("AppLevelRuntimePermissionsList")]
        public List<AndroidAppRuntimePermissions>? AppLevelRuntimePermissionsList { get; set; }
    }
}
