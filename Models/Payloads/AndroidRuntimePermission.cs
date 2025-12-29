using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads
{
    public sealed class AndroidRuntimePermission
    {
        [JsonProperty("PermissionId")]
        public string? PermissionId { get; set; }

        // Present in some API responses (not always in baseline payloads)
        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("PermissionOption")]
        public int? PermissionOption { get; set; }
    }
}
