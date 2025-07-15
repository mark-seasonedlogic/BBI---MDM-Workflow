using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Generic.Models
{
    /// <summary>
    /// Represents app configuration settings for a specific managed application.
    /// </summary>
    public class MdmAppConfiguration
    {
        public string Id { get; set; }

        /// <summary>
        /// The ID of the associated app this configuration applies to.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Display name of the configuration.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Key-value map of configuration properties.
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; }
    }


}
