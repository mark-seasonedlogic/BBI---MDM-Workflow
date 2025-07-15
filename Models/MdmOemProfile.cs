using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Generic.Models
{
    /// <summary>
    /// Represents a vendor-specific OEMConfig profile including raw configuration data.
    /// </summary>
    public class MdmOemProfile
    {
        public string Id { get; set; }

        /// <summary>
        /// Display name of the OEM configuration profile.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The vendor name (e.g., Zebra, Samsung).
        /// </summary>
        public string Vendor { get; set; }

        /// <summary>
        /// Raw payload data from the OEM configuration.
        /// </summary>
        public Dictionary<string, object> Payload { get; set; }
    }


}
