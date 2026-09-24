using System.ComponentModel.DataAnnotations;
using TalentManagement.Models.Common;

namespace TalentManagement.ViewModels;

public class ProjectViewModel : IConcurrencyEntity
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Project title is required.")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    [Display(Name = "Project Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Project description is required.")]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "End Date (optional)")]
    public DateTime? EndDate { get; set; }

    [Url(ErrorMessage = "Please enter a valid URL (e.g. https://github.com/project).")]
    [StringLength(1000, ErrorMessage = "URL cannot exceed 1000 characters.")]
    [Display(Name = "Project URL (optional)")]
    public string? ProjectUrl { get; set; }

    [Display(Name = "Technologies / Tags (comma-separated, e.g. SQL, Python, Docker)")]
    public string? TagsRaw { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class ProjectDisplayViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProjectUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsMatchingFilter { get; set; }
}
