using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Implementations;

public class CvService : ICvService
{
    private readonly ApplicationDbContext _context;

    public CvService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CvCreateEditViewModel?> BuildCvCreateEditViewModelAsync(int positionId, int candidateProfileId)
    {
        var position = await _context.Positions
            .AsNoTracking()
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(a => a!.Options)
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(a => a!.Category)
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position == null) return null;

        var existingCv = await _context.CVs
            .AsNoTracking()
            .Include(c => c.AttributeValues)
            .FirstOrDefaultAsync(c => c.PositionId == positionId && c.CandidateProfileId == candidateProfileId);

        var existingValues = existingCv?.AttributeValues
            .ToDictionary(v => v.AttributeDefinitionId, v => v.Value) 
            ?? new Dictionary<int, string?>();

        var attributeInputs = new List<CvAttributeInputViewModel>();
        foreach (var posAttr in position.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
        {
            var def = posAttr.AttributeDefinition;
            existingValues.TryGetValue(posAttr.AttributeDefinitionId, out var savedValue);

            var options = (def?.AttributeType == AttributeType.Dropdown && def.Options != null)
                ? def.Options.OrderBy(o => o.DisplayOrder).Select(o => o.Value).ToList()
                : new List<string>();

            attributeInputs.Add(new CvAttributeInputViewModel
            {
                AttributeDefinitionId = posAttr.AttributeDefinitionId,
                AttributeName = def?.Name ?? "Attribute",
                AttributeType = def?.AttributeType ?? AttributeType.String,
                CategoryName = def?.Category?.Name ?? "General",
                IsRequired = posAttr.IsRequired,
                DisplayOrder = posAttr.DisplayOrder,
                Value = savedValue,
                DropdownOptions = options
            });
        }

        return new CvCreateEditViewModel
        {
            CvId = existingCv?.Id,
            PositionId = position.Id,
            PositionTitle = position.Title,
            PositionDescription = position.Description,
            Status = existingCv?.Status ?? CvStatus.Draft,
            RowVersion = existingCv?.RowVersion,
            Attributes = attributeInputs
        };
    }

