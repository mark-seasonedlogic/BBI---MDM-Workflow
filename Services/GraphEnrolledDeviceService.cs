using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    /// <summary>
    /// Service for interacting with Intune managed devices.  This differs from Entra ID devices.
    /// </summary>
    public class GraphEnrolledDeviceService : IGraphADDeviceService
    {
        private readonly string _endpoint;

        public GraphEnrolledDeviceService(string endpoint = "https://graph.microsoft.com/v1.0/deviceManagement/managedDevices")
        {
            _endpoint = endpoint;
        }

        public async Task<List<ManagedDevice>> GetDevicesAsync(string accessToken)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync(_endpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonConvert.DeserializeObject<DeviceListWrapper>(json);
            return obj?.Value ?? new List<ManagedDevice>();
        }

        private class DeviceListWrapper
        {
            public List<ManagedDevice> Value { get; set; }
        }
    }

}
