using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    /// <summary>
    /// Flattened view used in the UI for review (one row per device).
    /// </summary>
    public sealed class TimeZoneTagReviewRow
    {
        public string RestaurantCode { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
        public string? TagName { get; set; }
        public int TagId { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string? EnrollmentUserName { get; set; }
        public bool AlreadyTagged { get; set; }
        public bool IsSelected { get; set; }
    }
}
