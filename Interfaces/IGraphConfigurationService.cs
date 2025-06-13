using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface IGraphIntuneConfigurationService
    {
        Task<JObject?> GetConfigurationByIdAsync(string configId);
        Task<List<JObject>> GetAllConfigurationsAsync();
        Task<List<JObject>> GetOemConfigurationsAsync();
        Task<JObject> FindManagedAppConfigurationByTargetedAppAsync(string applicationName, string? platformTypeHint = null);
        Task<JObject?> GetConfigurationByAppIdAsync(string appId);
    }

}
