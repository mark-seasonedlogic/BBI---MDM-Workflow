using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.Services.Authentication
{
    public interface IGraphAuthService
    {
        Task<GraphServiceClient> GetAuthenticatedGraphClientAsync();
        Task<string> GetAccessTokenAsync();
    }
}
