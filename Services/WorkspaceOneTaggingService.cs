using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Interfaces;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Services
{
    public class WorkspaceOneTaggingService : WorkspaceOneServiceBase, IWorkspaceOneTaggingService
    {
        private readonly ILogger<WorkspaceOneTaggingService> _logger;

        public WorkspaceOneTaggingService(
            HttpClient httpClient,
            IWorkspaceOneAuthService authService,
            ILogger<WorkspaceOneTaggingService> logger)
            : base(httpClient, authService)
        {
            _logger = logger;
        }

        public async Task<List<WorkspaceOneTimeZoneTagPlanItem>> InvokeTimeZoneTagAuditAsync(
            TimeZoneTagAuditRequest request,
            CancellationToken cancellationToken = default)
        {
            // 1) Build master index RestaurantCode -> TimeZone
            var masterIndex = BuildMasterIndex(request.MasterCsvPath);

            // 2) Get restaurants to check
            var restaurants = LoadRestaurantsList(
                request.RestaurantsPath,
                request.RestaurantsColumn);

            // 3) Devices by restaurant from the already-loaded devices list
            var devByRestaurant = BuildDevicesByRestaurant(request.Devices);

            // 4) Load tag lookup, and 5) map timezones to tag names / IDs.
            //    This is where you mirror your PowerShell helpers:
            //    Get-WS1TagLookupV1 + Resolve-WS1TimeZoneTag.
            var tagLookup = await GetTagLookupAsync(request.OgId, cancellationToken);

            var plan = new List<WorkspaceOneTimeZoneTagPlanItem>();

            foreach (var rc in restaurants.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!masterIndex.TryGetValue(rc, out var timeZone) || string.IsNullOrWhiteSpace(timeZone))
                    continue;
                /*
                ResolveTimeZoneTagAsync(
    string timeZoneValue,
    TagLookup tagLookup,
    int ogId,
    string? knownTagsPath = null,
    bool ensure = false,
    string tagNameTemplate = "{0}",
    CancellationToken cancellationToken = default)*/
                var tagResolution = await ResolveTimeZoneTagAsync(
                    timeZone,
                    tagLookup,
                    request.OgId,
                    null,
                    false,
                    "{0}",
                    cancellationToken);

                devByRestaurant.TryGetValue(rc, out var devicesForRc);
                devicesForRc ??= new List<string>();


                string? notes =
                    timeZone is null ? "No timezone in master data"
                    : string.IsNullOrWhiteSpace(tagResolution.TagName) ? "Unable to map timezone to tag"
                    : !tagResolution.TagId.HasValue ? "Tag does not exist (create first)"
                    : devicesForRc.Count == 0 ? "No devices matched by EnrollmentUserName"
                    : null;

                plan.Add(new WorkspaceOneTimeZoneTagPlanItem
                {
                    RestaurantCode = rc,
                    TimeZone = timeZone,
                    TagName = tagResolution.TagName,
                    TagId = tagResolution.TagId,
                    DeviceCount = devicesForRc.Count,
                    DeviceIds = devicesForRc,
                    Notes = notes
                });
            }

            return plan;
        }

        public async Task<HashSet<string>> GetDevicesForTagAsync(
    int tagId,
    int pageSize,
    CancellationToken cancellationToken = default)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int page = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var queryParams = new Dictionary<string, string>
                {
                    ["pagesize"] = pageSize.ToString(),
                    ["page"] = page.ToString()
                };

                // NOTE: itemType must match the JSON array name:
                // e.g., "Device", "Devices", or whatever the endpoint actually returns.
                var items = await GetPagedResponseAsync(
                    $"/mdm/tags/{tagId}/devices",
                    "Device",                  // <-- adjust to actual field: "Devices" if needed
                    queryParams,
                    "application/json;version=2");

                if (items == null || items.Count == 0)
                    break;

                // Remember how many we had before this page
                int beforeCount = result.Count;

                foreach (JObject j in items)
                {
                    var devId = j["DeviceId"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(devId))
                        result.Add(devId);
                }

                int afterCount = result.Count;

                // If no new IDs were added by this page, assume the API
                // is just returning the same full set every time -> stop.
                if (afterCount == beforeCount)
                    break;

                // If the API *does* honor paging and returns less than the requested page size,
                // we know we hit the last page.
                if (items.Count < pageSize)
                    break;

                page++;
            }

            return result;
        }


        public async Task BulkAddDevicesToTagAsync(
            int tagId,
            IReadOnlyCollection<string> deviceIds,
            int batchSize,
            CancellationToken cancellationToken = default)
        {
            if (deviceIds == null || deviceIds.Count == 0)
                return;

            var idList = deviceIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            for (var i = 0; i < idList.Count; i += batchSize)
            {
                var chunk = idList.Skip(i).Take(batchSize).ToList();

                var payload = new
                {
                    BulkValues = chunk.Select(id => new { DeviceId = id }).ToList()
                };

                try
                {
                    await PostJsonAsync(
    $"/mdm/tags/{tagId}/devices/bulk",
    payload,
    "application/json;version=2");

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to add {Count} devices to tag {TagId} in batch starting at index {Index}",
                        chunk.Count, tagId, i);
                    throw;
                }
            }
        }

        #region Private helpers (mirror PowerShell utilities)

        private static Dictionary<string, string> BuildMasterIndex(string masterCsvPath)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var allLines = File.ReadAllLines(masterCsvPath);
            if (allLines.Length == 0)
                throw new InvalidOperationException("Master CSV is empty.");

            // --- Parse header and locate required columns ---
            var header = SplitCsvLine(allLines[0]); // quote-aware split

            int conceptIndex = FindColumnIndex(header, "CONCEPT_RSTRNT_CD");
            int timeZoneIndex = FindColumnIndex(header, "TIME_ZN_NAME");

            if (conceptIndex < 0)
                throw new InvalidOperationException("Master CSV missing column: CONCEPT_RSTRNT_CD");

            if (timeZoneIndex < 0)
                throw new InvalidOperationException("Master CSV missing column: TIME_ZN_NAME");

            // --- Iterate data rows ---
            foreach (var line in allLines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = SplitCsvLine(line); // preserves "" as empty strings

                // Make sure we have enough columns for the indexes we located
                if (parts.Length <= conceptIndex || parts.Length <= timeZoneIndex)
                    continue;

                var conceptRestaurant = parts[conceptIndex].Trim(); // e.g., 1_567 or OBS1234
                var timeZoneName = parts[timeZoneIndex].Trim();

                var restaurantCode = ConvertToRestaurantCode(conceptRestaurant);

                if (!string.IsNullOrWhiteSpace(restaurantCode) &&
                    !string.IsNullOrWhiteSpace(timeZoneName))
                {
                    dict[restaurantCode] = timeZoneName;
                }
            }

            return dict;
        }
        /// <summary>
        /// Case-insensitive search for a column name in the header.
        /// </summary>
        private static int FindColumnIndex(string[] header, string columnName)
        {
            for (int i = 0; i < header.Length; i++)
            {
                if (string.Equals(header[i]?.Trim(), columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Minimal CSV splitter; replace with a proper CSV parser if you have commas in fields.
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Toggle in/out of quotes, but don't include the quote itself
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    // End of field
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            // Add last field
            fields.Add(sb.ToString());

            return fields.ToArray();
        }


        private static List<string> LoadRestaurantsList(string path, string? columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                // Simple text list, one restaurant code per line.
                return File.ReadAllLines(path)
                    .Select(l => l.Trim())
                    .Where(l => !string.IsNullOrWhiteSpace(l))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            // CSV with a specific column.
            var result = new List<string>();
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                return result;

            var header = lines[0].Split(',');
            var index = Array.FindIndex(header, h =>
                string.Equals(h, columnName, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                throw new InvalidOperationException($"Restaurants CSV missing column: {columnName}");

            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split(',');
                if (parts.Length <= index)
                    continue;

                var rc = parts[index].Trim();
                if (!string.IsNullOrWhiteSpace(rc))
                    result.Add(rc);
            }

            return result;
        }

        private static Dictionary<string, List<string>> BuildDevicesByRestaurant(
            IReadOnlyList<WorkspaceOneDevice> devices)
        {
            // Mirrors Get-WS1DevicesByRestaurant (EnrollmentUserName equality).
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var rx = new System.Text.RegularExpressions.Regex(@"^(OBS|BFG|CIG|FLM)(\d{4}).*", System.Text.RegularExpressions.RegexOptions.Compiled);
            foreach (var d in devices)
            {
                var username = d.UserName;
                if (string.IsNullOrWhiteSpace(username))
                    continue;
                var match = rx.Match(username);
                if(!match.Success)
                    continue;

                var restaurantCode = match.Groups[1].Value + match.Groups[2].Value;
                if (!map.TryGetValue(restaurantCode, out var list))
                {
                    list = new List<string>();
                    map[restaurantCode] = list;
                }

                list.Add(d.DeviceId.ToString()); // or DeviceId property, depending on your model
            }

            return map;
        }

        private async Task<TagLookup> GetTagLookupAsync(
    int ogId,
    CancellationToken cancellationToken)
        {
            // Page size chosen to match your PowerShell default
            const int defaultPageSize = 500;

            // Equivalent of: $all = Get-WS1TagsByOrgV1 -OgId $OgId ...
            var allTags = await GetTagsByOrgV1Async(ogId, defaultPageSize, cancellationToken);

            var byId = new Dictionary<int, TagInfo>();
            var byName = new Dictionary<string, TagInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in allTags)
            {
                if (t.Id is int id && !byId.ContainsKey(id))
                {
                    byId[id] = t;
                }

                if (!string.IsNullOrWhiteSpace(t.Name) && !byName.ContainsKey(t.Name))
                {
                    byName[t.Name] = t;
                }
            }

            return new TagLookup(allTags, byId, byName);
        }


/* 
    private async Task<(string? TagName, int? TagId)> ResolveTimeZoneTagAsync(
            string timeZoneName,
            int ogId,
            TagLookup lookup,
            CancellationToken cancellationToken)
        {
            // TODO: Implement the mapping between TIME_ZN_NAME and tag naming convention,
            // reusing the same logic as Resolve-WS1TimeZoneTag in PowerShell.
            return await Task.FromResult<(string?, int?)>((null, null));
        }
*/        
        
        private static string ConvertToRestaurantCode(string conceptRestaurantCode)
        {
            var conceptMap = new Dictionary<string, string>
        {
            {"1", "OBS"},
            {"2", "FLM"},
            {"6", "BFG"},
            {"7", "CIG"},
        };

            // TODO: mirror Convert-ToRestaurantCode from PowerShell.
            if (String.IsNullOrEmpty(conceptRestaurantCode))
                return string.Empty;
            var parts = conceptRestaurantCode.Split('_');
            if(parts.Length != 2)
                return string.Empty;
            var brand = conceptMap.GetValueOrDefault(parts[0]);
            var number = parts[1];
            return string.Format("{0}{1}",brand,number.PadLeft(4,'0'));
        }
        private sealed class TagInfo
        {
            public int Id { get; init; }
            public string? Name { get; init; }
            public string? Uuid { get; init; }
            public string? TagType { get; init; }
            public int? LocationGroupId { get; init; }
            public string? Description { get; init; }
            public DateTimeOffset? CreatedOn { get; init; }
            public DateTimeOffset? UpdatedOn { get; init; }
            public JObject Raw { get; init; } = new JObject();
        }
        public sealed class ResolvedTimeZoneTagResult
        {
            public string TagName { get; init; } = string.Empty;
            public int? TagId { get; init; }
        }
        private async Task<ResolvedTimeZoneTagResult?> ResolveTimeZoneTagAsync(
    string timeZoneValue,
    TagLookup tagLookup,
    int ogId,
    string? knownTagsPath = null,
    bool ensure = false,
    string tagNameTemplate = "{0}",
    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(timeZoneValue))
                return null;

            //
            // 1) Mapping table (PowerShell equivalent)
            //
            var tzToTagName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Eastern Standard Time"] = "Eastern",
                ["America/New_York"] = "Eastern",
                ["EST"] = "Eastern",

                ["Central Standard Time"] = "Central",
                ["America/Chicago"] = "Central",
                ["CST"] = "Central",

                ["Mountain Standard Time"] = "Mountain",
                ["America/Denver"] = "Mountain",
                ["MST"] = "Mountain",
                ["America/Phoenix"] = "Arizona",

                ["Pacific Standard Time"] = "Pacific",
                ["America/Los_Angeles"] = "Pacific",
                ["PST"] = "Pacific",

                ["Hawaii"] = "Hawaii",
                ["Alaska"] = "Alaska",
                ["Arizona"] = "Arizona"
            };

            //
            // 2) Direct match from table
            //
            if (!tzToTagName.TryGetValue(timeZoneValue, out string? targetTagName))
            {
                // fuzzy fallback (PowerShell -like logic)
                var t = timeZoneValue.ToLowerInvariant();

                if (t.Contains("eastern")) targetTagName = "Eastern";
                else if (t.Contains("central")) targetTagName = "Central";
                else if (t.Contains("mountain")) targetTagName = "Mountain";
                else if (t.Contains("pacific")) targetTagName = "Pacific";
                else if (t.Contains("hawai")) targetTagName = "Hawaii";
                else if (t.Contains("alaska")) targetTagName = "Alaska";
                else if (t.Contains("arizona")) targetTagName = "Arizona";
            }

            if (string.IsNullOrWhiteSpace(targetTagName))
                return null;

            //
            // 3) Load KnownTags JSON if present (PS: $KnownTagsParam)
            //
            List<TagInfo>? knownTagsFromJson = null;

            if (!string.IsNullOrWhiteSpace(knownTagsPath))
            {
                if (!File.Exists(knownTagsPath))
                    throw new FileNotFoundException($"KnownTagsPath not found: {knownTagsPath}");

                var raw = await File.ReadAllTextAsync(knownTagsPath, cancellationToken);
                var arr = JArray.Parse(raw);
                knownTagsFromJson = arr
                    .OfType<JObject>()
                    .Select(ConvertJsonToTagInfo)
                    .Where(x => x != null)
                    .ToList()!;
            }

            //
            // 4) Try match in KnownTags JSON file first
            //
            if (knownTagsFromJson != null)
            {
                var match = knownTagsFromJson
                    .FirstOrDefault(x => string.Equals(x.Name, targetTagName, StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    return new ResolvedTimeZoneTagResult
                    {
                        TagName = targetTagName,
                        TagId = match.Id
                    };
                }
            }

            //
            // 5) Try match in WS1 lookup (PS: $KnownTags | Select Name)
            //
            if (tagLookup.ByName.TryGetValue(targetTagName, out var found))
            {
                return new ResolvedTimeZoneTagResult
                {
                    TagName = targetTagName,
                    TagId = found.Id
                };
            }

            //
            // 6) If Ensure: create tag
            //
            if (ensure)
            {
                var newTagName = string.Format(tagNameTemplate, targetTagName);

                int tagId = await CreateTagIfMissingAsync(
                    ogId: ogId,                   // 👈 use the value passed in
                    tagName: newTagName,
                    cancellationToken);

                return new ResolvedTimeZoneTagResult
                {
                    TagName = newTagName,
                    TagId = tagId
                };
            }

            //
            // 7) Fallback: no TagId found
            //
            return new ResolvedTimeZoneTagResult
            {
                TagName = targetTagName,
                TagId = null
            };
        }
        private async Task<int> CreateTagIfMissingAsync(
    int ogId,
    string tagName,
    CancellationToken cancellationToken)
        {
            var payload = new JObject
            {
                ["Name"] = tagName,
                ["OrganizationGroupId"] = ogId
            };

            var response = await PostJsonAsync(
                "/mdm/tags/addtag",
                payload,
                accept: "application/json;version=1"
            );

            if (string.IsNullOrWhiteSpace(response))
                throw new Exception($"Failed to create tag '{tagName}'");

            var json = JObject.Parse(response);
            var id = UnwrapId(json["Id"]);
            if (!id.HasValue)
                throw new Exception($"WS1 returned invalid TagId for '{tagName}'");

            return id.Value;
        }


        private TagInfo? ConvertJsonToTagInfo(JObject obj)
        {
            int? id = null;

            if (obj.TryGetValue("Id", out var idToken))
            {
                id = UnwrapId(idToken);   // you already have UnwrapId from earlier
            }

            if (!id.HasValue)
                return null;

            return new TagInfo
            {
                Id = id.Value,
                Name = (string?)obj["Name"] ?? (string?)obj["TagName"],
                Uuid = (string?)obj["Uuid"],
                TagType = (string?)obj["TagType"],
                LocationGroupId = obj["LocationGroupId"]?.Value<int?>(),
                Description = (string?)obj["Description"],
                CreatedOn = TryParseDate(obj["CreatedOn"]),
                UpdatedOn = TryParseDate(obj["UpdatedOn"]),
                Raw = obj
            };
        }

        private sealed class TagLookup
        {
            public List<TagInfo> All { get; }
            public Dictionary<int, TagInfo> ById { get; }
            public Dictionary<string, TagInfo> ByName { get; }

            public TagLookup(
                List<TagInfo> all,
                Dictionary<int, TagInfo> byId,
                Dictionary<string, TagInfo> byName)
            {
                All = all;
                ById = byId;
                ByName = byName;
            }
        }
        private async Task<List<TagInfo>> GetTagsByOrgV1Async(
    int ogId,
    int pageSize,
    CancellationToken cancellationToken)
        {
            var all = new List<TagInfo>();
            int page = 0;
            int? total = null;

            // Accept header: application/json;version=1  (V1 schema)
            const string acceptHeader = "application/json;version=1";

            while (true)
            {
                // First page: no &page (to mimic the PowerShell function’s behavior)
                string endpoint;
                if (page == 0)
                {
                    endpoint = $"/mdm/tags/search?organizationgroupid={ogId}&pagesize={pageSize}";
                }
                else
                {
                    endpoint = $"/mdm/tags/search?organizationgroupid={ogId}&pagesize={pageSize}&page={page}";
                }

                var response = await SendRequestAsync(endpoint, HttpMethod.Get, null, acceptHeader);
                if (string.IsNullOrWhiteSpace(response))
                {
                    // Error or empty response, stop paging
                    break;
                }

                var json = JToken.Parse(response);

                // PowerShell expects V1 shape: { Tags = [...], Total = N }
                var batch = new List<JObject>();

                if (json is JObject obj)
                {
                    // collection of tags under "Tags"
                    if (obj.TryGetValue("Tags", out var tagsToken))
                    {
                        if (tagsToken is JArray tagsArray)
                        {
                            foreach (var t in tagsArray.OfType<JObject>())
                                batch.Add(t);
                        }
                        else if (tagsToken is JObject singleTagObj)
                        {
                            batch.Add(singleTagObj);
                        }
                    }
                    else
                    {
                        // Some tenants may return a single tag object (unlikely, but safe)
                        batch.Add(obj);
                    }

                    // Total count, if present
                    if (!total.HasValue && obj.TryGetValue("Total", out var totalToken))
                    {
                        if (int.TryParse(totalToken.ToString(), out var tInt))
                            total = tInt;
                    }
                }
                else if (json is JArray arr)
                {
                    foreach (var t in arr.OfType<JObject>())
                        batch.Add(t);
                }

                if (batch.Count == 0)
                {
                    // No more tags
                    break;
                }

                foreach (var t in batch)
                {
                    var id = UnwrapId(t["Id"]);
                    if (!id.HasValue)
                        continue;

                    // PowerShell uses TagName, but some payloads may use Name; handle both.
                    var name = (string?)(t["TagName"] ?? t["Name"]);
                    var uuid = (string?)t["Uuid"];
                    var type = (string?)t["TagType"];
                    int? locationGroupId = null;
                    if (t["LocationGroupId"] != null &&
                        int.TryParse(t["LocationGroupId"].ToString(), out var lg))
                    {
                        locationGroupId = lg;
                    }

                    DateTimeOffset? createdOn = TryParseDate(t["CreatedOn"]);
                    DateTimeOffset? updatedOn = TryParseDate(t["UpdatedOn"]);

                    all.Add(new TagInfo
                    {
                        Id = id.Value,
                        Name = name,
                        Uuid = uuid,
                        TagType = type,
                        LocationGroupId = locationGroupId,
                        Description = (string?)t["Description"],
                        CreatedOn = createdOn,
                        UpdatedOn = updatedOn,
                        Raw = t
                    });
                }

                page++;

                // Stop if we’ve reached the reported total
                if (total.HasValue && all.Count >= total.Value)
                    break;

                // (Very basic CT check; SendRequestAsync itself has no CT yet)
                if (cancellationToken.IsCancellationRequested)
                    break;
            }

            return all;
        }

        /// <summary>
        /// Mirror of PowerShell _Unwrap-Id helper:
        /// - Handles wrapped Id objects like { Value = 123 } or V1 scalar ints.
        /// </summary>
        private static int? UnwrapId(JToken? idToken)
        {
            if (idToken == null || idToken.Type == JTokenType.Null)
                return null;

            // If it's an object with a Value property, unwrap it
            if (idToken is JObject obj && obj.TryGetValue("Value", out var valueToken))
            {
                idToken = valueToken;
            }

            if (int.TryParse(idToken.ToString(), out var value))
                return value;

            return null;
        }

        private static DateTimeOffset? TryParseDate(JToken? token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;

            if (DateTimeOffset.TryParse(token.ToString(), out var dto))
                return dto;

            return null;
        }

        #endregion
    }
}
