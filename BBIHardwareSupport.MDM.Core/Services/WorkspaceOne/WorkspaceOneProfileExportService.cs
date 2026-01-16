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
        private readonly ILogger<WorkspaceOneProfileExportService> _logger;
        private readonly ConcurrentDictionary<string, WorkspaceOnePayloadDetailsResponse> _payloadByUuid
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<int, JObject> _recordById
            = new(); // profiles/{id} envelope

        private readonly ConcurrentDictionary<string, Lazy<Task<WorkspaceOneProfileExport>>> _exportsByUuid
            = new(StringComparer.OrdinalIgnoreCase);

        private readonly string _tenant; // inject from config/session
        private string CacheRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "BBI-MDM-Workflow", "ws1-cache", TenantKey(_tenant));

        private string CacheFile => Path.Combine(CacheRoot, "profiles.jsonl");
        private string MetaFile => Path.Combine(CacheRoot, "meta.json");

        private static string TenantKey(string tenant) =>
            string.Concat(tenant.Trim().ToLowerInvariant()
                .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        
        public async Task LoadCacheFromDiskAsync(TimeSpan? maxAge = null, CancellationToken ct = default)
        {
            maxAge ??= TimeSpan.FromDays(1);

            if (!File.Exists(CacheFile))
            {
                _logger.LogInformation("No WS1 export cache found for Tenant={Tenant}", _tenant);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            int loaded = 0;

            using var fs = new FileStream(CacheFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8);

            while (!sr.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await sr.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                ProfileExportCacheEntry? entry;
                try
                {
                    entry = JsonConvert.DeserializeObject<ProfileExportCacheEntry>(line);
                }
                catch
                {
                    continue; // skip corrupt line
                }

                if (entry is null) continue;
                if (string.IsNullOrWhiteSpace(entry.ProfileUuid)) continue;

                // TTL check
                if (now - entry.CachedAt > maxAge.Value) continue;

                var uuid = NormUuid(entry.ProfileUuid);

                // Record cache
                try
                {
                    var recordObj = JObject.Parse(entry.RecordJson);
                    _recordById[entry.ProfileId] = recordObj;
                }
                catch
                {
                    // ignore bad record json
                }

                // Payload cache
                WorkspaceOnePayloadDetailsResponse? payload = null;
                try
                {
                    payload = JsonConvert.DeserializeObject<WorkspaceOnePayloadDetailsResponse>(entry.PayloadJson);
                    if (payload != null && string.IsNullOrWhiteSpace(payload.ProfileUuid))
                        payload.ProfileUuid = uuid;

                    if (payload != null)
                        _payloadByUuid[uuid] = payload;
                }
                catch
                {
                    // ignore bad payload json
                }

                if (payload is null) continue;

                // Seed export cache with a completed Task
                var export = new WorkspaceOneProfileExport
                {
                    ProfileId = entry.ProfileId,
                    ProfileUuid = uuid,
                    Summary = entry.Summary,
                    RecordDetailsRaw = _recordById.TryGetValue(entry.ProfileId, out var rec) ? rec : new JObject(),
                    PayloadDetails = payload
                };

                _exportsByUuid[uuid] = new Lazy<Task<WorkspaceOneProfileExport>>(() => Task.FromResult(export));
                loaded++;
            }

            _logger.LogInformation("Loaded WS1 export cache. Tenant={Tenant} ProfilesLoaded={Count} Path={Path}",
                _tenant, loaded, CacheFile);
        }
        public async Task SaveCacheToDiskAsync(CancellationToken ct = default)
        {
            Directory.CreateDirectory(CacheRoot);

            // If you want TTL/versioning later
            var meta = new
            {
                Tenant = _tenant,
                SavedAtUtc = DateTimeOffset.UtcNow,
                Version = 1
            };
            await File.WriteAllTextAsync(MetaFile, JsonConvert.SerializeObject(meta, Formatting.Indented), ct);

            // Build a snapshot of completed exports
            var entries = new List<ProfileExportCacheEntry>();

            foreach (var kvp in _exportsByUuid)
            {
                ct.ThrowIfCancellationRequested();

                var uuid = kvp.Key;

                // Only persist if Lazy has been created AND its Task is completed successfully
                if (!kvp.Value.IsValueCreated) continue;

                Task<WorkspaceOneProfileExport> task = kvp.Value.Value;
                if (!task.IsCompletedSuccessfully) continue;

                var export = task.Result;

                // Pull raw JSON from caches if possible; otherwise serialize what we have
                var recordJson = export.RecordDetailsRaw?.ToString(Formatting.None) ?? "{}";

                // Prefer original raw payload json if you choose to cache it; otherwise re-serialize payload object
                var payloadJson = JsonConvert.SerializeObject(export.PayloadDetails, Formatting.None);

                entries.Add(new ProfileExportCacheEntry
                {
                    ProfileUuid = export.ProfileUuid,
                    ProfileId = export.ProfileId,
                    Summary = export.Summary,
                    RecordJson = recordJson,
                    PayloadJson = payloadJson,
                    CachedAt = DateTimeOffset.UtcNow
                });
            }

            // Write JSONL atomically
            var tmp = CacheFile + ".tmp";
            await using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            await using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                foreach (var entry in entries)
                {
                    ct.ThrowIfCancellationRequested();
                    var line = JsonConvert.SerializeObject(entry, Formatting.None);
                    await sw.WriteLineAsync(line);
                }
            }

            File.Copy(tmp, CacheFile, overwrite: true);
            File.Delete(tmp);

            _logger.LogInformation("Saved WS1 export cache. Tenant={Tenant} Profiles={Count} Path={Path}",
                _tenant, entries.Count, CacheFile);
        }

        public Task<WorkspaceOneProfileExport> GetExportAsync(WorkspaceOneProfileSummary summary, CancellationToken ct = default)
        {
            var uuid = NormUuid(summary.ProfileUuid);
            if (string.IsNullOrWhiteSpace(uuid))
                throw new InvalidOperationException($"ProfileUuid missing for ProfileId {summary.ProfileId}.");

            if (_exportsByUuid.TryGetValue(uuid, out var existing))
            {
                _logger.LogDebug("ProfileExport cache HIT: {ProfileName} (Id {ProfileId}, Uuid {Uuid})",
                    summary.ProfileName, summary.ProfileId, uuid);

                return existing.Value;
            }

            var lazy = _exportsByUuid.GetOrAdd(uuid, _ =>
                new Lazy<Task<WorkspaceOneProfileExport>>(() =>
                {
                    _logger.LogInformation("ProfileExport BUILD (cache MISS): {ProfileName} (Id {ProfileId}, Uuid {Uuid})",
                        summary.ProfileName, summary.ProfileId, uuid);

                    return BuildExportInternalAsync(summary, uuid, ct);
                }));

            return lazy.Value;
        }
        private async Task<JObject> GetRecordAsync(int profileId, CancellationToken ct)
        {
            if (_recordById.TryGetValue(profileId, out var cached))
                return cached;

            var raw = await _profileService.GetProfileDetailsRawAsync(profileId, ct);
            var obj = JObject.Parse(raw);

            _recordById[profileId] = obj;
            return obj;
        }
        public async Task PreloadAsync(IEnumerable<WorkspaceOneProfileSummary> profiles, CancellationToken ct = default)
        {
            // Throttle so you don’t hammer WS1
            using var throttler = new SemaphoreSlim(6);

            var tasks = profiles.Select(async p =>
            {
                await throttler.WaitAsync(ct);
                try
                {
                    await GetExportAsync(p, ct);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task<WorkspaceOnePayloadDetailsResponse> GetPayloadAsync(string profileUuid, CancellationToken ct)
        {
            if (_payloadByUuid.TryGetValue(profileUuid, out var cached))
                return cached;

            var raw = await _profileService.GetProfilePayloadDetailsRawAsync(profileUuid, ct);

            var resp = JsonConvert.DeserializeObject<WorkspaceOnePayloadDetailsResponse>(raw)
                       ?? throw new InvalidOperationException("Failed to deserialize payload-details response.");

            // payload-details sometimes has uuid mapped to ProfileUuid; enforce it
            if (string.IsNullOrWhiteSpace(resp.ProfileUuid))
                resp.ProfileUuid = profileUuid;

            _payloadByUuid[profileUuid] = resp;
            return resp;
        }

        private static string NormUuid(string uuid) => uuid.Trim().Trim('{', '}');
        public WorkspaceOneProfileExportService(IWorkspaceOneProfileService profileApiService, ILogger<WorkspaceOneProfileExportService> logger)
        {
            _profileService = profileApiService;
            _logger = logger;
        }
        public void ClearCache()
        {
            _exportsByUuid.Clear();
            _logger.LogInformation("Workspace ONE profile export cache cleared.");
        }
        private async Task<WorkspaceOneProfileExport> BuildExportInternalAsync(
            WorkspaceOneProfileSummary summary,
            string uuid,
            CancellationToken ct)
        {
            JObject? recordObj = null;

            try
            {
                recordObj = await GetRecordAsync(summary.ProfileId, ct);
            }
            catch (WorkspaceOneApiException ex) when (
                ex.HttpStatusCode == 400 &&
                (ex.ApiError?.Message?.IndexOf("Invalid Payload Key", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                _logger.LogWarning(
                    "ProfileId {ProfileId}: {Endpoint}} failed with Invalid Payload Key. Continuing with payload-details only. ActivityId={ActivityId}",
                    summary.ProfileId,
                    $"/mdm/profiles/{summary.ProfileId}",
                    ex.ApiError?.ActivityId);
            }
            var payload = await GetPayloadAsync(uuid, ct);

            return new WorkspaceOneProfileExport
            {
                ProfileId = summary.ProfileId,
                ProfileUuid = uuid,
                Summary = summary,
                RecordDetailsRaw = recordObj,
                PayloadDetails = payload
            };
        }
        private async Task<WorkspaceOneProfileExport> BuildInternalAsync(
            WorkspaceOneProfileSummary summary,
            CancellationToken ct)
        {
            // record/details
            var recordJson = await _profileService.GetProfileDetailsRawAsync(summary.ProfileId, ct);
            var recordObj = JObject.Parse(recordJson);

            // prefer uuid from record General if present, else summary uuid
            var uuid = recordObj["General"]?["ProfileUuid"]?.Value<string>() ?? summary.ProfileUuid;
            uuid = NormUuid(uuid);

            // payload-details
            var payloadJson = await _profileService.GetProfilePayloadDetailsRawAsync(uuid, ct);

            var payloadResp = JsonConvert.DeserializeObject<WorkspaceOnePayloadDetailsResponse>(payloadJson)
                              ?? throw new InvalidOperationException("Failed to deserialize payload-details response.");

            if (string.IsNullOrWhiteSpace(payloadResp.ProfileUuid))
                payloadResp.ProfileUuid = uuid;

            return new WorkspaceOneProfileExport
            {
                ProfileId = summary.ProfileId,
                ProfileUuid = uuid,
                Summary = summary,
                RecordDetailsRaw = recordObj,
                PayloadDetails = payloadResp
            };
        }

    }
}
