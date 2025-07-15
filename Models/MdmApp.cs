using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Generic.Models
{
    /// <summary>
    /// Represents an MDM-managed app (store, LOB, or VPP) with core metadata.
    /// </summary>
    public class MdmApp
    {
        public string Id { get; set; }

        /// <summary>
        /// Display name of the application.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Publisher or vendor of the application.
        /// </summary>
        public string Publisher { get; set; }

        /// <summary>
        /// The platform this app is targeting (e.g., Android, iOS).
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Type of application (e.g., Managed, Store, Line-of-Business).
        /// </summary>
        public string AppType { get; set; }
    }


}
