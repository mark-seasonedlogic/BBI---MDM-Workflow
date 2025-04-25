using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public interface IGraphAuthService
    {
        Task<GraphServiceClient> GetAuthenticatedGraphClientAsync();
        Task<string> GetAccessTokenAsync();
    }
}
