using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models
{
    public class DeviceRemovalResult
    {
        public string SerialNumber { get; set; }
        public string StoreTag1 { get; set; }
        public string StoreTag2 { get; set; }
        public string StateTag1 { get; set; } = "AutoRemoved-04-14-2025";
        public string StateTag2 { get; set; }
    }
}
