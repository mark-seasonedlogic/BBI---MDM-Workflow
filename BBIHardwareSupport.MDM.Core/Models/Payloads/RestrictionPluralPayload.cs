using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    public sealed class RestrictionPluralPayload : WorkspaceOnePayloadBase
    {
        // This payload is huge in practice; don’t block yourself modeling every knob up front.
        // Keep it lossless:
        [JsonExtensionData]
        public IDictionary<string, JToken> Settings { get; set; } = new Dictionary<string, JToken>();
    }
}
