using Microsoft.AspNetCore.Mvc.Rendering;
using TalentManagement.Models.Entities;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Interfaces;

public interface IAttributeService
{
    Task<PaginatedList<AttributeDefinition>> GetAttributesAsync(int? categoryId, string? search, int pageIndex, int pageSize);
    Task<AttributeDefinition?> GetAttributeByIdAsync(int id);
    Task<AttributeDetailsViewModel?> GetAttributeDetailsAsync(int id);
    Task<AttributeDefinition> CreateAttributeAsync(AttributeCreateViewModel model);
    Task UpdateAttributeAsync(AttributeEditViewModel model);
    Task<(bool Success, string Message)> DeleteAttributeAsync(int id, byte[]? rowVersion);
    Task<List<SelectListItem>> GetCategorySelectListAsync();
}
