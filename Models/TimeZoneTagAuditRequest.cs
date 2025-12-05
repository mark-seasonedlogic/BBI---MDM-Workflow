using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Input for the audit service; mirrors the PowerShell parameters but
    /// feeds the already-loaded device list from the device service.
    /// </summary>
    public sealed class TimeZoneTagAuditRequest
    {
        public string MasterCsvPath { get; set; } = string.Empty;
        public string RestaurantsPath { get; set; } = string.Empty;
        public string? RestaurantsColumn { get; set; }

        /// <summary>
        /// Full device list for the OG (from IWorkspaceOneDeviceService).
        /// </summary>
        public IReadOnlyList<WorkspaceOneDevice> Devices { get; set; } =
            Array.Empty<WorkspaceOneDevice>();

        public int OgId { get; set; }

        /// <summary>
        /// Default batch size used later for BulkAddDevicesToTagAsync; kept here
        /// so you can reuse your existing PowerShell defaults (e.g., 200).
        /// </summary>
        public int BatchSize { get; set; } = 200;
    }
}
