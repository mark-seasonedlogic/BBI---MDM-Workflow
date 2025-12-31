using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    public sealed class WiFiPayload : WorkspaceOnePayloadBase
    {
        [JsonProperty("SSID_STR")]
        public string? Ssid { get; set; }

        [JsonProperty("EncryptionType")]
        public string? EncryptionType { get; set; }

        [JsonProperty("IsHidden")]
        public bool? IsHidden { get; set; }

        [JsonProperty("MakeActive")]
        public bool? MakeActive { get; set; }

        [JsonProperty("Password")]
        public string? Password { get; set; }

        [JsonProperty("EnterprisePassword")]
        public string? EnterprisePassword { get; set; }
    }

}
