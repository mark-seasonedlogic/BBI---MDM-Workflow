using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication
{
    public sealed class WorkspaceOneAuthService : AuthServiceBase, IWorkspaceOneAuthService
    {
        private readonly HttpClient _httpClient;

        private string? _username;
        private string? _password;
        private string? _tenantCode; // this is your API key / aw-tenant-code header value

        private WorkspaceOneEnvironment _environment = WorkspaceOneEnvironment.Production; // default
        private WorkspaceOneConnectionInfo? _connectionInfo;

        public WorkspaceOneAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        // ---- IWorkspaceOneAuthService required members ----

        public string Username => _username ?? string.Empty;
        public string Password => _password ?? string.Empty;

        // Your interface calls it TenantCode. This is the value used in the aw-tenant-code header.
        public string TenantCode => _tenantCode ?? string.Empty;

        // Your interface calls it BaseUri. We'll expose the resolved BaseUri from env vars.
        public string BaseUri => _connectionInfo?.BaseUri ?? string.Empty;

        // If your consumers depend on this:
        public bool IsAuthenticated =>
    !string.IsNullOrWhiteSpace(_username)
    && !string.IsNullOrWhiteSpace(_password)
    && _connectionInfo is not null
    && !string.IsNullOrWhiteSpace(_connectionInfo.BaseUri)
    && !string.IsNullOrWhiteSpace(_connectionInfo.AwTenantCode);


        /// <summary>
        /// Legacy interface method. For Basic auth, there isn't a separate "login" token flow.
        /// Treat this as "validate that credentials+environment have been set".
        /// </summary>
        public Task<bool> AuthenticateAsync(string username, string password)
        {
            // Preserve old calling pattern: they pass username/password here
            // but tenant code + environment must have been set via SetCredentials.
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));

            return Task.FromResult(IsAuthenticated);
        }

        /// <summary>
        /// Legacy overload: keeps interface compatibility.
        /// Uses the last selected environment (default Production) to resolve BaseUri/TenantId.
        /// </summary>
        public void SetCredentials(string username, string password, string tenantCode)
        {
            SetCredentials(username, password, _environment);
        }

        /// <summary>
        /// Preferred overload going forward: includes environment selection.
        /// </summary>
        public void SetCredentials(string username, string password, WorkspaceOneEnvironment environment)
        {
            _username = username ?? throw new ArgumentNullException(nameof(username));
            _password = password ?? throw new ArgumentNullException(nameof(password));

            _environment = environment;

            _connectionInfo = WorkspaceOneEnvironmentResolver.Resolve(environment);

            // Optional: if this HttpClient is WS1-dedicated, set BaseAddress once
            // _httpClient.BaseAddress = new Uri(_connectionInfo.BaseUri, UriKind.Absolute);
        }


        public Uri GetBaseUri()
        {
            if (_connectionInfo is null)
                throw new InvalidOperationException("Workspace ONE connection info has not been resolved. Call SetCredentials first.");

            return new Uri(_connectionInfo.BaseUri, UriKind.Absolute);
        }

        public override Task<string> GetAccessTokenAsync()
        {
            // For your base class contract: return the Basic token string.
            if (!IsAuthenticated)
                throw new InvalidOperationException("Workspace ONE credentials are not set.");

            var raw = $"{_username}:{_password}";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            return Task.FromResult(encoded);
        }

        public override Task<Dictionary<string, string>> GetAuthorizationHeaderAsync()
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Workspace ONE credentials are not set.");

            if (_connectionInfo is null)
                throw new InvalidOperationException("Workspace ONE connection info has not been resolved. Call SetCredentials first.");

            var raw = $"{_username}:{_password}";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

            return Task.FromResult(new Dictionary<string, string>
    {
        { "Authorization", $"Basic {encoded}" },
        { "aw-tenant-code", _connectionInfo.AwTenantCode }, // ✅ from resolver/env vars now
        { "Accept", "application/json" }
    });
        }


        // If your interface includes this, keep it. Otherwise remove it.
        public string GetTenantCode()
        {
            if (string.IsNullOrWhiteSpace(_tenantCode))
                throw new InvalidOperationException("Workspace ONE tenant code (API key) is not set.");

            return _tenantCode;
        }
    }
}
