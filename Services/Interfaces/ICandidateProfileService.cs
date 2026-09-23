using TalentManagement.Models.Entities;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Interfaces;

public interface ICandidateProfileService
{
    Task<CandidateProfile?> GetProfileByUserIdAsync(string userId);
    Task<CandidateProfileViewModel?> GetProfileViewModelByUserIdAsync(string userId);
    Task<CandidateDashboardViewModel> GetDashboardDataAsync(string userId);
    Task UpdateProfileAsync(CandidateProfileViewModel model, string userId);
}
