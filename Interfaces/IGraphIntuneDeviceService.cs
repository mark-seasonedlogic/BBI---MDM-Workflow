using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public interface IGraphIntuneDeviceService
    {
        Task<List<ManagedDevice>> GetDevicesAsync(string accessToken);
        Task UpdateDeviceUserNameAsync(string deviceId, string newUserName, string accessToken);

    }
}
