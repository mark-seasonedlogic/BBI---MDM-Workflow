using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Generic.Models
{
    /// <summary>
    /// Represents a generic MDM device configuration profile, including platform-agnostic settings.
    /// </summary>
    public class MdmDeviceConfiguration
    {
        public string Id { get; set; }

        /// <summary>
        /// Display name of the configuration profile.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The platform this configuration targets (e.g., Android, iOS, Windows).
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Optional description of the configuration profile.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Key-value pairs representing settings within the configuration.
        /// </summary>
        public Dictionary<string, object> Settings { get; set; }
    }


}
