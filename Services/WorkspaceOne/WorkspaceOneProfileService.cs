using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
            var products = new List<WorkspaceOneProfileSummary>();

            try
            {
                //No params for this call but we pass it anyway:
                var queryParams = new Dictionary<string, string>();
                var obj = await GetPagedResponseAsync("/mdm/profiles/search", "Profiles", queryParams, "application/json;version=2");
                var profiles = new List<WorkspaceOneProfileSummary>();

                foreach (var item in obj)
                {
                    profiles.Add(new WorkspaceOneProfileSummary
                        {
                            ProfileId = item["ID"]?["Value"]?.ToObject<int>() ?? 0,
                            ProfileName = item["Name"]?.ToString() ?? string.Empty,
                            ManagedBy = item["ManagedByOrganizationGroupName"]?.ToString() ?? string.Empty,
                            OrganizationGroupId = item["ManagedByOrganizationGroupID"]?.ToObject<int>() ?? 0,
                            OrganizationGroupUuid = item["ManagedByOrganizationGroupUUID"]?.ToString() ?? string.Empty,
                            ProfileStatus = item["Active"]?.ToString() == "true" ? "Active" : "Inactive",
                            Platform = item["Platform"]?.ToString() ?? string.Empty,
                            AssignmentType = item["AssignmentType"]?.ToString() ?? string.Empty,
                            AssignmentSmartGroups = item["SmartGroups"]?.ToObject<List<WorkspaceOneSmartGroupReference>>() ?? new(),
                            ExcludedSmartGroups = item["ExclusionSmartGroups"]?.ToObject<List<WorkspaceOneSmartGroupReference>>() ?? new(),
                            ProfileType = item["ProfileType"]?.ToString() ?? string.Empty,
                            ProfileUuid = item["DevicePolicyUuid"]?.ToString() ?? string.Empty,
                            Context = item["Context"]?.ToString() ?? string.Empty
                        });
                    }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve product provisioning profiles from Workspace ONE");
            }

            return products;
        }
    }
}
