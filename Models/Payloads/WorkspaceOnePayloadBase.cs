using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models.Payloads
{
    [JsonConverter(typeof(WorkspaceOnePayloadConverter))]
    public abstract class WorkspaceOnePayloadBase
    {
        [JsonProperty("PayloadType")]
        public string? PayloadType { get; set; }
    }







}
