using BBIHardwareSupport.MDM.IntuneConfigManager.Models;
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
        Task<List<Group>> GetGroupsAsync(string accessToken);
        Task<string> CreateDynamicGroupAsync(string displayName, string description, string membershipRule, List<string> ownerIds);
        Task<BBIEntraGroupExtension?> GetBbiGroupExtensionAsync(string groupId);
        Task<bool> SetBbiGroupExtensionAsync(string groupId, BBIEntraGroupExtension extension);
        Task AddOrUpdateGroupOpenExtensionAsync(string groupId, IDictionary<string, object> extensionData);
        Task<IDictionary<string, object>> GetOpenExtensionAsync(string groupId, string extensionId);
        Task AddOrUpdateOpenExtensionAsync(string groupId, string extensionId, IDictionary<string, object> extensionData);
        Task<OpenTypeExtension?> GetGroupExtensionMetadataAsync(string groupId, string extensionName);
        Task<Group> FindGroupByDisplayNameAsync(string displayName);


    }
}
