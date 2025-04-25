using Microsoft.Graph.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Protection.PlayReady;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    /// <summary>
    /// Service for interacting with Intune managed devices.  This differs from Entra ID devices.
    /// </summary>
    public class GraphManagedDeviceService : IGraphIntuneDeviceService
    {
        private readonly string _endpoint;

        public GraphManagedDeviceService(string endpoint = "https://graph.microsoft.com/v1.0/deviceManagement/managedDevices")
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
        public async Task UpdateDeviceUserNameAsync(string deviceId, string newUserName, string accessToken)
        {
            /* var url = $"{_endpoint}/{deviceId}";

             using var client = new HttpClient();
             client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
             client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

             var updatePayload = new
             {
                 userPrincipalName = newUserName
             };

             var json = JsonConvert.SerializeObject(updatePayload);
             var content = new StringContent(json, Encoding.UTF8, "application/json");

             var response = await client.PatchAsync(url, content);
             response.EnsureSuccessStatusCode();
            */

            using var client = new HttpClient();
            var url = $"https://graph.microsoft.com/v1.0/devices/a5db2112-92be-482a-bb4f-7206b74e1ff6";

            var payload = new
            {
                extensionAttributes = new
                {
                    extensionAttribute1 = "1_9921"
                }
            };


            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = content;

            var response = await client.SendAsync(request);
            var errorBody = await response.Content.ReadAsStringAsync();
        }

    }

}
