using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Generic.Models
{
    /// <summary>
    /// Represents a generic MDM compliance policy and its platform-specific rules.
    /// </summary>
    public class MdmCompliancePolicy
    {
        public string Id { get; set; }

        /// <summary>
        /// Display name of the compliance policy.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The target platform for this policy (e.g., Android, iOS).
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Compliance rules and thresholds expressed as key-value pairs.
        /// </summary>
        public Dictionary<string, object> Rules { get; set; }
    }


}
