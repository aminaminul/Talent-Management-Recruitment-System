using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Implementations;

public class AttributeService : IAttributeService
{
    private readonly ApplicationDbContext _context;

    public AttributeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<AttributeDefinition>> GetAttributesAsync(int? categoryId, string? search, int pageIndex, int pageSize)
    {
        var query = _context.AttributeDefinitions
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Options)
            .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(a => a.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(a => a.Name.Contains(search));
        }

        query = query.OrderBy(a => a.Name);

        return await PaginatedList<AttributeDefinition>.CreateAsync(query, pageIndex, pageSize);
    }

    public async Task<AttributeDefinition?> GetAttributeByIdAsync(int id)
    {
        return await _context.AttributeDefinitions
            .Include(a => a.Category)
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AttributeDetailsViewModel?> GetAttributeDetailsAsync(int id)
    {
        var attribute = await _context.AttributeDefinitions
            .AsNoTracking()
            .Include(a => a.Category)
            .Include(a => a.Options.OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attribute == null) return null;

        var positionUsageCount = await _context.PositionAttributes.CountAsync(pa => pa.AttributeDefinitionId == id);
        var cvUsageCount = await _context.CVAttributeValues.CountAsync(cav => cav.AttributeDefinitionId == id);

        return new AttributeDetailsViewModel
        {
            Id = attribute.Id,
            Name = attribute.Name,
            AttributeType = attribute.AttributeType,
            CategoryName = attribute.Category?.Name ?? "Uncategorized",
            CreatedAt = attribute.CreatedAt,
            UpdatedAt = attribute.UpdatedAt,
            RowVersion = attribute.RowVersion,
            Options = attribute.Options.ToList(),
            PositionUsageCount = positionUsageCount,
            CvUsageCount = cvUsageCount
        };
    }

    public async Task<AttributeDefinition> CreateAttributeAsync(AttributeCreateViewModel model)
    {
        var attribute = new AttributeDefinition
        {
            Name = model.Name.Trim(),
            AttributeType = model.AttributeType,
            CategoryId = model.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        if (model.AttributeType == AttributeType.Dropdown && !string.IsNullOrWhiteSpace(model.OptionsRaw))
        {
            var options = model.OptionsRaw
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var order = 1;
            foreach (var opt in options)
            {
                attribute.Options.Add(new AttributeOption
                {
                    Value = opt,
                    DisplayOrder = order++
                });
            }
        }

        _context.AttributeDefinitions.Add(attribute);
        await _context.SaveChangesAsync();

        return attribute;
    }

    public async Task UpdateAttributeAsync(AttributeEditViewModel model)
    {
        var attribute = await _context.AttributeDefinitions
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == model.Id);

        if (attribute == null)
        {
            throw new KeyNotFoundException($"Attribute with ID {model.Id} not found.");
        }

        if (model.RowVersion != null)
        {
            _context.Entry(attribute).Property(a => a.RowVersion).OriginalValue = model.RowVersion;
        }

        attribute.Name = model.Name.Trim();
        attribute.CategoryId = model.CategoryId;
        attribute.UpdatedAt = DateTime.UtcNow;

        if (attribute.AttributeType == AttributeType.Dropdown)
        {
            var raw = model.OptionsRaw ?? string.Empty;
            var options = raw
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _context.AttributeOptions.RemoveRange(attribute.Options);

            var order = 1;
            foreach (var opt in options)
            {
                attribute.Options.Add(new AttributeOption
                {
                    AttributeDefinitionId = attribute.Id,
                    Value = opt,
                    DisplayOrder = order++
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string Message)> DeleteAttributeAsync(int id, byte[]? rowVersion)
    {
        var attribute = await _context.AttributeDefinitions
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attribute == null)
        {
            return (false, "The attribute could not be found.");
        }

        if (rowVersion != null)
        {
            _context.Entry(attribute).Property(a => a.RowVersion).OriginalValue = rowVersion;
        }

        var isUsedInPosition = await _context.PositionAttributes.AnyAsync(pa => pa.AttributeDefinitionId == id);
        if (isUsedInPosition)
        {
            return (false, "Cannot delete this attribute because it is assigned to one or more job positions.");
        }

        var isUsedInCv = await _context.CVAttributeValues.AnyAsync(cav => cav.AttributeDefinitionId == id);
        if (isUsedInCv)
        {
            return (false, "Cannot delete this attribute because candidate CVs currently reference it.");
        }

        _context.AttributeDefinitions.Remove(attribute);
        await _context.SaveChangesAsync();

        return (true, "Attribute was deleted successfully.");
    }

    public async Task<List<SelectListItem>> GetCategorySelectListAsync()
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToListAsync();
    }
}
