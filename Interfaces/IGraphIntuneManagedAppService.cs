using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface IGraphIntuneManagedAppService
    {
        Task<JObject?> GetManagedAppByIdAsync(string appId);
        Task<JObject?> GetManagedAppByNameAsync(string appName);
        Task<JObject?> AssignAppToGroupAsync(string appId, string groupId);
    }

}
