using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Newtonsoft.Json;
using NLog;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public class GraphAuthHelper
    {
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

        /// <summary>
        /// Acquires an application access token without user interaction.
        /// </summary>
        public async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return _accessToken;

            try
            {
                var result = await _app.AcquireTokenForClient(_scopes).ExecuteAsync();
                _accessToken = result.AccessToken;
                return _accessToken;
            }
            catch (MsalException ex)
            {
                Console.WriteLine($"Authentication failed: {ex.Message}");
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


        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
                Logger.Debug($"📌 Compliance Policies (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Error retrieving compliance policies: {ex.Message}");
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
                Logger.Debug($"⚙️ Device Configuration Profiles (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Error retrieving device configuration profiles: {ex.Message}");
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
                Logger.Debug($"🛡️ App Protection Policies (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Error retrieving app protection policies: {ex.Message}");
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
                Logger.Debug($"🔐 Endpoint Security Policies (JSON):\n{jsonOutput}");
                return jsonOutput;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Error retrieving endpoint security policies: {ex.Message}");
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
                Console.WriteLine($"Error retrieving compliance policies: {ex.Message}");
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
