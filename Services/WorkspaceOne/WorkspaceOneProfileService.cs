using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BBIHardwareSupport.MDM.Services.WorkspaceOne
{
    /// <summary>
    /// Implements IProductsService to retrieve product provisioning data from Workspace ONE.
    /// </summary>
    public class WorkspaceOneProfileService : WorkspaceOneServiceBase, IWorkspaceOneProfileService
    {

        private readonly ILogger<WorkspaceOneProfileService> _logger;

        public WorkspaceOneProfileService(HttpClient httpClient, IWorkspaceOneAuthService authService, ILogger<WorkspaceOneProfileService> logger) : base(httpClient, authService)
        {
            _logger = logger;
        }

        public async Task<List<WorkspaceOneProfileSummary>> GetAllProfilesAsync()
        {
            var profiles = new List<WorkspaceOneProfileSummary>();
            try
            {
                //No params for this call but we pass it anyway:
                var queryParams = new Dictionary<string, string>();
                var obj = await GetPagedResponseAsync("/mdm/profiles/search", "ProfileList", queryParams, "application/json;version=2");


                foreach (var item in obj)
                {
                    profiles.Add(new WorkspaceOneProfileSummary
                    {
                        ProfileId = item["ProfileId"]?.ToObject<int>() ?? 0,
                        ProfileName = item["ProfileName"]?.ToString() ?? string.Empty,
                        ManagedBy = item["ManagedBy"]?.ToString() ?? string.Empty,
                        OrganizationGroupId = item["OrganizationGroupId"]?.ToObject<int>() ?? 0,
                        OrganizationGroupUuid = item["ManagedByOrganizationGroupUUID"]?.ToString() ?? string.Empty,
                        ProfileStatus = item["ProfileStatus"]?.ToString() ?? string.Empty,
                        Platform = item["Platform"]?.ToString() ?? string.Empty,
                        AssignmentType = item["AssignmentType"]?.ToString() ?? string.Empty,
                        AssignmentSmartGroups = item["AssignmentSmartGroups"]?.ToObject<List<WorkspaceOneSmartGroupReference>>() ?? new(),
                        ExcludedSmartGroups = item["ExclusionSmartGroups"]?.ToObject<List<WorkspaceOneSmartGroupReference>>() ?? new(),
                        ProfileType = item["ProfileType"]?.ToString() ?? string.Empty,
                        ProfileUuid = item["ProfileUuid"]?.ToString() ?? string.Empty,
                        Context = item["Context"]?.ToString() ?? string.Empty
                    });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product provisioning profiles from Workspace ONE");
            }

            return profiles;
        }

        public Task<WorkspaceOneProfileSummary> GetProfileByIdAsync(int profileId)
        {
            throw new NotImplementedException();
        }
        

        public async Task<WorkspaceOneProfileDetails> CreateProfileAsync(
            WorkspaceOneProfileCreateRequest request,
            CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.PlatformSegment)) throw new ArgumentException("Platform is required.", nameof(request));
            if (request.Payload is null) throw new ArgumentException("Payload is required.", nameof(request));

            try
            {
                // Normalize platform segment to match endpoint expectations
                var platform = request.PlatformSegment.Trim();
                // (Optional) enforce known values:
                // if (!new[] { "Android", "iOS", "Windows" }.Contains(platform, StringComparer.OrdinalIgnoreCase)) ...

                var endpoint = $"profiles/platforms/{platform}/Create";

                using var msg = new HttpRequestMessage(HttpMethod.Post, endpoint);

                // WS1 v2 contract owned by transport layer
                msg.Headers.Accept.Clear();
                msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Ws1V2Json));

                msg.Content = new StringContent(
                    request.Payload.ToString(Formatting.None),
                    Encoding.UTF8,
                    "application/json");

                var resp = await _httpClient.SendAsync(msg, ct);
                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync(ct);
                return JsonConvert.DeserializeObject<WorkspaceOneProfileDetails>(json) ?? new WorkspaceOneProfileDetails();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create WS1 profile for platform {Platform}", request.PlatformSegment);
                return new WorkspaceOneProfileDetails();
            }
        }
        public async Task<WorkspaceOneProfileDetails> GetProfileDetailsAsync(int profileId)
        {
            try
            {
                var details = await GetJsonAsync<WorkspaceOneProfileDetails>(
                    $"mdm/profiles/{profileId}",
                    "application/json;version=2");

                if (details is null)
                    return new WorkspaceOneProfileDetails();

                var json = JsonConvert.SerializeObject(details, Formatting.Indented);

                var safeName = string.Concat((details.Name ?? $"profile_{profileId}")
                    .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

                File.WriteAllText(
                    $"C:\\Users\\MarkYoung\\source\\repos\\BBI - MDM Workflow\\Documentation\\WorkspaceOneArtifacts\\Device Profiles\\{safeName}.json",
                    json);

                return details;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve profile details for profile ID {ProfileId}", profileId);
                return new WorkspaceOneProfileDetails();
            }
        }

        public async Task<List<WorkspaceOneProfileDetails>> GetProfileDetailsBySummaryList(List<WorkspaceOneProfileSummary> profileSummaries)
        {
            var profileDetailsList = new List<WorkspaceOneProfileDetails>();
            foreach (var profile in profileSummaries)
            {
                try
                {
                    if (profile.ProfileId <= 0)
                    {
                        _logger.LogWarning($"Profile ID is invalid: {profile.ProfileId}");
                        continue;
                    }
                    var profileDetails = await GetProfileDetailsAsync(profile.ProfileId);
                    if(profileDetails != null)
                    {
                        profileDetailsList.Add(profileDetails);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing profile summary for ID {profile.ProfileId}");
                    continue;
                }
            }
            return profileDetailsList;
        }
    }
}
