using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads
{
    public class ApplicationControlAppRule
    {
        [JsonProperty("AppIdentifier")]
        public string? AppIdentifier { get; set; }     // package name, bundle id, etc.

        [JsonProperty("AppName")]
        public string? AppName { get; set; }

        [JsonProperty("Action")]
        public string? Action { get; set; }            // Allow/Block/etc.

        [JsonProperty("Platform")]
        public string? Platform { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? AdditionalData { get; set; }
    }
}
