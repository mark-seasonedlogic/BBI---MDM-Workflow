using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    /// <summary>
    /// Container used by exported/baseline profile JSON where payloads are under:
    /// "Payload": { "payloads": [ ... ] }
    /// </summary>
    public sealed class WorkspaceOneProfilePayloadContainer
    {
        [JsonProperty("payloads", ItemConverterType = typeof(WorkspaceOnePayloadConverter))]
        public List<WorkspaceOnePayloadBase>? Payloads { get; set; }
    }
}
