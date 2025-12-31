using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;


namespace BBIHardwareSupport.MDM.WorkspaceOne.Core.Services
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
