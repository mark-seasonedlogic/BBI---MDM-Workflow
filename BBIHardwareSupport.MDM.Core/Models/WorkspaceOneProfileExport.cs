using BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models.Payloads;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models.Payloads;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models
{
    public sealed class WorkspaceOneProfileExport
    {
        // Identity
        public int ProfileId { get; set; }
        public string ProfileUuid { get; set; } = "";

        // Keep the stable summary you already have
        public WorkspaceOneProfileSummary Summary { get; set; } = default!;

        // Raw record/details envelope (profiles/{profileId}) — varies by profile type
        public JObject RecordDetailsRaw { get; set; } = new();

        // Parsed payload-details response (the one you just added)
        public WorkspaceOnePayloadDetailsResponse PayloadDetails { get; set; } = new();

        // Optional: derived helpers (nice for debugging/display)
        public List<WorkspaceOnePayloadBase> Payloads =>
            PayloadDetails.Payloads ?? new List<WorkspaceOnePayloadBase>();
    }

}
