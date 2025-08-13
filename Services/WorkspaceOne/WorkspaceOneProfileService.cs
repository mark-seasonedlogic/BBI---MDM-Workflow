using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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

        public async Task<WorkspaceOneProfileDetails> GetProfileDetailsAsync(int profileId)
        {
            try
            {

                var obj = await GetPagedResponseAsync($"/mdm/profiles/{profileId}", "General", null, "application/json;version=2");
                if (obj.Count > 0)
                {
                    JObject token = obj[0];
                    string json = token.ToString(Formatting.Indented);
                    WorkspaceOneProfileDetails currProfile = obj[0].ToObject<WorkspaceOneProfileDetails>() ?? new WorkspaceOneProfileDetails();
                    File.WriteAllText($"C:\\Users\\MarkYoung\\source\\repos\\BBI - MDM Workflow\\Documentation\\WorkspaceOneArtifacts\\Device Profiles\\{currProfile.Name}.json", json);
                    return currProfile;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to retrieve profile details for profile ID {profileId}");
            }
            return new WorkspaceOneProfileDetails();
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
