using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOneManager.Interfaces
{
    public interface IWorkspaceOneAdminsService
    {
        Task<WorkspaceOneAdminUser?> GetAdminByUsernameAsync(string username);
    }


}
