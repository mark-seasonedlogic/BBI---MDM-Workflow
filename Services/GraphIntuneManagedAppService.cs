using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    class GraphIntuneManagedAppService : IGraphIntuneManagedAppService
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<GraphIntuneManagedAppService> _logger;

        public GraphIntuneManagedAppService(HttpClient httpClient, IGraphAuthService authService, ILogger<GraphIntuneManagedAppService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<JObject?> AssignAppToGroupAsync(string appId, string groupId)
        {
            string responseContent = string.Empty;
            try
            {
                var assignUrl = $"https://graph.microsoft.com/v1.0/deviceAppManagement/mobileApps/{appId}/assign";

                var assignmentPayload = new
                {
                    mobileAppAssignments = new[]
                    {
            new
            {
                target = new Dictionary<string, object>
                {
                    { "@odata.type", "#microsoft.graph.groupAssignmentTarget" },
                    { "groupId", groupId }
                },
                intent = "required"
            }
        }
                };

                var json = JsonConvert.SerializeObject(assignmentPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var token = await _authService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync(assignUrl, content);
                responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Assign operation succeeded with no content.");
                    return JObject.Parse("{\"Success\":true}"); // or return true, etc.
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning app {AppId} to group {GroupId}.\nException Message:\n{ExceptionMessage}\nStack Trace: {StackTrace}", appId, groupId, ex.Message, ex.StackTrace);
            }
            return JObject.Parse(responseContent);
        }


        public async Task<JObject?> GetManagedAppByIdAsync(string appId)
        {
            var requestUrl = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps/{appId}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve managed app by ID: {StatusCode} - {Content}", response.StatusCode, json);
                return null;
            }

            return JObject.Parse(json);
        }

        public async Task<JObject?> GetManagedAppByNameAsync(string appName)
        {
            var requestUrl = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileApps?$filter=displayName eq '{appName}' and isOf('microsoft.graph.iosVppApp')";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            var apps = JObject.Parse(json)["value"]?.ToObject<List<JObject>>();
            return apps?.FirstOrDefault(a =>
                string.Equals(a["displayName"]?.ToString(), appName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a["publisher"]?.ToString(), appName, StringComparison.OrdinalIgnoreCase)
            );
        }

    }
}
