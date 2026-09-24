using System.ComponentModel.DataAnnotations;
using TalentManagement.Models.Common;
using TalentManagement.Models.Enums;

namespace TalentManagement.ViewModels;

public class CvAttributeInputViewModel
{
    public int AttributeDefinitionId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }

    public string? Value { get; set; }
    public List<string> DropdownOptions { get; set; } = new();
    public bool HasValidationError { get; set; }
    public string? ValidationErrorMessage { get; set; }
}

public class CvCreateEditViewModel : IConcurrencyEntity
{
    public int? CvId { get; set; }
    public int PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public string PositionDescription { get; set; } = string.Empty;

    public CvStatus Status { get; set; } = CvStatus.Draft;
    public byte[]? RowVersion { get; set; }

    public List<CvAttributeInputViewModel> Attributes { get; set; } = new();

    public bool IsPublishAttempt { get; set; }
}

public class CvAttributeDisplayViewModel
{
    public int AttributeDefinitionId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? Value { get; set; }

    public string FormattedValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                return "Empty / Not provided";
            }

            if (AttributeType == AttributeType.Boolean)
            {
                var parts = Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                bool isTrue = parts.Any(p => p.Equals("true", StringComparison.OrdinalIgnoreCase) || p == "1" || p.Equals("on", StringComparison.OrdinalIgnoreCase) || p.Equals("yes", StringComparison.OrdinalIgnoreCase));
                return isTrue ? "Yes" : "No";
            }

            return Value;
        }
    }
}

public class CvDetailsViewModel : IConcurrencyEntity
{
    public int CvId { get; set; }
    public int CandidateProfileId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string CandidateEmail { get; set; } = string.Empty;
    public string? CandidatePhone { get; set; }
    public string? CandidateBio { get; set; }
    public string? CandidateProfileImage { get; set; }

    public int PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;

    public CvStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public List<CvAttributeDisplayViewModel> Attributes { get; set; } = new();
    public List<ProjectDisplayViewModel> MatchingProjects { get; set; } = new();
    public int MaxRecentProjects { get; set; }
}
