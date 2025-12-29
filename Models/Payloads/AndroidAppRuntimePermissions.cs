using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads
{
    public sealed class AndroidAppRuntimePermissions
    {
        [JsonProperty("ProductId")]
        public string? ProductId { get; set; }

        [JsonProperty("Kind")]
        public string? Kind { get; set; }

        [JsonProperty("Permission")]
        public List<AndroidRuntimePermission> Permission { get; set; } = new();
    }
}
