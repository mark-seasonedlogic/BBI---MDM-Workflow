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
        Task<JObject?> GetManagedAppConfigurationByIdAsync(string appId);
        Task<JObject?> CloneManagedAppConfigurationAsync(JObject originalConfig, string newDisplayName, Dictionary<string, object> tokenReplacements = null);
        Task<List<JObject>> FindManagedAppConfigurationsByTargetedAppAsync(string targetedAppId, string? platformTypeHint = null, string? odataAppType = null);
    }

}
