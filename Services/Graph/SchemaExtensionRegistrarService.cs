using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.Services.Authentication;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public class SchemaExtensionRegistrarService : ISchemaExtensionRegistrar
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<SchemaExtensionRegistrarService> _logger;

        public SchemaExtensionRegistrarService(HttpClient httpClient, IGraphAuthService authService, ILogger<SchemaExtensionRegistrarService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<bool> RegisterBbiEntraGroupExtensionAsync()
        {
            var token = await _authService.GetAccessTokenAsync();

            var payload = new JObject
            {
                ["id"] = "bbiEntraGroupExtension",
                ["description"] = "Custom extension for Entra groups with restaurant metadata",
                ["targetTypes"] = new JArray("Group"),
                ["properties"] = new JArray
            {
                new JObject { ["name"] = "restaurantCdId", ["type"] = "String" },
                new JObject { ["name"] = "brandAbbreviation", ["type"] = "String" },
                new JObject { ["name"] = "restaurantNumber", ["type"] = "String" },
                new JObject { ["name"] = "restaurantName", ["type"] = "String" },
                new JObject { ["name"] = "regionId", ["type"] = "String" }
            }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://graph.microsoft.com/v1.0/schemaExtensions")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", token) },
                Content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"❌ Failed to register schema extension: {response.StatusCode} - {result}");
                return false;
            }

            _logger.LogInformation($"✅ Schema extension registered successfully.");
            return true;
        }
    }

}
