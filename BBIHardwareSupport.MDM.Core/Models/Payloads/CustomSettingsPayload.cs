using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    public sealed class CustomSettingsPayload : WorkspaceOnePayloadBase
    {
        // Often XML text under a key like "CustomSettings"
        [JsonExtensionData]
        public IDictionary<string, JToken> Data { get; set; } = new Dictionary<string, JToken>();
    }
}
