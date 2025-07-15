using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BBIHardwareSupport.MDM.Intune.Models.Packaging;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.Packaging
{
    interface IIntunePackageService
    {
        Task<IntunePackageMetadata> LoadMetadataAsync(string path);
        Task<bool> ValidateAgainstLiveTenantAsync(IntunePackageMetadata package);
        Task DeployPackageAsync(IntunePackageMetadata package);
        Task<string> GenerateDiffReportAsync(IntunePackageMetadata package);
    }
}
