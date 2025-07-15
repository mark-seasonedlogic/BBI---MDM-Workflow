using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using Microsoft.Graph.Models;
using System.Linq;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public class GraphAuthHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string _clientId = "c937126d-5291-47ed-8e01-3cb0fd4e1dfb";
        private readonly string _tenantId = "dd656aba-7b3a-4606-a1d5-b1d05cad986b";
        private readonly string _clientSecret = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET");
        private readonly string[] _scopes = { "https://graph.microsoft.com/.default" }; // Application permissions
        private readonly string _authority;
        private readonly IConfidentialClientApplication _app;
        private string _accessToken;

        public GraphAuthHelper()
        {
            _authority = $"https://login.microsoftonline.com/{_tenantId}";

            _app = ConfidentialClientApplicationBuilder.Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority(new Uri(_authority))
                .Build();
        }

        public async Task<List<ManagedDevice>> GetManagedDevicesBySerialAsync(string serialNumber)
        {
            var graphClient = GetAuthenticatedClient();
            var devices = new List<ManagedDevice>();

            try
            {
                // Use the Intune `managedDevices` API instead of `devices`
                var queryFilter = $"serialNumber eq '{serialNumber}'";

                var deviceResults = await graphClient.DeviceManagement.ManagedDevices
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = queryFilter;
                        requestConfiguration.QueryParameters.Select = new[] { "id", "deviceName", "serialNumber", "operatingSystem" }; // ✅ Select serialNumber
                    });

                // Paginate through results if needed
                while (deviceResults?.Value != null)
                {
                    devices.AddRange(deviceResults.Value);

                    if (!string.IsNullOrEmpty(deviceResults.OdataNextLink))
                    {
                        deviceResults = await graphClient.DeviceManagement.ManagedDevices
                            .WithUrl(deviceResults.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                logger.Debug($"✅ Retrieved {devices.Count} devices matching serial: {serialNumber}");
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving managed devices: {ex.Message}");
            }

            return devices;
        }

        public async Task GetIntuneDeviceExtensionsAsync(string serialNumber)
        {
            var graphClient = GetAuthenticatedClient();

            try
            {
                var deviceQuery = await graphClient.DeviceManagement.ManagedDevices
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"serialNumber eq '{serialNumber}'";
                        requestConfiguration.QueryParameters.Select = new[] { "id", "deviceName", "ManagedDeviceOwnerType", "deviceCategory" };
                    });

                var device = deviceQuery?.Value?.FirstOrDefault();
                if (device == null)
                {
                    logger.Info($"❌ No Intune device found for serial: {serialNumber}");
                    return;
                }
                string deviceOwnerType = device.ManagedDeviceOwnerType == null ? "N/A" : device.ManagedDeviceOwnerType.ToString();
                string deviceCategory = device.DeviceCategory == null ? "N/A" : device.DeviceCategory.ToString();

                logger.Debug($"✅ Found Intune Device: {device.DeviceName}");
                logger.Debug($"🔹 Owner Type: {deviceOwnerType}");
                logger.Debug($"🔹 Device Category: {deviceCategory}");
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving Intune device attributes: {ex.Message}");
            }
        }

        public async Task AddCustomSchemaAttributesToManagedDeviceAsync(string managedDeviceId, Dictionary<string, string> customAttributes)
        {
            var graphClient = GetAuthenticatedClient();

            try
            {
                var updateData = new Dictionary<string, object>();

                foreach (var kvp in customAttributes)
                {
                    updateData[$"comBBIHardwareSupport_{kvp.Key}"] = kvp.Value; // Prefix with Schema ID
                }

                await graphClient.DeviceManagement.ManagedDevices[managedDeviceId]
                    .PatchAsync(new Microsoft.Graph.Models.ManagedDevice
                    {
                        AdditionalData = updateData
                    });

                logger.Info($"✅ Successfully updated schema attributes for device {managedDeviceId}");
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error updating schema attributes: {ex.Message}");
            }
        }
        /// <summary>
        /// Acquires an access token for the Graph API using the client credentials flow.  This will prompt for a user login.
        /// It is necessary to allow the application to create schema extensions.
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetDelegatedAccessTokenAsync()
        {
            var app = PublicClientApplicationBuilder.Create(_clientId)
                .WithRedirectUri("http://localhost")  // Required for interactive login
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{_tenantId}"))
                .Build();

            try
            {
                // Try silent authentication first
                var accounts = await app.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();

                if (firstAccount != null)
                {
                    logger.Info("🔑 Attempting silent authentication...");
                    var result = await app.AcquireTokenSilent(new[] { "Directory.AccessAsUser.All" }, firstAccount).ExecuteAsync();
                    return result.AccessToken;
                }
                else
                {
                   logger.Info("🔄 No cached user found. Prompting for login...");
                    var result = await app.AcquireTokenInteractive(new[] { "Directory.AccessAsUser.All" }).ExecuteAsync();
                    return result.AccessToken;
                }
            }
            catch (MsalUiRequiredException)
            {
                // If token is expired or unavailable, force interactive login
                logger.Error("🔄 User interaction required for login.");
                var result = await app.AcquireTokenInteractive(new[] { "Directory.AccessAsUser.All" }).ExecuteAsync();
                return result.AccessToken;
            }
        }

        /// <summary>
        /// Acquires an application access token without user interaction.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return _accessToken;

            try
            {
                var result = await _app.AcquireTokenForClient(_scopes)
                    .WithForceRefresh(true)
                    .ExecuteAsync();
                _accessToken = result.AccessToken;
                logger.Debug($"🔑 Acquired access token: {_accessToken}");
                return _accessToken;
            }
            catch (MsalException ex)
            {
                logger.Error($"Authentication failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates an authenticated GraphServiceClient using the application token.
        /// </summary>
        public GraphServiceClient GetAuthenticatedClient()
        {
            var accessTokenProvider = new TokenProvider(this);
            var authProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
            var graphClient = new GraphServiceClient(new HttpClientRequestAdapter(authProvider));

            return graphClient;
        }


        
        public async Task<string> GetDeviceCompliancePoliciesJsonAsync()
        {
            var graphClient = GetAuthenticatedClient();

            try
            {
                var compliancePolicies = await graphClient.DeviceManagement.DeviceCompliancePolicies.GetAsync();
                var allPolicies = new List<object>();

                while (compliancePolicies?.Value != null)
                {
                    allPolicies.AddRange(compliancePolicies.Value);

                    if (!string.IsNullOrEmpty(compliancePolicies.OdataNextLink))
                    {
                        compliancePolicies = await graphClient.DeviceManagement.DeviceCompliancePolicies
                            .WithUrl(compliancePolicies.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                string jsonOutput = JsonConvert.SerializeObject(allPolicies, Newtonsoft.Json.Formatting.Indented);
                logger.Debug($"📌 Compliance Policies (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving compliance policies: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GetDeviceConfigurationProfilesJsonAsync()
        {
            var graphClient = GetAuthenticatedClient();

            try
            {
                var deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations.GetAsync();
                var allConfigurations = new List<object>();

                while (deviceConfigurations?.Value != null)
                {
                    allConfigurations.AddRange(deviceConfigurations.Value);

                    if (!string.IsNullOrEmpty(deviceConfigurations.OdataNextLink))
                    {
                        deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations
                            .WithUrl(deviceConfigurations.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                string jsonOutput = JsonConvert.SerializeObject(allConfigurations, Newtonsoft.Json.Formatting.Indented);
                logger.Debug($"⚙️ Device Configuration Profiles (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving device configuration profiles: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GetAppProtectionPoliciesJsonAsync()
        {
            var graphClient = GetAuthenticatedClient();

            try
            {
                var appProtectionPolicies = await graphClient.DeviceAppManagement.TargetedManagedAppConfigurations.GetAsync();
                var allPolicies = new List<object>();

                while (appProtectionPolicies?.Value != null)
                {
                    allPolicies.AddRange(appProtectionPolicies.Value);

                    if (!string.IsNullOrEmpty(appProtectionPolicies.OdataNextLink))
                    {
                        appProtectionPolicies = await graphClient.DeviceAppManagement.TargetedManagedAppConfigurations
                            .WithUrl(appProtectionPolicies.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                string jsonOutput = JsonConvert.SerializeObject(allPolicies, Newtonsoft.Json.Formatting.Indented);
                logger.Debug($"🛡️ App Protection Policies (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving app protection policies: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GetEndpointSecurityPoliciesJsonAsync()
        {
            var graphClient = GetAuthenticatedClient();

            try
            {
                // Endpoint Security policies are stored under DeviceConfigurations with specific categories
                var securityPolicies = await graphClient.DeviceManagement.DeviceConfigurations
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = "contains(displayName, 'Endpoint Security')";
                    });

                var allPolicies = new List<object>();

                while (securityPolicies?.Value != null)
                {
                    allPolicies.AddRange(securityPolicies.Value);

                    if (!string.IsNullOrEmpty(securityPolicies.OdataNextLink))
                    {
                        securityPolicies = await graphClient.DeviceManagement.DeviceConfigurations
                            .WithUrl(securityPolicies.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                string jsonOutput = JsonConvert.SerializeObject(allPolicies, Newtonsoft.Json.Formatting.Indented);
                logger.Debug($"🔐 Endpoint Security Policies (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving endpoint security policies: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public async Task<List<string>> GetDeviceCompliancePoliciesAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var policies = new List<string>();

            try
            {
                var compliancePolicies = await graphClient.DeviceManagement.DeviceCompliancePolicies.GetAsync();

                while (compliancePolicies?.Value != null)
                {
                    foreach (var policy in compliancePolicies.Value)
                    {
                        policies.Add($"Compliance Policy: {policy.DisplayName} (ID: {policy.Id})");
                    }

                    // Handle pagination
                    if (!string.IsNullOrEmpty(compliancePolicies.OdataNextLink))
                    {
                        compliancePolicies = await graphClient.DeviceManagement.DeviceCompliancePolicies
                            .WithUrl(compliancePolicies.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error retrieving compliance policies: {ex.Message}");
            }

            return policies;
        }

 

        /// <summary>
        /// Retrieves all device configuration profiles, including restriction policies.
        /// </summary>
        public async Task<List<string>> GetDeviceConfigurationProfilesAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var configurations = new List<string>();

            try
            {
                var deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations.GetAsync();

                while (deviceConfigurations?.Value != null)
                {
                    foreach (var config in deviceConfigurations.Value)
                    {
                        configurations.Add($"Device Configuration: {config.DisplayName} (ID: {config.Id})");
                    }

                    if (!string.IsNullOrEmpty(deviceConfigurations.OdataNextLink))
                    {
                        deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations
                            .WithUrl(deviceConfigurations.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving device configuration profiles: {ex.Message}");
            }

            return configurations;
        }


        /// <summary>
        /// Retrieves all device configuration profiles assigned to a group.
        /// </summary>
        public async Task<List<string>> GetDeviceConfigurationsForGroupAsync(string groupId)
        {
            var graphClient = GetAuthenticatedClient();
            var configurations = new List<string>();

            try
            {
                var deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations.GetAsync();

                // Loop through paginated results
                while (deviceConfigurations != null && deviceConfigurations.Value != null)
                {
                    foreach (var config in deviceConfigurations.Value)
                    {
                        var assignments = await graphClient.DeviceManagement.DeviceConfigurations[config.Id].Assignments.GetAsync();

                        if (assignments?.Value != null)
                        {
                            foreach (var assignment in assignments.Value)
                            {
                                if (assignment.Target is Microsoft.Graph.Models.GroupAssignmentTarget groupTarget &&
                                    groupTarget.GroupId == groupId)
                                {
                                    configurations.Add($"Device Configuration: {config.DisplayName} (ID: {config.Id})");
                                }
                            }
                        }
                    }

                    // Check for more pages
                    if (!string.IsNullOrEmpty(deviceConfigurations.OdataNextLink))
                    {
                        deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations
                            .WithUrl(deviceConfigurations.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving device configurations for group {groupId}: {ex.Message}");
            }
            return configurations;
        }
        /// <summary>
        /// Retrieves all applications assigned to a group.
        /// </summary>
        public async Task<List<string>> GetAppsForGroupAsync(string groupId)
        {
            var graphClient = GetAuthenticatedClient();
            var apps = new List<string>();

            try
            {
                var applications = await graphClient.DeviceAppManagement.MobileApps
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = $"assignments/any(a:a/target/groupId eq '{groupId}')";
                    });

                if (applications?.Value != null)
                {
                    foreach (var app in applications.Value)
                    {
                        apps.Add(app.DisplayName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving applications for group {groupId}: {ex.Message}");
            }
            return apps;
        }
        public async Task<List<Device>> GetDevicesByFilterAsync(string filterKey, string filterValue)
        {
            var graphClient = GetAuthenticatedClient();
            var devices = new List<Device>();

            try
            {
                // Construct OData filter query dynamically
                var queryFilter = $"{filterKey} eq '{filterValue}'";

                var deviceResults = await graphClient.Devices
                    .GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Filter = queryFilter;
                        requestConfiguration.QueryParameters.Select = new[] { "id", "displayName", "operatingSystem", "physicalIds" }; // ✅ Explicitly request 'physicalIds'
                    });

                // Paginate through results if needed
                while (deviceResults?.Value != null)
                {
                    devices.AddRange(deviceResults.Value);

                    if (!string.IsNullOrEmpty(deviceResults.OdataNextLink))
                    {
                        deviceResults = await graphClient.Devices
                            .WithUrl(deviceResults.OdataNextLink)
                            .GetAsync();
                    }
                    else
                    {
                        break;
                    }
                }

                logger.Debug($"✅ Retrieved {devices.Count} devices matching {filterKey}: {filterValue}");
            }
            catch (Exception ex)
            {
                logger.Error($"❌ Error retrieving devices: {ex.Message}");
            }

            return devices;
        }

        private class TokenProvider : IAccessTokenProvider
        {
            private readonly GraphAuthHelper _authHelper;

            public TokenProvider(GraphAuthHelper authHelper)
            {
                _authHelper = authHelper;
            }

            public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
            {
                return await _authHelper.GetAccessTokenAsync();
            }

            public AllowedHostsValidator AllowedHostsValidator { get; } = new AllowedHostsValidator();
        }
    }
}
