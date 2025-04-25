using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Interfaces
{
    public interface IGraphADGroupService
    {
        Task<List<ManagedDevice>> GetGroupsAsync(string accessToken);
        Task<Group> CreateGroupAsync();
    }
}
