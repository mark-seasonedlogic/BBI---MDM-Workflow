using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models.Payloads
{
    public sealed class WorkspaceOnePayloadDetailsResponse
    {
        // WS1 payload-details uses "uuid" (your sample), NOT ProfileUuid
        [JsonProperty("uuid")]
        public string ProfileUuid { get; set; } = "";

        [JsonProperty("unique_key")]
        public string? UniqueKey { get; set; }

        [JsonProperty("context")]
        public string? Context { get; set; }

        [JsonProperty("configuration_type")]
        public string? ConfigurationType { get; set; } // "Device" etc.

        [JsonProperty("device_type")]
        public string? DeviceType { get; set; } // "Android" etc.

        [JsonProperty("managed_by_og")]
        public string? ManagedByOg { get; set; }

        // Top-level payload list
        [JsonProperty("payloads", ItemConverterType = typeof(WorkspaceOnePayloadConverter))]
        public List<WorkspaceOnePayloadBase>? Payloads { get; set; }
    }

}
