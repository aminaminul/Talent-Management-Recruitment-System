using TalentManagement.Models.Entities;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Interfaces;

public interface ICvService
{
    Task<CvCreateEditViewModel?> BuildCvCreateEditViewModelAsync(int positionId, int candidateProfileId);
    Task<(bool Success, bool ValidationFailed, string Message, int? CvId)> SaveCvAsync(CvCreateEditViewModel model, int candidateProfileId);
    Task<CvDetailsViewModel?> GetCvDetailsAsync(int cvId, int? requestingCandidateProfileId = null, bool isRecruiter = false);
    Task<List<CV>> GetCandidateCvsAsync(int candidateProfileId);
    Task DeleteCvAsync(int cvId, int candidateProfileId, byte[]? rowVersion);
}
