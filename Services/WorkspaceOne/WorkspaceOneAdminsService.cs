using BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne
{


    public sealed class WorkspaceOneAdminsService : WorkspaceOneServiceBase, IWorkspaceOneAdminsService
    {
        public WorkspaceOneAdminsService(HttpClient httpClient, IWorkspaceOneAuthService authService)
            : base(httpClient, authService) { }

        public async Task<WorkspaceOneAdminUser?> GetAdminByUsernameAsync(string username)
        {
            // NOTE: your BaseUri currently ends with "/API" :contentReference[oaicite:4]{index=4}
            // SendRequestAsync() prepends BaseUri automatically :contentReference[oaicite:5]{index=5}

            var encoded = Uri.EscapeDataString(username);
            var endpoint = $"/system/admins/search?username={encoded}";

            var json = await SendRequestAsync(endpoint, HttpMethod.Get, content: null, accept: "application/json");
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var parsed = JsonConvert.DeserializeObject<WorkspaceOneAdminsSearchResponse>(json);
            return parsed?.Admins?.Count > 0 ? parsed.Admins[0] : null;
        }
    }

}
