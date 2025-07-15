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
using Microsoft.Extensions.Logging;
using BBIHardwareSupport.MDM.Services.Authentication;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public class GraphDeviceCategoryService : IGraphDeviceCategoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<GraphDeviceCategoryService> _logger;

        public GraphDeviceCategoryService(HttpClient httpClient, IGraphAuthService authService, ILogger<GraphDeviceCategoryService> logger)
        {
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<List<JObject>> GetAllDeviceCategoriesAsync()
        {
            _logger.LogInformation("Calling GetAllDeviceCategoriesAsync to retrieve categories.");
            List<JObject> resultList = null;
            try
            {
                var token = await _authService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("https://graph.microsoft.com/v1.0/deviceManagement/deviceCategories");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                resultList = parsed["value"]?.ToObject<List<JObject>>() ?? new List<JObject>();
            }
            catch(Exception ex)
            {
                _logger.LogError("An error occurred retrieving all categories from Graph.\nException:\n{ExceptionMessage}\nStack Trace:\n{StackTrace}", ex.Message, ex.StackTrace);

            }
            return resultList;
            }
            

        public async Task<JObject?> GetDeviceCategoryByNameAsync(string categoryName)
        {
            _logger.LogInformation("Calling GetDeviceCategoryByNameAsync for category: {CategoryName}", categoryName);
            JObject result = null;
            try
            {


                var token = await _authService.GetAccessTokenAsync();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"https://graph.microsoft.com/v1.0/deviceManagement/deviceCategories?$filter=displayName eq '{categoryName}'";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var parsed = JObject.Parse(json);
                result = parsed["value"]?.FirstOrDefault() as JObject;
            }
            catch(Exception ex)
            {
                _logger.LogError("An error occurred retrieving all categories from Graph.\nException:\n{ExceptionMessage}\nStack Trace:\n{StackTrace}", ex.Message, ex.StackTrace);
            }
            return result;
        }

        public async Task<JObject?> CreateDeviceCategoryAsync(string categoryName, string description = "")
        {
            _logger.LogInformation("Calling CreateDeviceCategoryAsync for category: {CategoryName} with description: {description}", categoryName,description);
            JObject result = null;
            try
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
                    _logger.LogError($"Failed to create device category: {response.StatusCode}\n{error}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                result = JObject.Parse(responseContent);
            }
            catch(Exception ex)
            {
                _logger.LogError("An error occurred creating category: {Category} from Graph API.\nException:\n{ExceptionMessage}\nStack Trace:\n{StackTrace}", categoryName, ex.Message, ex.StackTrace);

            }
            return result;
        }
    }

}
