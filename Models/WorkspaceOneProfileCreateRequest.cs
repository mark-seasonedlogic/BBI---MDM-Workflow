using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Transport request used to create a Workspace ONE Device Profile.
    /// Payload is intentionally flexible; the Composer/Provisioning layers will build it.
    /// </summary>
    public sealed class WorkspaceOneProfileCreateRequest
    {
        /// <summary>
        /// The JSON body WS1 expects for profile creation.
        /// </summary>
        public required JObject Payload { get; init; }

        ///<summary>
        ///WS1 platform segment used in the API endpoint URL: profiles/platforms/{PlatformSegment}/create
        /// Example: "Android", "iOS", "Windows"
        /// </summary>

        public required string PlatformSegment { get; init; }
        /// WS1 API versioned content type used throughout your service.
        /// </summary>
        public string ContentType { get; init; } = "application/json;version=2";
    }
}
