using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public class GraphAuthService : IGraphAuthService
    {
        private GraphServiceClient? _graphClient;

        public async Task<GraphServiceClient> GetAuthenticatedGraphClientAsync()
        {
            if (_graphClient != null)
                return _graphClient;

            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? throw new InvalidOperationException("Missing tenant ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? throw new InvalidOperationException("Missing client ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? throw new InvalidOperationException("Missing client secret");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
            _graphClient = new GraphServiceClient(clientSecretCredential);

            return await Task.FromResult(_graphClient);
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? throw new InvalidOperationException("Missing tenant ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? throw new InvalidOperationException("Missing client ID");
            var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? throw new InvalidOperationException("Missing client secret");

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

            return token.Token;
        }
    }
}
