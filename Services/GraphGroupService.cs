using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Privacy.SubjectRightsRequests.Item.Notes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;
using Windows.Web.Http;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    /// <summary>
    /// Service for interacting with Intune managed devices.  This differs from Entra ID devices.
    /// </summary>
    public class GraphGroupService : IGraphADGroupService
    {
        private readonly string _endpoint;
        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<GraphIntuneConfigurationService> _logger;


        public GraphGroupService(System.Net.Http.HttpClient httpClient, IGraphAuthService authService, ILogger<GraphIntuneConfigurationService> logger, string endpoint = "https://graph.microsoft.com/v1.0/group")
        {
            _endpoint = endpoint;
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<string> CreateDynamicGroupAsync(string displayName, string description, string membershipRule, List<string> ownerIds)
        {
            using var client = new System.Net.Http.HttpClient();
            if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName must not be empty.");
            if (string.IsNullOrWhiteSpace(membershipRule)) throw new ArgumentException("Membership rule must not be empty.");
            if (ownerIds == null || ownerIds.Count == 0) throw new ArgumentException("At least one owner must be specified.");

            // Build owners @odata.bind list
            var ownersODataBind = new List<string>();
            foreach (var ownerId in ownerIds)
            {
                ownersODataBind.Add($"https://graph.microsoft.com/v1.0/users/{ownerId}");
            }

            // Create the dynamic group payload
            var groupPayload = new Dictionary<string, object>
{
    { "description", description },
    { "displayName", displayName },
    { "mailEnabled", false },
    { "mailNickname", GenerateMailNickname(displayName) },
    { "securityEnabled", true },
    { "groupTypes", new[] { "DynamicMembership" } },
    { "membershipRule", membershipRule },
    { "membershipRuleProcessingState", "On" },
    { "owners@odata.bind", ownersODataBind }
};

            var content = new StringContent(JsonConvert.SerializeObject(groupPayload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://graph.microsoft.com/v1.0/groups", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new ApplicationException($"Failed to create group. Status: {response.StatusCode}. Details: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var createdGroup = JsonConvert.DeserializeObject<dynamic>(responseContent);

            // Return the ID of the created group
            return createdGroup.id;
        }

        private string GenerateMailNickname(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return Guid.NewGuid().ToString();
            return displayName.Replace(" ", "").Replace("-", "").Replace("_", "").ToLower();
        }

        public async Task<List<Group>> GetGroupsAsync(string accessToken)
        {
            using var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(_endpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<GroupListWrapper>(json);
            return obj?.Value ?? new List<Group>();
        }

        

        private class GroupListWrapper
        {
            public List<Group> Value { get; set; }
        }
        public async Task<BBIEntraGroupExtension?> GetBbiGroupExtensionAsync(string groupId)
        {
            var token = await _authService.GetAccessTokenAsync();

            var selectFields = string.Join(",", new[]
            {
        "displayName",
        $"{BBIEntraGroupExtension.Prefix}restaurantCdId",
        $"{BBIEntraGroupExtension.Prefix}brandAbbreviation",
        $"{BBIEntraGroupExtension.Prefix}restaurantNumber",
        $"{BBIEntraGroupExtension.Prefix}restaurantName",
        $"{BBIEntraGroupExtension.Prefix}regionId"
    });

            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"https://graph.microsoft.com/v1.0/groups/{groupId}?$select={selectFields}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) return null;

            var jObj = JObject.Parse(json);
            return new BBIEntraGroupExtension
            {
                RestaurantCdId = jObj[$"{BBIEntraGroupExtension.Prefix}restaurantCdId"]?.ToString(),
                BrandAbbreviation = jObj[$"{BBIEntraGroupExtension.Prefix}brandAbbreviation"]?.ToString(),
                RestaurantNumber = jObj[$"{BBIEntraGroupExtension.Prefix}restaurantNumber"]?.ToString(),
                RestaurantName = jObj[$"{BBIEntraGroupExtension.Prefix}restaurantName"]?.ToString(),
                RegionId = jObj[$"{BBIEntraGroupExtension.Prefix}regionId"]?.ToString()
            };
        }
        public async Task<bool> SetBbiGroupExtensionAsync(string groupId, BBIEntraGroupExtension extension)
        {
            var token = await _authService.GetAccessTokenAsync();
            var body = new JObject();

            void Set(string name, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    body[$"{BBIEntraGroupExtension.Prefix}{name}"] = value;
            }

            Set("restaurantCdId", extension.RestaurantCdId);
            Set("brandAbbreviation", extension.BrandAbbreviation);
            Set("restaurantNumber", extension.RestaurantNumber);
            Set("restaurantName", extension.RestaurantName);
            Set("regionId", extension.RegionId);

            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Patch, $"https://graph.microsoft.com/v1.0/groups/{groupId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        public async Task AddOrUpdateGroupOpenExtensionAsync(string groupId, IDictionary<string, object> extensionData)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();

            var extension = new OpenTypeExtension
            {
                ExtensionName = "com.bbi.entra.group.metadata",
                AdditionalData = extensionData
            };

            try
            {
                await client.Groups[groupId].Extensions.PostAsync(extension);
            }
            catch (ServiceException ex) when ((int)ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.Conflict)
            {
                // If already exists, do PATCH
                await client.Groups[groupId].Extensions["com.bbi.entra.group.metadata"]
                    .PatchAsync(extension);
            }
        }
        public async Task<IDictionary<string, object>?> GetGroupOpenExtensionAsync(string groupId)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();

            try
            {
                var result = await client.Groups[groupId].Extensions["com.bbi.entra.group.metadata"]
                    .GetAsync();

                return result.AdditionalData;
            }
            catch (ServiceException ex) when ((int)ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        public async Task<IDictionary<string, object>> GetOpenExtensionAsync(string groupId, string extensionId)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();
            var extension = await client.Groups[groupId].Extensions[extensionId].GetAsync();
            return extension.AdditionalData;
        }
        public async Task AddOrUpdateOpenExtensionAsync(string groupId, string extensionId, IDictionary<string, object> extensionData)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();
            var extension = new OpenTypeExtension
            {
                ExtensionName = extensionId,
                AdditionalData = extensionData
            };

            try
            {
                await client.Groups[groupId].Extensions.PostAsync(extension);
            }
            catch (ServiceException ex) when ((int)ex.ResponseStatusCode == (int)HttpStatusCode.Conflict)
            {
                await client.Groups[groupId].Extensions[extensionId].PatchAsync(extension);
            }
        }
        public async Task<OpenTypeExtension?> GetGroupExtensionMetadataAsync(string groupId, string extensionName)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();
            var group = await client.Groups[groupId]
                .GetAsync(config => config.QueryParameters.Expand = new[] { "extensions" });

            var match = group?.Extensions?
                .OfType<OpenTypeExtension>()
                .FirstOrDefault(e => e.ExtensionName?.Equals(extensionName, StringComparison.OrdinalIgnoreCase) == true);

            return match;
        }
        public async Task<Group> FindGroupByDisplayNameAsync(string displayName)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();
            var result = await client.Groups
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = $"displayName eq '{displayName}'";
                });

            return result?.Value?.FirstOrDefault();
        }




    }

}
