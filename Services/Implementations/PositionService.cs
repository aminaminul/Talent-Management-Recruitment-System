using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Implementations;

public class PositionService : IPositionService
{
    private readonly ApplicationDbContext _context;

    public PositionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<Position>> GetPositionsAsync(string? search, PositionStatus? status, int pageIndex, int pageSize)
    {
        var query = _context.Positions
            .AsNoTracking()
            .Include(p => p.CreatedByRecruiter)
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
            .Include(p => p.TagFilters)
                .ThenInclude(ptf => ptf.Tag)
            .Include(p => p.CVs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        query = query.OrderByDescending(p => p.CreatedAt);

        return await PaginatedList<Position>.CreateAsync(query, pageIndex, pageSize);
    }

    public async Task<Position?> GetPositionByIdAsync(int id)
    {
        return await _context.Positions
            .Include(p => p.CreatedByRecruiter)
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(a => a!.Category)
            .Include(p => p.TagFilters)
                .ThenInclude(ptf => ptf.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PositionDetailsViewModel?> GetPositionDetailsAsync(int id, int? currentCandidateProfileId = null)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(p => p.CreatedByRecruiter)
            .Include(p => p.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(a => a!.Category)
            .Include(p => p.TagFilters)
                .ThenInclude(ptf => ptf.Tag)
            .Include(p => p.CVs)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null) return null;

        var userCv = currentCandidateProfileId.HasValue
            ? position.CVs.FirstOrDefault(c => c.CandidateProfileId == currentCandidateProfileId.Value)
            : null;

        return new PositionDetailsViewModel
        {
            Id = position.Id,
            Title = position.Title,
            Description = position.Description,
            Status = position.Status,
            MaxRecentProjects = position.MaxRecentProjects,
            RecruiterName = position.CreatedByRecruiter?.FullName ?? position.CreatedByRecruiter?.Email ?? "Recruiter",
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt,
            RowVersion = position.RowVersion,
            Attributes = position.PositionAttributes.ToList(),
            TagFilters = position.TagFilters.Select(tf => tf.Tag?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList(),
            TotalApplicationsCount = position.CVs.Count,
            PublishedApplicationsCount = position.CVs.Count(c => c.Status == CvStatus.Published),
            HasCandidateApplied = userCv != null,
            CandidateCvId = userCv?.Id
        };
    }

    public async Task<PositionCreateViewModel> BuildCreateViewModelAsync()
    {
        var allAttributes = await _context.AttributeDefinitions
            .AsNoTracking()
            .Include(a => a.Category)
            .OrderBy(a => a.Category!.Name)
            .ThenBy(a => a.Name)
            .ToListAsync();

        return new PositionCreateViewModel
        {
            MaxRecentProjects = 4,
            AvailableAttributes = allAttributes.Select(a => new PositionAttributeSelectionItem
            {
                AttributeDefinitionId = a.Id,
                AttributeName = a.Name,
                AttributeType = a.AttributeType,
                CategoryName = a.Category?.Name ?? "General",
                IsSelected = false,
                IsRequired = false,
                DisplayOrder = 1
            }).ToList()
        };
    }

    public async Task<PositionEditViewModel?> BuildEditViewModelAsync(int id)
    {
        var position = await _context.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.TagFilters)
                .ThenInclude(ptf => ptf.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null) return null;

        var allAttributes = await _context.AttributeDefinitions
            .AsNoTracking()
            .Include(a => a.Category)
            .OrderBy(a => a.Category!.Name)
            .ThenBy(a => a.Name)
            .ToListAsync();

        var existingMap = position.PositionAttributes
            .ToDictionary(pa => pa.AttributeDefinitionId, pa => pa);

        var tagsString = string.Join(", ", position.TagFilters.Select(tf => tf.Tag?.Name).Where(t => !string.IsNullOrEmpty(t)));

        return new PositionEditViewModel
        {
            Id = position.Id,
            Title = position.Title,
            Description = position.Description,
            Status = position.Status,
            MaxRecentProjects = position.MaxRecentProjects,
            TagsRaw = tagsString,
            RowVersion = position.RowVersion,
            AvailableAttributes = allAttributes.Select(a =>
            {
                var isSelected = existingMap.TryGetValue(a.Id, out var pa);
                return new PositionAttributeSelectionItem
                {
                    AttributeDefinitionId = a.Id,
                    AttributeName = a.Name,
                    AttributeType = a.AttributeType,
                    CategoryName = a.Category?.Name ?? "General",
                    IsSelected = isSelected,
                    IsRequired = isSelected && pa!.IsRequired,
                    DisplayOrder = isSelected ? pa!.DisplayOrder : 1
                };
            }).ToList()
        };
    }

    public async Task<Position> CreatePositionAsync(PositionCreateViewModel model, string recruiterId)
    {
        var position = new Position
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Status = model.Status,
            MaxRecentProjects = model.MaxRecentProjects,
            CreatedByRecruiterId = recruiterId,
            CreatedAt = DateTime.UtcNow
        };

        var order = 1;
        foreach (var item in model.AvailableAttributes.Where(a => a.IsSelected).OrderBy(a => a.DisplayOrder))
        {
            position.PositionAttributes.Add(new PositionAttribute
            {
                AttributeDefinitionId = item.AttributeDefinitionId,
                IsRequired = item.IsRequired,
                DisplayOrder = item.DisplayOrder > 0 ? item.DisplayOrder : order++
            });
        }

        await SyncPositionTagsAsync(position, model.TagsRaw);

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        return position;
    }

    public async Task UpdatePositionAsync(PositionEditViewModel model)
    {
        var position = await _context.Positions
            .Include(p => p.PositionAttributes)
            .Include(p => p.TagFilters)
            .FirstOrDefaultAsync(p => p.Id == model.Id);

        if (position == null)
        {
            throw new KeyNotFoundException($"Position {model.Id} not found.");
        }

        if (model.RowVersion != null)
        {
            _context.Entry(position).Property(p => p.RowVersion).OriginalValue = model.RowVersion;
        }

        position.Title = model.Title.Trim();
        position.Description = model.Description.Trim();
        position.Status = model.Status;
        position.MaxRecentProjects = model.MaxRecentProjects;
        position.UpdatedAt = DateTime.UtcNow;

        var selected = model.AvailableAttributes.Where(a => a.IsSelected).ToList();
        var selectedIds = selected.Select(s => s.AttributeDefinitionId).ToHashSet();

        var toRemove = position.PositionAttributes
            .Where(pa => !selectedIds.Contains(pa.AttributeDefinitionId))
            .ToList();

        foreach (var item in toRemove)
        {
            position.PositionAttributes.Remove(item);
        }

        var order = 1;
        foreach (var item in selected)
        {
            var existingPa = position.PositionAttributes.FirstOrDefault(pa => pa.AttributeDefinitionId == item.AttributeDefinitionId);
            if (existingPa != null)
            {
                existingPa.IsRequired = item.IsRequired;
                existingPa.DisplayOrder = item.DisplayOrder > 0 ? item.DisplayOrder : order++;
            }
            else
            {
                position.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeDefinitionId = item.AttributeDefinitionId,
                    IsRequired = item.IsRequired,
                    DisplayOrder = item.DisplayOrder > 0 ? item.DisplayOrder : order++
                });
            }
        }

