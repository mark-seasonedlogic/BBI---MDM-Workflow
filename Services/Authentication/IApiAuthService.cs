using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Services.Authentication
{
    public interface IApiAuthService
    {
        Task<string> GetAccessTokenAsync();
        Task<Dictionary<string,string>> GetAuthorizationHeaderAsync();
    }

}
