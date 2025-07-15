using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.WorkspaceOne.Models;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne
{
    public interface IWorkspaceOneProfileService
    {
        Task<List<WorkspaceOneProfileSummary>> GetAllProfilesAsync();
        
    }
}