    public async Task<(bool Success, bool ValidationFailed, string Message, int? CvId)> SaveCvAsync(
        CvCreateEditViewModel model, int candidateProfileId)
    {
        var position = await _context.Positions
            .Include(p => p.PositionAttributes)
                .ThenInclude(pa => pa.AttributeDefinition)
                    .ThenInclude(a => a!.Options)
            .FirstOrDefaultAsync(p => p.Id == model.PositionId);

        if (position == null)
        {
            return (false, false, "Position not found.", null);
        }

        var hasValidationErrors = false;
        string? generalErrorMessage = null;

        // 1. Format and range validation for provided attribute values (applies to both draft and publish)
        foreach (var input in model.Attributes)
        {
            var posAttr = position.PositionAttributes.FirstOrDefault(pa => pa.AttributeDefinitionId == input.AttributeDefinitionId);
            var attrDef = posAttr?.AttributeDefinition;
            var attrName = attrDef?.Name ?? input.AttributeName;
            var val = input.Value?.Trim();

            if (!string.IsNullOrWhiteSpace(val))
            {
                if (attrDef?.AttributeType == AttributeType.Number)
                {
                    if (!double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
                    {
                        hasValidationErrors = true;
                        input.HasValidationError = true;
                        input.ValidationErrorMessage = $"{attrName} must be a valid number.";
                        generalErrorMessage ??= $"{attrName} must be a valid number.";
                    }
                    else if (num < 0)
                    {
                        hasValidationErrors = true;
                        input.HasValidationError = true;
                        input.ValidationErrorMessage = $"{attrName} cannot be negative.";
                        generalErrorMessage ??= $"{attrName} cannot be negative.";
                    }
                    else if (attrName.Contains("CGPA", StringComparison.OrdinalIgnoreCase) && num > 4.0)
                    {
                        hasValidationErrors = true;
                        input.HasValidationError = true;
                        input.ValidationErrorMessage = $"{attrName} cannot exceed 4.00.";
                        generalErrorMessage ??= $"{attrName} cannot exceed 4.00 (Scale: 0.00 to 4.00).";
                    }
                    else if (attrName.Contains("GPA", StringComparison.OrdinalIgnoreCase) && num > 5.0)
                    {
                        hasValidationErrors = true;
                        input.HasValidationError = true;
                        input.ValidationErrorMessage = $"{attrName} cannot exceed 5.00.";
                        generalErrorMessage ??= $"{attrName} cannot exceed 5.00 (Scale: 0.00 to 5.00).";
                    }
                }
                else if (attrDef?.AttributeType == AttributeType.Dropdown)
                {
                    var allowedOptions = attrDef.Options
                        .Select(o => o.Value)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (!allowedOptions.Contains(val))
                    {
                        hasValidationErrors = true;
                        input.HasValidationError = true;
                        input.ValidationErrorMessage = "Please select a valid option.";
                        generalErrorMessage ??= "Please select a valid option.";
                    }
                }
            }
        }

        // 2. Completeness validation for publish attempts
        if (model.IsPublishAttempt)
        {
            foreach (var posAttr in position.PositionAttributes.Where(pa => pa.IsRequired))
            {
                var input = model.Attributes.FirstOrDefault(a => a.AttributeDefinitionId == posAttr.AttributeDefinitionId);
                var value = input?.Value?.Trim();

                if (string.IsNullOrWhiteSpace(value))
                {
                    hasValidationErrors = true;
                    if (input != null)
                    {
                        input.HasValidationError = true;
                        input.ValidationErrorMessage = $"{posAttr.AttributeDefinition?.Name ?? "This field"} is required.";
                    }
                    generalErrorMessage ??= "Please fill in all required fields before publishing.";
                }
            }
        }

        if (hasValidationErrors)
        {
            return (false, true, generalErrorMessage ?? "Please correct the errors in the form.", model.CvId);
        }

        CV cv;
        if (model.CvId.HasValue && model.CvId.Value > 0)
        {
            cv = await _context.CVs
                .Include(c => c.AttributeValues)
                .FirstOrDefaultAsync(c => c.Id == model.CvId.Value && c.CandidateProfileId == candidateProfileId)
                ?? throw new KeyNotFoundException("CV not found or access denied.");

            if (model.RowVersion != null)
            {
                _context.Entry(cv).Property(c => c.RowVersion).OriginalValue = model.RowVersion;
            }
            cv.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var existing = await _context.CVs
                .Include(c => c.AttributeValues)
                .FirstOrDefaultAsync(c => c.PositionId == model.PositionId && c.CandidateProfileId == candidateProfileId);

            if (existing != null)
            {
                cv = existing;
                if (model.RowVersion != null)
                {
                    _context.Entry(cv).Property(c => c.RowVersion).OriginalValue = model.RowVersion;
                }
                cv.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cv = new CV
                {
                    PositionId = model.PositionId,
                    CandidateProfileId = candidateProfileId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CVs.Add(cv);
            }
        }

        cv.Status = model.IsPublishAttempt ? CvStatus.Published : CvStatus.Draft;

        foreach (var input in model.Attributes)
        {
            var cleanValue = input.Value?.Trim();

            if (input.AttributeType == AttributeType.Boolean)
            {
                var parts = cleanValue?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
                bool isTrue = parts.Any(p => p.Equals("true", StringComparison.OrdinalIgnoreCase) || p == "1" || p.Equals("on", StringComparison.OrdinalIgnoreCase) || p.Equals("yes", StringComparison.OrdinalIgnoreCase));
                cleanValue = isTrue ? "true" : "false";
            }

            var existingValue = cv.AttributeValues
                .FirstOrDefault(v => v.AttributeDefinitionId == input.AttributeDefinitionId);

            if (existingValue != null)
            {
                existingValue.Value = cleanValue;
            }
            else
            {
                cv.AttributeValues.Add(new CVAttributeValue
                {
                    CV = cv,
                    AttributeDefinitionId = input.AttributeDefinitionId,
                    Value = cleanValue
                });
            }
        }

        await _context.SaveChangesAsync();

        var statusMessage = model.IsPublishAttempt
            ? "Your CV has been successfully published!"
            : "Your CV draft has been saved.";

        return (true, false, statusMessage, cv.Id);
    }

    public async Task<CvDetailsViewModel?> GetCvDetailsAsync(int cvId, int? requestingCandidateProfileId = null, bool isRecruiter = false)
    {
        var cv = await _context.CVs
            .AsNoTracking()
            .Include(c => c.CandidateProfile)
                .ThenInclude(cp => cp!.Projects)
                    .ThenInclude(pr => pr.ProjectTags)
                        .ThenInclude(pt => pt.Tag)
            .Include(c => c.Position)
                .ThenInclude(p => p!.PositionAttributes)
                    .ThenInclude(pa => pa.AttributeDefinition)
                        .ThenInclude(a => a!.Category)
            .Include(c => c.Position)
                .ThenInclude(p => p!.TagFilters)
                    .ThenInclude(ptf => ptf.Tag)
            .Include(c => c.AttributeValues)
            .FirstOrDefaultAsync(c => c.Id == cvId);

        if (cv == null) return null;

        if (!isRecruiter && requestingCandidateProfileId.HasValue && cv.CandidateProfileId != requestingCandidateProfileId.Value)
        {
            return null;
        }

        var candidate = cv.CandidateProfile!;
        var position = cv.Position!;

        var filterTags = position.TagFilters
            .Select(tf => tf.Tag?.Name ?? string.Empty)
            .Where(t => !string.IsNullOrEmpty(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var savedValues = cv.AttributeValues
            .ToDictionary(v => v.AttributeDefinitionId, v => v.Value);

        var displayAttributes = new List<CvAttributeDisplayViewModel>();
        foreach (var posAttr in position.PositionAttributes.OrderBy(pa => pa.DisplayOrder))
        {
            savedValues.TryGetValue(posAttr.AttributeDefinitionId, out var value);
            displayAttributes.Add(new CvAttributeDisplayViewModel
            {
                AttributeDefinitionId = posAttr.AttributeDefinitionId,
                AttributeName = posAttr.AttributeDefinition?.Name ?? "Attribute",
                AttributeType = posAttr.AttributeDefinition?.AttributeType ?? AttributeType.String,
                CategoryName = posAttr.AttributeDefinition?.Category?.Name ?? "General",
                IsRequired = posAttr.IsRequired,
                Value = value
            });
        }

        var matchingProjects = candidate.Projects
            .Select(pr =>
            {
                var tags = pr.ProjectTags
                    .Select(pt => pt.Tag?.Name ?? string.Empty)
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                var isMatch = tags.Any(t => filterTags.Contains(t));

                return new ProjectDisplayViewModel
                {
                    Id = pr.Id,
                    Title = pr.Title,
                    Description = pr.Description,
                    ProjectUrl = pr.ProjectUrl,
                    StartDate = pr.StartDate,
                    EndDate = pr.EndDate,
                    Tags = tags,
                    IsMatchingFilter = isMatch
                };
            })
            .OrderByDescending(p => p.IsMatchingFilter)
            .ThenByDescending(p => p.EndDate ?? p.StartDate)
            .Take(position.MaxRecentProjects)
            .ToList();

        return new CvDetailsViewModel
        {
            CvId = cv.Id,
            CandidateProfileId = candidate.Id,
            CandidateName = candidate.FullName,
            CandidateEmail = candidate.Email,
            CandidatePhone = candidate.PhoneNumber,
            CandidateBio = candidate.Bio,
            CandidateProfileImage = candidate.ProfileImageUrl,
            PositionId = position.Id,
            PositionTitle = position.Title,
            Status = cv.Status,
            CreatedAt = cv.CreatedAt,
            UpdatedAt = cv.UpdatedAt,
            RowVersion = cv.RowVersion,
            Attributes = displayAttributes,
            MatchingProjects = matchingProjects,
            MaxRecentProjects = position.MaxRecentProjects
        };
    }

    public async Task<List<CV>> GetCandidateCvsAsync(int candidateProfileId)
    {
        return await _context.CVs
            .AsNoTracking()
            .Include(c => c.Position)
            .Where(c => c.CandidateProfileId == candidateProfileId)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteCvAsync(int cvId, int candidateProfileId, byte[]? rowVersion)
    {
        var cv = await _context.CVs
            .FirstOrDefaultAsync(c => c.Id == cvId && c.CandidateProfileId == candidateProfileId);

        if (cv == null) return;

        if (rowVersion != null)
        {
            _context.Entry(cv).Property(c => c.RowVersion).OriginalValue = rowVersion;
        }

        _context.CVs.Remove(cv);
        await _context.SaveChangesAsync();
    }
}
