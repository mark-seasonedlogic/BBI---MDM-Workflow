using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface ISchemaExtensionRegistrar
    {
        Task<bool> RegisterBbiEntraGroupExtensionAsync();
    }

}
