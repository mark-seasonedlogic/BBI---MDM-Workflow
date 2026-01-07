using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models
{
    public sealed class WorkspaceOneProfileRecordEnvelope
    {
        public JObject Raw { get; set; } = new();
        public string ProfileUuid { get; set; } = "";
        public int ProfileId { get; set; }
        public Dictionary<string, JToken> Sections { get; set; } = new();
    }

}
