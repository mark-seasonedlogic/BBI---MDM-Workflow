using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.Services.Authentication;
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
        public async Task<JObject?> CloneManagedAppConfigurationAsync(JObject originalConfig, string newDisplayName, Dictionary<string, object> tokenReplacements = null)
        {
            _logger.LogInformation("Cloning configuration: {ConfigId} to new display name: {NewDisplayName}", originalConfig["id"], newDisplayName);
            var configArray = (JArray)originalConfig["value"];
            var sourceConfig = (JObject?)configArray?.FirstOrDefault();
            _logger.LogDebug("Applying token replacement to source configuration: {SourceConfig}", sourceConfig?.ToString());
            //perform replacements if provided
            if (tokenReplacements != null)
            {
                foreach (var kvp in tokenReplacements)
                {
                    string token = $"{{{{{kvp.Key}}}}}";
                    string value = kvp.Value?.ToString() ?? "";

                    // Replace in top-level string fields (like Description)
                    foreach (var prop in sourceConfig.Properties().Where(p => p.Value.Type == JTokenType.String))
                    {
                        string original = prop.Value.ToString();
                        if (original.Contains(token))
                        {
                            _logger.LogDebug("Replacing token '{Token}' with value '{Value}' in property '{PropertyName}'", token, value, prop.Name);
                            prop.Value = original.Replace(token, value);
                        }
                    }

                    // Replace in settings
                    if (sourceConfig["settings"] is JArray settingsArray)
                    {
                        foreach (var setting in settingsArray.OfType<JObject>())
                        {
                            foreach (var settingProp in setting.Properties().Where(p => p.Value.Type == JTokenType.String))
                            {
                                string original = settingProp.Value.ToString();
                                if (original.Contains(token))
                                {
                                    _logger.LogDebug("Replacing token '{Token}' with value '{Value}' in setting '{SettingName}'", token, value, settingProp.Name);
                                    settingProp.Value = original.Replace(token, value);
                                }
                            }
                        }
                    }
                }
            }

            if (sourceConfig == null)
            {
                _logger.LogError("No source configuration found to clone.");
                return null;
            }
            // Clone base structure
            var clonedConfig = new JObject
            {
                ["@odata.type"] = sourceConfig["@odata.type"] ?? "#microsoft.graph.iosMobileAppConfiguration",
                ["displayName"] = newDisplayName,
                ["description"] = "Cloned from " + (sourceConfig["displayName"] ?? "unknown"),
                ["targetedMobileApps"] = sourceConfig["targetedMobileApps"],
                ["roleScopeTagIds"] = sourceConfig["roleScopeTagIds"] ?? new JArray("0"),
                ["settings"] = sourceConfig["settings"]
            };
            _logger.LogDebug("Cloned configuration structure: {ClonedConfig}", clonedConfig.ToString());
            var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/beta/deviceAppManagement/mobileAppConfigurations")
            {
                Content = new StringContent(clonedConfig.ToString(), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());
            _logger.LogInformation("Sending request to clone configuration: {RequestUrl}", request.RequestUri);
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                //replace tokens if provided
                _logger.LogInformation("Clone successful with status code: {StatusCode}\n{json}", response.StatusCode, json.ToString());
                return JObject.Parse(json);
            }

            _logger.LogError($"Clone failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return null;
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
                //.Where(cfg => cfg["@odata.type"]?.ToString()?.Contains("androidForWork") == true)
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

        public async Task<List<JObject>> FindManagedAppConfigurationsByTargetedAppAsync(
    string targetedAppId,
    string? platformTypeHint = null,
    string? odataAppType = null)
        {
            var allConfigs = await GetAllConfigurationsAsync();

            var matchingConfigs = allConfigs.Where(cfg =>
                cfg["targetedMobileApps"] is JArray apps &&
                apps.Any(app => app?.ToString().Equals(targetedAppId, StringComparison.OrdinalIgnoreCase) == true) &&
                (string.IsNullOrWhiteSpace(platformTypeHint) ||
                 cfg["@odata.type"]?.ToString()?.Contains(platformTypeHint, StringComparison.OrdinalIgnoreCase) == true) &&
                (string.IsNullOrWhiteSpace(odataAppType) ||
                 cfg["@odata.type"]?.ToString()?.Equals(odataAppType, StringComparison.OrdinalIgnoreCase) == true)
            ).ToList();

            return matchingConfigs;
        }



        public async Task<JObject?> GetManagedAppConfigurationByIdAsync(string configId)
        {
            var requestUrl = $"https://graph.microsoft.com/beta/deviceAppManagement/mobileAppConfigurations?$filter=id eq '{configId}'";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();
            return response.IsSuccessStatusCode ? JObject.Parse(json) : null;
        }
    }

}
