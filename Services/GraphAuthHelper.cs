using System;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Collections.Generic;
using System.Threading;

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

        /// <summary>
        /// Retrieves all device configuration profiles assigned to a group.
        /// </summary>
        public async Task<List<string>> GetDeviceConfigurationsForGroupAsync(string groupId)
        {
            var graphClient = GetAuthenticatedClient();
            var configurations = new List<string>();

            try
            {
                // Get all device configurations
                var deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations.GetAsync();

                if (deviceConfigurations?.Value != null)
                {
                    foreach (var config in deviceConfigurations.Value)
                    {
                        // Fetch assignments for each configuration
                        var assignments = await graphClient.DeviceManagement.DeviceConfigurations[config.Id].Assignments.GetAsync();

                        if (assignments?.Value != null)
                        {
                            foreach (var assignment in assignments.Value)
                            {
                                // Check if the assignment targets the specified group
                                if (assignment.Target is Microsoft.Graph.Models.GroupAssignmentTarget groupTarget &&
                                    groupTarget.GroupId == groupId)
                                {
                                    configurations.Add($"Device Configuration: {config.DisplayName} (ID: {config.Id})");
                                }
                            }
                        }
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
