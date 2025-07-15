using BBIHardwareSupport.MDM.Services.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Services.Authentication
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
