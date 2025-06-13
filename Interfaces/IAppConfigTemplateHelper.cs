using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface IAppConfigTemplateHelper
    {
        Task<bool> CreateFromTemplateAsync(string templatePath, string displayName, string restaurantCode);
    }

}