        await SyncPositionTagsAsync(position, model.TagsRaw);

        await _context.SaveChangesAsync();
    }

    public async Task DeletePositionAsync(int id, byte[]? rowVersion)
    {
        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position == null) return;

        if (rowVersion != null)
        {
            _context.Entry(position).Property(p => p.RowVersion).OriginalValue = rowVersion;
        }

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();
    }

    public async Task<PositionCandidateCvsViewModel?> GetPositionCandidateCvsAsync(int positionId)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(p => p.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(a => a!.Category)
            .Include(p => p.TagFilters)
                .ThenInclude(ptf => ptf.Tag)
            .Include(p => p.CVs)
                .ThenInclude(c => c.CandidateProfile)
                    .ThenInclude(cp => cp!.Projects)
                        .ThenInclude(pr => pr.ProjectTags)
                            .ThenInclude(pt => pt.Tag)
            .Include(p => p.CVs)
                .ThenInclude(c => c.AttributeValues)
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position == null) return null;

        var filterTagNames = position.TagFilters
            .Select(tf => tf.Tag?.Name ?? string.Empty)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidateCvs = new List<CvDetailsViewModel>();

        foreach (var cv in position.CVs.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt))
        {
            var profile = cv.CandidateProfile;
            if (profile == null) continue;

            var valueMap = cv.AttributeValues.ToDictionary(v => v.AttributeDefinitionId, v => v.Value);

            var attrDisplayList = position.PositionAttributes
                .OrderBy(pa => pa.DisplayOrder)
                .Select(pa =>
                {
                    valueMap.TryGetValue(pa.AttributeDefinitionId, out var val);
                    return new CvAttributeDisplayViewModel
                    {
                        AttributeDefinitionId = pa.AttributeDefinitionId,
                        AttributeName = pa.AttributeDefinition?.Name ?? "Attribute",
                        AttributeType = pa.AttributeDefinition?.AttributeType ?? AttributeType.String,
                        CategoryName = pa.AttributeDefinition?.Category?.Name ?? "General",
                        IsRequired = pa.IsRequired,
                        Value = val
                    };
                })
                .ToList();

            var matchingProjects = profile.Projects
                .Select(pr =>
                {
                    var projTags = pr.ProjectTags.Select(pt => pt.Tag?.Name ?? string.Empty).Where(t => !string.IsNullOrEmpty(t)).ToList();
                    var hasMatchingTag = projTags.Any(t => filterTagNames.Contains(t));
                    return new ProjectDisplayViewModel
                    {
                        Id = pr.Id,
                        Title = pr.Title,
                        Description = pr.Description,
                        ProjectUrl = pr.ProjectUrl,
                        StartDate = pr.StartDate,
                        EndDate = pr.EndDate,
                        Tags = projTags,
                        IsMatchingFilter = hasMatchingTag
                    };
                })
                .OrderByDescending(x => x.IsMatchingFilter)
                .ThenByDescending(x => x.EndDate ?? x.StartDate)
                .Take(position.MaxRecentProjects)
                .ToList();

            candidateCvs.Add(new CvDetailsViewModel
            {
                CvId = cv.Id,
                CandidateProfileId = profile.Id,
                CandidateName = profile.FullName,
                CandidateEmail = profile.Email,
                CandidatePhone = profile.PhoneNumber,
                CandidateBio = profile.Bio,
                CandidateProfileImage = profile.ProfileImageUrl,
                PositionId = position.Id,
                PositionTitle = position.Title,
                Status = cv.Status,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt,
                RowVersion = cv.RowVersion,
                Attributes = attrDisplayList,
                MatchingProjects = matchingProjects,
                MaxRecentProjects = position.MaxRecentProjects
            });
        }

        return new PositionCandidateCvsViewModel
        {
            PositionId = position.Id,
            PositionTitle = position.Title,
            MaxRecentProjects = position.MaxRecentProjects,
            FilterTags = filterTagNames.ToList(),
            PositionAttributes = position.PositionAttributes.ToList(),
            CandidateCvs = candidateCvs
        };
    }

    private async Task SyncPositionTagsAsync(Position position, string? tagsRaw)
    {
        position.TagFilters.Clear();

        if (string.IsNullOrWhiteSpace(tagsRaw)) return;

        var tagNames = tagsRaw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in tagNames)
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == name);
            if (tag == null)
            {
                tag = new Tag { Name = name };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }

            position.TagFilters.Add(new PositionTagFilter
            {
                Position = position,
                Tag = tag,
                TagId = tag.Id
            });
        }
    }
}
