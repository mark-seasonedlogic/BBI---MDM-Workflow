using BBIHardwareSupport.MDM.WorkspaceOne.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication
{
    public interface IWorkspaceOneAuthService
    {
        Task<bool> AuthenticateAsync(string username, string password);
        Task<string> GetAccessTokenAsync();
        Task<Dictionary<string,string>> GetAuthorizationHeaderAsync();
        string Username { get; }
        string Password { get; }
        string BaseUri { get; }
        string TenantCode { get; }
        bool IsAuthenticated { get; }
        Uri GetBaseUri();
        void SetCredentials(string username, string password, string apiKey);
        void SetCredentials(string username, string password, WorkspaceOneEnvironment environment);
    }
}
