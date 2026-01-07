using BBIHardwareSupport.MDM.IntuneConfigManager.BBIHardwareSupport.MDM.Core.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;


namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
{
    public interface IWorkspaceOneProfileService
    {
        Task<List<WorkspaceOneProfileSummary>> GetAllProfilesAsync();
        Task<WorkspaceOneProfileSummary> GetProfileByIdAsync(int profileId);
        Task<WorkspaceOneProfileDetails> GetProfilePayloadAndExportAsync(int profileId);
        Task<WorkspaceOneProfileDetails> GetProfilePayloadDetailsAsync(string profileUuid);
        Task<List<WorkspaceOneProfileDetails>> GetProfileDetailsBySummaryList(List<WorkspaceOneProfileSummary> profileSummaries);
        Task<string> GetProfileDetailsRawAsync(int profileId, CancellationToken ct = default);

        Task<string> GetProfilePayloadDetailsRawAsync(string profileUuid, CancellationToken ct = default);


        // NEW: thin transport method (HTTP POST to WS1)
        Task<WorkspaceOneProfileDetails> CreateProfileAsync(
            WorkspaceOneProfileCreateRequest request,
            CancellationToken ct = default);

    }
}
