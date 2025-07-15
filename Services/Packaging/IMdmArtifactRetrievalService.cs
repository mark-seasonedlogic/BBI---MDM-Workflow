using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.Generic.Models;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.Packaging
{
    public interface IMdmArtifactRetrievalService
    {
        Task<IEnumerable<MdmDeviceConfiguration>> GetDeviceConfigurationsAsync();
        Task<IEnumerable<MdmCompliancePolicy>> GetCompliancePoliciesAsync();
        Task<IEnumerable<MdmApp>> GetAppsAsync();
        Task<IEnumerable<MdmAppConfiguration>> GetAppConfigurationsAsync();
        Task<IEnumerable<MdmOemProfile>> GetOemConfigProfilesAsync();
        Task<IEnumerable<MdmEnrollmentConfiguration>> GetEnrollmentConfigurationsAsync();
    }
}
