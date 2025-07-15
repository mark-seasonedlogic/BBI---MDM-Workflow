using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Generic.Models
{
    /// <summary>
    /// Represents a generic enrollment configuration or restriction rule.
    /// </summary>
    public class MdmEnrollmentConfiguration
    {
        public string Id { get; set; }

        /// <summary>
        /// Display name of the enrollment configuration.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Type of enrollment configuration (e.g., platform restriction, automatic assignment).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Arbitrary key-value property map for configuration content.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; }
    }

}
