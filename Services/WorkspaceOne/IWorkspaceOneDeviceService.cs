using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using BBIHardwareSupport.MDM.WorkspaceOneManager.Models;
using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces
{
    public interface IWorkspaceOneDeviceService
    {

        Task<bool> RemoveDeviceAsync(int deviceId);
        Task<List<JObject>> GetAllAndroidDevicesByOrgExAsync(string orgId, Action<WorkspaceOnePagingProgress>? progress = null);
        Task<List<DeviceRemovalResult>> RemoveBulkDevicesBySerialAsync(List<DeviceRemovalRequest> requests, string username);
        Task<string> GetAccessTokenAsync();
        string GetLoggedInUsername();
        Task<int?> LookupDeviceIdBySerialAsync(string serialNumber);
        Task<List<DeviceRemovalResult>> ProcessDeviceRemovalAsync(List<DeviceRemovalRequest> requests);

        object GenerateBulkDeletePayloadAsync(List<DeviceRemovalRequest> requests);
    }

}
