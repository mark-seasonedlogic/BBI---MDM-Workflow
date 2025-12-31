using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication
{
    public interface IApiAuthService
    {
        Task<string> GetAccessTokenAsync();
        Task<Dictionary<string,string>> GetAuthorizationHeaderAsync();
    }

}
