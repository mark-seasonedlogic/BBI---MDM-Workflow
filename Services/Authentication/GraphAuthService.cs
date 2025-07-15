using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Services.Authentication
{
    public class GraphAuthService : AuthServiceBase, IGraphAuthService
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly ClientSecretCredential _credential;
        private GraphServiceClient? _graphClient;

        public GraphAuthService(string tenantId, string clientId, string clientSecret)
        {
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));

            _credential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
        }

        public override async Task<string> GetAccessTokenAsync()
        {
            if (IsTokenValid()) return _cachedToken;

            var tokenRequestContext = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
            var tokenResult = await _credential.GetTokenAsync(tokenRequestContext);

            _cachedToken = tokenResult.Token;
            _tokenExpiry = tokenResult.ExpiresOn.UtcDateTime.AddMinutes(-1); // buffer to prevent expiry
            return _cachedToken;
        }

        public async Task<GraphServiceClient> GetAuthenticatedGraphClientAsync()
        {
            if (_graphClient != null)
                return _graphClient;

            _graphClient = new GraphServiceClient(_credential);
            return _graphClient;
        }

        public override Task<Dictionary<string, string>> GetAuthorizationHeaderAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetDecodedAccessTokenAsync()
        {
            var token = await GetAccessTokenAsync();
            var parts = token.Split('.');

            if (parts.Length != 3)
                throw new InvalidOperationException("Invalid JWT token format.");

            var payload = parts[1];
            var jsonBytes = Convert.FromBase64String(PadBase64(payload));
            return Encoding.UTF8.GetString(jsonBytes);
        }

        private string PadBase64(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: return base64 + "==";
                case 3: return base64 + "=";
                case 0: return base64;
                default: throw new FormatException("Invalid Base64 string.");
            }
        }
    }
}
