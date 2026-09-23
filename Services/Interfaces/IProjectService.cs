using TalentManagement.Models.Entities;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Interfaces;

public interface IProjectService
{
    Task<List<Project>> GetProjectsForCandidateAsync(int candidateProfileId);
    Task<Project?> GetProjectByIdAsync(int id, int candidateProfileId);
    Task<ProjectViewModel?> BuildEditViewModelAsync(int id, int candidateProfileId);
    Task<Project> CreateProjectAsync(ProjectViewModel model, int candidateProfileId);
    Task UpdateProjectAsync(ProjectViewModel model, int candidateProfileId);
    Task DeleteProjectAsync(int id, int candidateProfileId, byte[]? rowVersion);
}
