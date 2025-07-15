using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using BBIHardwareSupport.MDM.Services.Authentication;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Helpers
{
    public class AppConfigTemplateHelper : IAppConfigTemplateHelper
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<AppConfigTemplateHelper> _logger;

        public AppConfigTemplateHelper(HttpClient httpClient, IGraphAuthService authService, ILogger<AppConfigTemplateHelper> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<bool> CreateFromTemplateAsync(string templatePath, string displayName, string restaurantCode)
        {
            try
            {
                var templateJson = await File.ReadAllTextAsync(templatePath);

                // Define tokens (can be expanded later)
                var tokens = new Dictionary<string, string>
            {
                { "restaurant_cd_id", restaurantCode }
            };

                // Step 1: Replace tokens
                var substituted = ApplyTokenSubstitution(templateJson, tokens);

                // Step 2: Remove read-only fields
                var jObj = JObject.Parse(substituted);
                jObj.Remove("id");
                jObj.Remove("createdDateTime");
                jObj.Remove("lastModifiedDateTime");
                jObj.Remove("version");

                // Step 3: Set the display name
                jObj["displayName"] = displayName;

                var payload = jObj.ToString(Formatting.None);
                var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/beta/deviceAppManagement/mobileAppConfigurations")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetAccessTokenAsync());

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to create app config: {response.StatusCode} - {responseContent}");
                    return false;
                }

                _logger.LogInformation($"✅ Successfully created app config: {displayName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception creating app config from template: {ex.Message}");
                return false;
            }
        }

        private static string ApplyTokenSubstitution(string template, Dictionary<string, string> tokens)
        {
            foreach (var token in tokens)
            {
                template = template.Replace($"{{{{{token.Key}}}}}", token.Value);
            }
            return template;
        }
    }

}
