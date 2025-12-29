using Newtonsoft.Json;
using System.Collections.Generic;
using BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Container used by exported/baseline profile JSON where payloads are under:
    /// "Payload": { "payloads": [ ... ] }
    /// </summary>
    public sealed class WorkspaceOneProfilePayloadContainer
    {
        [JsonProperty("payloads")]
        public List<WorkspaceOnePayloadBase>? Payloads { get; set; }
    }
}
