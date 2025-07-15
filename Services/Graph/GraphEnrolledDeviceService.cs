using BBIHardwareSupport.MDM.Services.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    /// <summary>
    /// Service for interacting with Intune managed devices.  This differs from Entra ID devices.
    /// </summary>
    public class GraphEnrolledDeviceService : IGraphADDeviceService
    {
        private readonly string _endpoint;

        private readonly System.Net.Http.HttpClient _httpClient;
        private readonly IGraphAuthService _authService;
        private readonly ILogger<GraphEnrolledDeviceService> _logger;
        public GraphEnrolledDeviceService(System.Net.Http.HttpClient httpClient, IGraphAuthService authService, ILogger<GraphEnrolledDeviceService> logger, string endpoint = "https://graph.microsoft.com/v1.0/deviceManagement/managedDevices")
        {
            _endpoint = endpoint;
            _httpClient = httpClient;
            _authService = authService;
            _logger = logger;
        }

        public async Task<List<ManagedDevice>> GetDevicesAsync(string accessToken)
        {
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.GetAsync(_endpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<DeviceListWrapper>(json);
            return obj?.Value ?? new List<ManagedDevice>();
        }

        private class DeviceListWrapper
        {
            public List<ManagedDevice> Value { get; set; }
        }

        public async Task AddOrUpdateDeviceOpenExtensionAsync(string deviceId, IDictionary<string, object> extensionData)
        {
            var client = await _authService.GetAuthenticatedGraphClientAsync();

            var extension = new OpenTypeExtension
            {
                ExtensionName = "com.bbi.entra.device.metadata",
                AdditionalData = extensionData
            };

            try
            {
                await client.Devices[deviceId].Extensions.PostAsync(extension);
            }
            catch (ServiceException ex) when ((int)ex.ResponseStatusCode == (int)System.Net.HttpStatusCode.Conflict)
            {
                // If already exists, do PATCH
                await client.Devices[deviceId].Extensions["com.bbi.entra.device.metadata"]
                    .PatchAsync(extension);
            }
        }

        public Task<ManagedDevice?> GetDeviceByIntuneDeviceAsync(string deviceId)
        {
            throw new NotImplementedException();
        }
    }

}
