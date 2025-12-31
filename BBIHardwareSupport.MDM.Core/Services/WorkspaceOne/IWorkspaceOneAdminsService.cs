using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public interface IWorkspaceOneAdminsService
    {
        Task<WorkspaceOneAdminUser?> GetAdminByUsernameAsync(string username);
    }


}
