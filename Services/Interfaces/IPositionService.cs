using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Interfaces;

public interface IPositionService
{
    Task<PaginatedList<Position>> GetPositionsAsync(string? search, PositionStatus? status, int pageIndex, int pageSize);
    Task<Position?> GetPositionByIdAsync(int id);
    Task<PositionDetailsViewModel?> GetPositionDetailsAsync(int id, int? currentCandidateProfileId = null);
    Task<PositionCreateViewModel> BuildCreateViewModelAsync();
    Task<PositionEditViewModel?> BuildEditViewModelAsync(int id);
    Task<Position> CreatePositionAsync(PositionCreateViewModel model, string recruiterId);
    Task UpdatePositionAsync(PositionEditViewModel model);
    Task DeletePositionAsync(int id, byte[]? rowVersion);
    Task<PositionCandidateCvsViewModel?> GetPositionCandidateCvsAsync(int positionId);
}
