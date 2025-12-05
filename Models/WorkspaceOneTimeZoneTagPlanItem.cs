using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Per-restaurant plan record produced by the audit (Invoke-WS1TimeZoneTagAudit).
    /// </summary>
    public sealed class WorkspaceOneTimeZoneTagPlanItem
    {
        public string RestaurantCode { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
        public string? TagName { get; set; }
        public int? TagId { get; set; }
        public int DeviceCount { get; set; }
        public IReadOnlyList<string> DeviceIds { get; set; } = Array.Empty<string>();
        public string? Notes { get; set; }
    }
}
