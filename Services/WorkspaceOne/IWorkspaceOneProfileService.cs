using BBIHardwareSupport.MDM.WorkspaceOne.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BBIHardwareSupport.MDM.IntuneConfigManager.Services.WorkspaceOne
{
    public interface IWorkspaceOneProfileService
    {
        Task<List<WorkspaceOneProfileSummary>> GetAllProfilesAsync();
        Task<WorkspaceOneProfileSummary> GetProfileByIdAsync(int profileId);
        Task<WorkspaceOneProfileDetails> GetProfileDetailsAsync(int profileId);
        Task<List<WorkspaceOneProfileDetails>> GetProfileDetailsBySummaryList(List<WorkspaceOneProfileSummary> profileSummaries);

        // NEW: thin transport method (HTTP POST to WS1)
        Task<WorkspaceOneProfileDetails> CreateProfileAsync(
            WorkspaceOneProfileCreateRequest request,
            CancellationToken ct = default);

    }
}
