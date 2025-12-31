using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
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
