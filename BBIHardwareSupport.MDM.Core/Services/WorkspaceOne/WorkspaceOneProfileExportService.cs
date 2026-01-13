using BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models;
using BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models.Payloads;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public class WorkspaceOneProfileExportService: IWorkspaceOneProfileExportService
    {
        private readonly IWorkspaceOneProfileService _profileService;
        private readonly ILogger<IWorkspaceOneProfileExportService> _logger;
        private readonly ConcurrentDictionary<string, Lazy<Task<WorkspaceOneProfileExport>>> _exportsByUuid
            = new(StringComparer.OrdinalIgnoreCase);

        private static string NormUuid(string uuid) => uuid.Trim().Trim('{', '}');
        public WorkspaceOneProfileExportService(IWorkspaceOneProfileService profileApiService, ILogger<IWorkspaceOneProfileExportService> logger)
        {
            _profileService = profileApiService;
            _logger = logger;
        }
        public async Task<WorkspaceOneProfileExport> BuildProfileExportAsync(
    WorkspaceOneProfileSummary summary,
    CancellationToken ct)
        {
            // A) record/details envelope by ProfileId
            var recordJson = await _profileService.GetProfileDetailsRawAsync(summary.ProfileId, ct);
            var recordObj = JObject.Parse(recordJson);

            // Extract UUID from record General (authoritative for join)
            var uuidFromRecord =
                recordObj["General"]?["ProfileUuid"]?.Value<string>() ??
                summary.ProfileUuid;

            // B) payload-details by UUID (preferred)
            var payloadJson = await _profileService.GetProfilePayloadDetailsRawAsync(uuidFromRecord, ct);
            var payloadResp = JsonConvert.DeserializeObject<WorkspaceOnePayloadDetailsResponse>(payloadJson)
                             ?? throw new InvalidOperationException("Failed to deserialize payload-details response.");

            // payload-details uses "uuid" -> ProfileUuid. If missing, set it.
            if (string.IsNullOrWhiteSpace(payloadResp.ProfileUuid))
                payloadResp.ProfileUuid = uuidFromRecord;

            return new WorkspaceOneProfileExport
            {
                ProfileId = summary.ProfileId,
                ProfileUuid = uuidFromRecord,
                Summary = summary,
                RecordDetailsRaw = recordObj,
                PayloadDetails = payloadResp
            };
        }

    }
}
