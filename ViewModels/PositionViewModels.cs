using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TalentManagement.Models.Common;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;

namespace TalentManagement.ViewModels;

public class PositionListViewModel
{
    public PaginatedList<Position> Positions { get; set; } = null!;
    public string? SearchTerm { get; set; }
    public PositionStatus? StatusFilter { get; set; }
}

public class PositionAttributeSelectionItem
{
    public int AttributeDefinitionId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}

public class PositionCreateViewModel
{
    [Required(ErrorMessage = "Position title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    [Display(Name = "Job Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position description is required.")]
    [Display(Name = "Job Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Position Status")]
    public PositionStatus Status { get; set; } = PositionStatus.Active;

    [Range(1, 20, ErrorMessage = "Recent projects limit must be between 1 and 20.")]
    [Display(Name = "Maximum Recent Projects to Display")]
    public int MaxRecentProjects { get; set; } = 4;

    [Display(Name = "Project Filter Tags (comma-separated, e.g. SQL, Python, R)")]
    public string? TagsRaw { get; set; }

    public List<PositionAttributeSelectionItem> AvailableAttributes { get; set; } = new();
}

public class PositionEditViewModel : IConcurrencyEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Position title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    [Display(Name = "Job Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position description is required.")]
    [Display(Name = "Job Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Position Status")]
    public PositionStatus Status { get; set; } = PositionStatus.Active;

    [Range(1, 20, ErrorMessage = "Recent projects limit must be between 1 and 20.")]
    [Display(Name = "Maximum Recent Projects to Display")]
    public int MaxRecentProjects { get; set; } = 4;

    [Display(Name = "Project Filter Tags (comma-separated, e.g. SQL, Python, R)")]
    public string? TagsRaw { get; set; }

    public List<PositionAttributeSelectionItem> AvailableAttributes { get; set; } = new();

    public byte[]? RowVersion { get; set; }
}

public class PositionDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PositionStatus Status { get; set; }
    public int MaxRecentProjects { get; set; }
    public string RecruiterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public List<PositionAttribute> Attributes { get; set; } = new();
    public List<string> TagFilters { get; set; } = new();
    public int TotalApplicationsCount { get; set; }
    public int PublishedApplicationsCount { get; set; }
    public bool HasCandidateApplied { get; set; }
    public int? CandidateCvId { get; set; }
}

public class PositionCandidateCvsViewModel
{
    public int PositionId { get; set; }
    public string PositionTitle { get; set; } = string.Empty;
    public int MaxRecentProjects { get; set; }
    public List<string> FilterTags { get; set; } = new();
    public List<PositionAttribute> PositionAttributes { get; set; } = new();
    public List<CvDetailsViewModel> CandidateCvs { get; set; } = new();
}
