using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services
{
    public interface IGraphADDeviceService
    {
        Task<List<ManagedDevice>> GetDevicesAsync(string accessToken);
    }
}
