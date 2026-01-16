using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models
{
    public sealed class WorkspaceOneApiError
    {
        [JsonProperty("errorCode")]
        public int? ErrorCode { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("activityId")]
        public string? ActivityId { get; set; }
    }
}
