using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public class WorkspaceOneDeviceService : WorkspaceOneServiceBase, IWorkspaceOneDeviceService
    {
        private readonly HttpClient _httpClient;
        private readonly IWorkspaceOneAuthService _authService;
        private List<JObject> _airwatchDevices;
        private Dictionary<string, JObject> _airwatchDeviceCache;

        public WorkspaceOneDeviceService(HttpClient httpClient, IWorkspaceOneAuthService authService) : base(httpClient, authService)
        {

        }


        public async Task<List<JObject>> GetAllAndroidDevicesByOrgExAsync(string orgId)
        {
            var queryParams = new Dictionary<string, string>
    {
        { "organizationgroupid", orgId },
        { "platform", "Android" }
    };

            await AddAuthHeaderAsync();

            var result = await GetPagedResponseAsync(
                "/mdm/devices/extensivesearch",
                "Devices",
                queryParams,
                "application/json;version=2");

            return result ?? new List<JObject>();
        }
        public async Task<List<JObject>> GetAllAndroidDevicesByOrgExAsync(
    string orgId,
    Action<WorkspaceOnePagingProgress>? progress = null)
        {
            var queryParams = new Dictionary<string, string>
    {
        { "organizationgroupid", orgId },
        { "platform", "Android" }
    };

            await AddAuthHeaderAsync();

            return await GetPagedResponseAsync(
                "/mdm/devices/extensivesearch",
                "Devices",
                queryParams,
                "application/json;version=2",
                progress);
        }


        public async Task<List<DeviceRemovalResult>> RemoveBulkDevicesBySerialAsync(List<DeviceRemovalRequest> requests, string username)
        {
            var results = new List<DeviceRemovalResult>();
            if (_airwatchDevices == null)
            {
                _airwatchDevices = await GetAllAndroidDevicesByOrgExAsync("570");


                _airwatchDeviceCache = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
                foreach (var obj in _airwatchDevices)
                {
                    var currDeviceLastEnrolledOn = DateTime.Parse(obj["EnrollmentDate"].ToString());
                    var serialToken = obj["SerialNumber"];
                    var friendlyNameToken = obj["DeviceFriendlyName"];
                    if (serialToken != null)
                    {
                        var serial = serialToken.ToString();
                        var friendlyName = friendlyNameToken?.ToString() ?? string.Empty;
                        // Only add if the key is not already present
                        if (!_airwatchDeviceCache.ContainsKey(serial) && serial != "unknown")
                        {
                            _airwatchDeviceCache[serial] = obj;
                        }
                        else
                        {
                            //We should not remove devices that have duplicate Serial Numbers.  Just log these.
                            if (_airwatchDeviceCache.ContainsKey(serial))
                            {
                                //Keep only the device that is older than 30 days?
                                var dupDeviceName = _airwatchDeviceCache[serial].Value<string>("DeviceFriendlyName");
                                var dupDeviceSerial = _airwatchDeviceCache[serial].Value<string>("SerialNumber");
                                Debug.WriteLine($"Device {friendlyName} - Duplicate SerialNumber encountered: {serial}\nDuplicate Device Name: {dupDeviceName} - Duplicate Serial: {dupDeviceSerial}");
                                Debug.WriteLine($"These devices will have to be checked manually for removal.  Please ensure that the correct device is removed from Airwatch.");
                                _airwatchDeviceCache.Remove(serial); // Remove the duplicate from the cache
                            }
                            else
                            {
                                Debug.WriteLine($"Device {friendlyName} - Unknown SerialNumber encountered: {serial}");
                                Debug.WriteLine($"These devices will have to be checked manually for removal.  Please ensure that the correct device is removed from Airwatch.");
                                _airwatchDeviceCache.Remove(serial); // Remove the duplicate from the cache
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Skipping object without SerialNumber.");
                    }
                }
            }
            //await AddAuthHeaderAsync();
            string removalDate = DateTime.Now.ToString("yyyyMMdd");
            _httpClient.DefaultRequestHeaders.Clear();
            var authHeaders = await _authService.GetAuthorizationHeaderAsync();
            foreach (var header in authHeaders)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the scheme ("Basic") and parameter (Base64) from the full value
                    var parts = header.Value.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
                    }
                }
                else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    _httpClient.DefaultRequestHeaders.Accept.Clear(); // Optional, if Accept was preconfigured
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
                }
                else
                {
                    if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        _httpClient.DefaultRequestHeaders.Remove(header.Key);

                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            List<DeviceRemovalRequest> filteredRemovalRequests = new List<DeviceRemovalRequest>();
            //Get all devices from bloomin for lookup -- Move this to the appropriate place

            foreach (var request in requests)
            {
                //Get all devices as of this moment!  We need to remove devices that have re-enrolled over the last 30 days!!
                /*

                "Model": "samsung SM-T570",
                "OperatingSystem": "13.0.0",
                "PhoneNumber": "",
                "LastSeen": "2025-04-01T00:37:41.837",
                "EnrollmentStatus": "Enrolled",
                "ComplianceStatus": "Compliant",
                "CompromisedStatus": false,
                "LastEnrolledOn": "2022-12-08T21:40:49.087",

                https://as863.awmdm.com/API/mdm/devices?searchby=SerialNumber&id=R52T608Z21R

                */
                //var deviceResponse = await _httpClient.GetAsync($"{_authService.BaseUri}/mdm/devices?searchby=SerialNumber&id={request.SerialNumber}");
                //if (!deviceResponse.IsSuccessStatusCode)
                //{
                //    var errorContent = await deviceResponse.Content.ReadAsStringAsync();
                //throw new HttpRequestException($"Request failed with status {deviceResponse.StatusCode}: {errorContent}");
                //    continue;
                //}
                JObject deviceContent = new JObject();
                var deviceExists = _airwatchDeviceCache.TryGetValue(request.SerialNumber, out deviceContent);
                if (!deviceExists)
                {
                    Debug.WriteLine($"Device {request.SerialNumber} discrepancy found in Airwatch.  This device must be checked manually for removal.");
                    continue;
                }
                // Extract the LastEnrolledOn value
                DateTime lastEnrolledOn = deviceContent.Value<DateTime>("EnrollmentDate");

                // Extract the LastSeen value
                DateTime lastSeen = deviceContent.Value<DateTime>("LastSeen");

                // Compare against today's date
                if (lastEnrolledOn.Date == DateTime.UtcNow.Date)
                {
                    Debug.WriteLine($"Device {request.SerialNumber} was enrolled today. It was last seen on {lastSeen.ToString()}. It will be skipped.");
                    continue;
                }
                else if (lastEnrolledOn < DateTime.UtcNow.AddDays(-60) && lastSeen < DateTime.UtcNow.AddDays(-38))
                {
                    Debug.WriteLine($"Device {request.SerialNumber} was enrolled over 38 days ago. It was last seen on {lastSeen.ToString()}. It will be removed.");
                    filteredRemovalRequests.Add(request);

                }
                else
                {
                    Debug.WriteLine($"Device {request.SerialNumber} enrolled on: {lastEnrolledOn:d}.  It was last seen on {lastSeen.ToString()}.  It will be skipped.");
                    continue;
                }
                var result = new DeviceRemovalResult
                {
                    SerialNumber = request.SerialNumber,
                    StoreTag1 = request.StoreTag1,
                    StoreTag2 = request.StoreTag2,
                    StateTag1 = $"AirwatchAutoDelete-{removalDate}",//request.StateTag1,
                    StateTag2 = username
                };

                try
                {
                    if (string.IsNullOrWhiteSpace(request.SerialNumber))
                    {
                        result.StateTag1 = "Skipped - Serial Number missing";
                        results.Add(result);
                        continue;
                    }



                    //var success = await DeleteDeviceBySerialAsync(request.SerialNumber);

                    //_httpClient.DefaultRequestHeaders.Clear();
                    //_httpClient.DefaultRequestHeaders.Add("aw-tenant-code", tenantCode);
                    //_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
                    //_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var deviceId = deviceContent.Value<String>("DeviceId");
                    var deleteUri = $"{_authService.BaseUri}/mdm/devices/{deviceId}";
                    var deviceSerial = deviceContent.Value<string>("SerialNumber");
                    var deviceFriendlyName = deviceContent.Value<string>("DeviceFriendlyName");
                    var deleteResponse = await _httpClient.DeleteAsync(deleteUri);

                    if (deleteResponse.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"Successfully deleted {deviceFriendlyName} with Serial Number {deviceSerial}");  // Device deleted successfully
                    }
                    else
                    {
                        var errorDetails = await deleteResponse.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Failed to delete {deviceFriendlyName} with Serial Number {deviceSerial}. Status: {deleteResponse.StatusCode}, Details: {errorDetails}");

                        //throw new Exception($"Failed to delete device. Status: {response.StatusCode}, Details: {errorDetails}");
                    }


                    //result.StateTag = success ? $"AutoRemoved-{DateTime.UtcNow:MM-dd-yyyy}" : "FailedToRemove";
                }
                catch (Exception ex)
                {
                    result.StateTag1 = $"Error: {ex.Message}";
                }

                results.Add(result);
            }
            /*
                        request.Headers.Add("Authorization", _credentialsManager.GetAuthorizationHeader());
            request.Headers.Add("aw-tenant-code", _credentialsManager.TenantCode);
            request.Headers.Add("Accept", accept ?? "application/json");
            */
            var payload = GenerateBulkDeletePayloadAsync(filteredRemovalRequests);
            string tenantCode = Environment.GetEnvironmentVariables()["AirWatchAPIKey"]?.ToString();
            var jsonPayload = JsonConvert.SerializeObject(payload);
            //ensure new headers are generated
            _httpClient.DefaultRequestHeaders.Clear();
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            foreach (var header in authHeaders)
            {
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the scheme ("Basic") and parameter (Base64) from the full value
                    var parts = header.Value.Split(' ', 2);
                    if (parts.Length == 2)
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(parts[0], parts[1]);
                    }
                }
                else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    _httpClient.DefaultRequestHeaders.Accept.Clear(); // Optional, if Accept was preconfigured
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
                }
                else
                {
                    if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        _httpClient.DefaultRequestHeaders.Remove(header.Key);

                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            //var response = await _httpClient.PostAsync($"{_authService.BaseUri}/mdm/devices/commands/bulk?command=EnterpriseWipe&searchby=SerialNumber", content);
            return results;
        }
        public object GenerateBulkDeletePayloadAsync(List<DeviceRemovalRequest> requests)
        {

            return new
            {
                BulkValues = new
                {
                    Value = requests
                        .Where(r => !string.IsNullOrWhiteSpace(r.SerialNumber))
                        .Select(r => r.SerialNumber)
                        .Distinct()
                        .ToArray()
                }
            };
        }

        private async Task<bool> DeleteDeviceBySerialAsync(string serialNumber)
        {
            var payload = new
            {
                SearchValue = serialNumber,
                SearchType = "Serialnumber"
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_authService.BaseUri}/users/registereddevices/delete", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<int?> LookupDeviceIdBySerialAsync(string serialNumber)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_authService.BaseUri}/mdm/devices/search?serialnumber={serialNumber}");


            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
            return json?.DeviceId;
        }

        public async Task<bool> RemoveDeviceAsync(int deviceId)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_authService.BaseUri}/mdm/devices/{deviceId}");


            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<DeviceRemovalResult>> ProcessDeviceRemovalAsync(List<DeviceRemovalRequest> requests)
        {
            var results = new List<DeviceRemovalResult>();

            foreach (var request in requests)
            {
                var result = new DeviceRemovalResult
                {
                    SerialNumber = request.SerialNumber,
                    StoreTag1 = request.StoreTag1,
                    StateTag1 = string.Empty
                };

                try
                {
                    if (string.IsNullOrWhiteSpace(request.SerialNumber))
                    {
                        result.StateTag1 = "Skipped - Serial Number missing";
                        results.Add(result);
                        continue;
                    }

                    var deviceId = await LookupDeviceIdBySerialAsync(request.SerialNumber);

                    if (deviceId == null)
                    {
                        result.StateTag1 = "NotFound";
                        results.Add(result);
                        continue;
                    }

                    var success = await RemoveDeviceAsync(deviceId.Value);

                    result.StateTag1 = success ? $"AutoRemoved-{DateTime.UtcNow:MM-dd-yyyy}" : "FailedToRemove";
                }
                catch (Exception ex)
                {
                    result.StateTag1 = $"Error: {ex.Message}";
                }

                results.Add(result);
            }

            return results;
        }

       
    }

}


