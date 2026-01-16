using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Models
{
    public sealed class ProfileExportCacheEntry
    {
        public string ProfileUuid { get; set; } = "";
        public int ProfileId { get; set; }
        public WorkspaceOneProfileSummary Summary { get; set; } = default!;
        public string RecordJson { get; set; } = "";
        public string PayloadJson { get; set; } = "";
        public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;
    }

}
