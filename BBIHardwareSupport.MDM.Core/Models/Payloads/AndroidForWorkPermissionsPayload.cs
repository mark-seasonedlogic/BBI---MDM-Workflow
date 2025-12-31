using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    /// <summary>
    /// PayloadType = "Permission.plural"
    /// </summary>
    public sealed class AndroidForWorkPermissionsPayload : WorkspaceOnePayloadBase
    {
        [JsonProperty("MasterRuntimePermission")]
        public string? MasterRuntimePermission { get; set; }

        // In your baseline JSON this is a JSON string containing a list.
        // In some responses it may be an actual array/object.
        [JsonProperty("AppLevelRuntimePermissions")]
        [JsonConverter(typeof(AppLevelRuntimePermissionsConverter))]
        public List<AndroidAppRuntimePermissions> AppLevelRuntimePermissionsList { get; set; } = new();
    }
}
