using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using Microsoft.Extensions.Logging;
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
    public class GraphIntuneConfigurationService : IGraphIntuneConfigurationService
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<GraphIntuneConfigurationService> _logger;

        public GraphIntuneConfigurationService(HttpClient httpClient, IGraphAuthService authService, ILogger<GraphIntuneConfigurationService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<JObject?> GetConfigurationByIdAsync(string configId)
        {
            var requestUrl = $"https://graph.microsoft.com/beta/deviceManagement/deviceConfigurations/{configId}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? JObject.Parse(json) : null;
        }
        //There should be ONLY ONE app configuration
        public List<JObject> FindConfigsWithStoreIdentifier(List<JObject> configs, string storeIdentifier)
        {
            return configs
                .Where(config =>
                    config["settings"] is JArray settings &&
                    settings.Any(setting =>
                        setting["appConfigKey"]?.ToString() == "storeIdentifierValues" &&
                        setting["appConfigKeyValue"]?.ToString() == storeIdentifier))
                .ToList();
        }

        public async Task<JObject?> GetConfigurationByAppIdAsync(string appId)
        {
            var requestUrl = $"https://graph.microsoft.com/beta/deviceManagement/deviceConfigurations/$filter=targetedMobileApps {appId} eq '{{appId}}'";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? JObject.Parse(json) : null;
        }
        public async Task<List<JObject>> GetAllConfigurationsAsync()
        {
            var requestUrl = "https://graph.microsoft.com/beta/deviceAppManagement/mobileAppConfigurations";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to retrieve device configurations: " + response.StatusCode);
                return new List<JObject>();
            }

            var result = JObject.Parse(json);
            return result["value"]?.ToObject<List<JObject>>() ?? new List<JObject>();
        }

        public async Task<List<JObject>> GetOemConfigurationsAsync()
        {
            var allConfigs = await GetAllConfigurationsAsync();
            return allConfigs
                .Where(cfg => cfg["@odata.type"]?.ToString()?.Contains("androidForWork") == true)
                .ToList();
        }

        public async Task<JObject?> FindManagedAppConfigurationByTargetedAppAsync(string targetedAppId, string? platformTypeHint = null)
        {
            var allConfigs = await GetAllConfigurationsAsync();

            return allConfigs.FirstOrDefault(cfg =>
                cfg["targetedMobileApps"] is JArray apps &&
                apps.Any(app => app?.ToString().Equals(targetedAppId, StringComparison.OrdinalIgnoreCase) == true) &&
                (string.IsNullOrWhiteSpace(platformTypeHint) ||
                 cfg["@odata.type"]?.ToString()?.Contains(platformTypeHint, StringComparison.OrdinalIgnoreCase) == true)
            );
        }


    }

}
