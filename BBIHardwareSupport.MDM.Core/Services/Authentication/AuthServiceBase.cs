namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication
{
    public abstract class AuthServiceBase : IApiAuthService
    {
        protected string _cachedToken;
        protected DateTime _tokenExpiry;

        public abstract Task<string> GetAccessTokenAsync();

        public abstract Task<Dictionary<string, string>> GetAuthorizationHeaderAsync();
        protected bool IsTokenValid() =>
            !string.IsNullOrWhiteSpace(_cachedToken) && DateTime.UtcNow < _tokenExpiry;
    }

}
