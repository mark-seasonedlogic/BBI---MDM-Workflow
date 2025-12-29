using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Represents Workspace ONE profile details.
    ///
    /// This model supports TWO shapes you've encountered:
    ///  1) API profile-details (v2): { "General": { ... }, "AndroidForWorkPermissions": { ... }, ... }
    ///  2) Baseline/exported profiles: { "General": { ... }, "Payload": { "payloads": [ ... ] } }
    ///
    /// Unknown sections are captured in <see cref="AdditionalSections"/> for later modeling.
    /// </summary>
    public sealed class WorkspaceOneProfileDetails
    {
        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("General")]
        public WorkspaceOneProfileGeneral? General { get; set; }

        // Baseline/exported JSON shape
        [JsonProperty("Payload")]
        public WorkspaceOneProfilePayloadContainer? Payload { get; set; }

        // API details (v2) shape
        [JsonProperty("AndroidForWorkPermissions")]
        public AndroidForWorkPermissions? AndroidForWorkPermissions { get; set; }

        [JsonProperty("ApplicationControlPayload")]
        public ApplicationControlPayload? ApplicationControlPayload { get; set; }


        /// <summary>
        /// Captures any additional sections returned by Workspace ONE that we haven't modeled yet
        /// (e.g., AndroidForWorkCustomSettingsList, AndroidForWorkCustomSettings, etc.).
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, JToken>? AdditionalSections { get; set; }
    }
}
