using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads
{
    /// <summary>
    /// Fallback payload type used when we encounter a PayloadType we don't model yet.
    /// Preserves all fields for later analysis.
    /// </summary>
    public sealed class UnknownPayload : WorkspaceOnePayloadBase
    {
        [JsonExtensionData]
        public IDictionary<string, JToken>? AdditionalData { get; set; }
    }
}
