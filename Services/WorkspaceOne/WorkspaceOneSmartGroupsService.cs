
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport.MDM.Services.WorkspaceOne
{
    /// <summary>
    /// Provides access to SmartGroup data using the Workspace ONE API.
    /// Implements the GetAllSmartGroupsAsync method using paginated requests.
    /// </summary>
    public class WorkspaceOneSmartGroupsService : IWorkspaceOneSmartGroupsService
    {
        private readonly ILogger<WorkspaceOneSmartGroupsService> _logger;
        private readonly IWorkspaceOneAuthService _authService;
        private readonly HttpClient _httpClient;

        public WorkspaceOneSmartGroupsService(
            ILogger<WorkspaceOneSmartGroupsService> logger,
            IWorkspaceOneAuthService authService,
            HttpClient httpClient)
        {
            _logger = logger;
            _authService = authService;
            _httpClient = httpClient;
        }

        public async Task<List<WorkspaceOneSmartGroup>> GetAllSmartGroupsAsync()
        {
            var results = new List<WorkspaceOneSmartGroup>();
            var rawItems = await GetPagedResponseAsync("/mdm/smartgroups/search", "SmartGroups");

            foreach (var item in rawItems)
            {
                try
                {
                    var group = new WorkspaceOneSmartGroup
                    {
                        Id = item["Id"]?.ToString(),
                        Name = item["Name"]?.ToString(),
                        Description = item["Description"]?.ToString(),
                        Platform = item["Platform"]?.ToString(),
                        Type = item["Type"]?.ToString(),
                        AssignmentType = item["AssignmentType"]?.ToString(),
                        ManagedDeviceCount = item["ManagedDeviceCount"]?.Value<int>() ?? 0,
                        OrganizationGroupId = item["LocationGroupId"]?.ToString(),
                        LastModifiedDate = item["LastModifiedDate"]?.Value<DateTime?>(),
                        MembershipCriteriaSummary = string.Join("; ",
                            item["MembershipCriteria"]?.Select(c => $"{c["CriteriaType"]}: {c["Value"]}") ?? Enumerable.Empty<string>()),
                        MembershipCriteria = item["MembershipCriteria"]?.Select(c => new WorkspaceOneSmartGroupCriteria
                        {
                            CriteriaType = c["CriteriaType"]?.ToString(),
                            Value = c["Value"]?.ToString()
                        }).ToList(),
                        Tags = new Dictionary<string, object>()
                    };

                    results.Add(group);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SmartGroup JSON.");
                }
            }

            return results;
        }

        public Task<WorkspaceOneSmartGroupAssignment> GetAssignmentsBySmartGroupIdAsync(string smartGroupId)
        {
            throw new NotImplementedException();
        }

        public Task<WorkspaceOneSmartGroup> GetSmartGroupByIdAsync(string smartGroupId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<WorkspaceOneSmartGroup>> GetSmartGroupsByDeviceIdAsync(string deviceId)
        {
            throw new NotImplementedException();
        }


        private async Task<List<JObject>> GetPagedResponseAsync(string endpoint, string itemType,
            Dictionary<string, string>? queryParams = null, string? accept = null)
        {
            var allResults = new List<JObject>();
            int currentPage = 1;
            int pageSize = 0;
            int totalItems = int.MaxValue;

            do
            {
                var queryWithPage = new Dictionary<string, string>(queryParams ?? new Dictionary<string, string>())
                {
                    ["page"] = currentPage.ToString()
                };

                var queryString = string.Join("&", queryWithPage.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

                var response = await SendRequestAsync($"{endpoint}?{queryString}", HttpMethod.Get, null, accept);

                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogWarning("Empty response on page {Page}", currentPage);
                    break;
                }

                try
                {
                    var jsonResponse = JToken.Parse(response);
                    JArray objectItems = jsonResponse[itemType] as JArray ?? new JArray();

                    if (jsonResponse is JObject obj)
                    {
                        pageSize = pageSize == 0
                            ? int.Parse(obj.Value<string>("page_size") ?? obj.Value<string>("PageSize") ?? "0")
                            : pageSize;

                        totalItems = totalItems == int.MaxValue
                            ? int.Parse(obj.Value<string>("total") ?? obj.Value<string>("Total") ?? "0")
                            : totalItems;
                    }

                    foreach (var item in objectItems)
                    {
                        allResults.Add((JObject)item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse response for page {Page}", currentPage);
                }

                currentPage++;

            } while ((currentPage - 1) * pageSize < totalItems);

            return allResults;
        }

        private async Task<string?> SendRequestAsync(string endpoint, HttpMethod method,
            HttpContent? content = null, string? accept = null)
        {
            var request = new HttpRequestMessage(method, $"{_authService.BaseUri}{endpoint}");

            var authHeaders = await _authService.GetAuthorizationHeaderAsync();
            foreach (var header in authHeaders)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = header.Value.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
                    }
                }
                else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
                }
                else
                {
                    if (request.Headers.Contains(header.Key))
                        request.Headers.Remove(header.Key);

                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (content != null)
            {
                request.Content = content;
            }

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("WorkspaceONE API Error: {StatusCode}, Body: {Body}", response.StatusCode, errorBody);
                    return null;
                }

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed to Workspace ONE");
                return null;
            }
        }
    }
}
