using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads
{
    /// <summary>
    /// WS1 "ApplicationControlPayload" (Android restrictions / app control).
    /// Exact fields vary by WS1 version and profile template, so keep this tolerant.
    /// </summary>
    public class ApplicationControlPayload
    {
        // Many payloads include a "PayloadType" string in your baseline JSON set
        [JsonProperty("PayloadType")]
        public string? PayloadType { get; set; }

        // Common WS1 keys you may see (names can vary); keep nullable.
        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        // These are *typical* patterns in WS1 application control payloads.
        // Your baseline JSON will confirm exact property names.
        [JsonProperty("Allow")]
        public bool? Allow { get; set; }

        [JsonProperty("Whitelist")]
        public List<ApplicationControlAppRule>? Whitelist { get; set; }

        [JsonProperty("Blacklist")]
        public List<ApplicationControlAppRule>? Blacklist { get; set; }

        [JsonProperty("Rules")]
        public List<ApplicationControlAppRule>? Rules { get; set; }

        // Catch-all for unknown fields so deserialization never yields "all null"
        [JsonExtensionData]
        public IDictionary<string, object>? AdditionalData { get; set; }
    }

    
}
