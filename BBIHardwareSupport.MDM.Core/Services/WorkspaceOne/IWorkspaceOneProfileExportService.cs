using BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public interface IWorkspaceOneProfileExportService
    {
        Task<WorkspaceOneProfileExport> BuildProfileExportAsync(WorkspaceOneProfileSummary summary, CancellationToken ct);
    }
}
