using BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public class GraphDeviceCategoryService : IGraphDeviceCategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAuthService _authService;

        public GraphDeviceCategoryService(HttpClient httpClient, IGraphAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        public async Task<List<JObject>> GetAllDeviceCategoriesAsync()
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync("https://graph.microsoft.com/v1.0/deviceManagement/deviceCategories");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JObject.Parse(json);
            return parsed["value"]?.ToObject<List<JObject>>() ?? new List<JObject>();
        }

        public async Task<JObject?> GetDeviceCategoryByNameAsync(string categoryName)
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var url = $"https://graph.microsoft.com/v1.0/deviceManagement/deviceCategories?$filter=displayName eq '{categoryName}'";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var parsed = JObject.Parse(json);
            return parsed["value"]?.FirstOrDefault() as JObject;
        }

        public async Task<JObject?> CreateDeviceCategoryAsync(string categoryName, string description = "")
        {
            var token = await _authService.GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var payload = new
            {
                displayName = categoryName,
                description = description
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://graph.microsoft.com/v1.0/deviceManagement/deviceCategories", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ApplicationException($"Failed to create device category: {response.StatusCode}\n{error}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return JObject.Parse(responseContent);
        }
    }

}
