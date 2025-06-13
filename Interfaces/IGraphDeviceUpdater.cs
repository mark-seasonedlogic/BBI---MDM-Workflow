using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface IGraphDeviceUpdater
    {
        Task UpdateIntuneDeviceCustomAttributesAsync(string managedDeviceId, Dictionary<string, string> customAttributes);
        Task RenameDeviceBasedOnConventionAsync(string managedDeviceId, string concept, string restaurantNumber, string deviceFunction, string deviceNumber);
    }

}
