using System.ComponentModel.DataAnnotations;
using TalentManagement.Models.Common;
using TalentManagement.Models.Entities;

namespace TalentManagement.ViewModels;

public class CandidateProfileViewModel : IConcurrencyEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(150, ErrorMessage = "Full Name cannot exceed 150 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number.")]
    [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters.")]
    [Display(Name = "Professional Bio")]
    public string? Bio { get; set; }

    [Display(Name = "Profile Photo")]
    public string? ProfileImageUrl { get; set; }

    [Display(Name = "Upload New Photo (JPG, PNG, WebP - max 2MB)")]
    public IFormFile? ProfileImageFile { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class CandidateDashboardViewModel
{
    public string CandidateName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int TotalApplicationsCount { get; set; }
    public int PublishedApplicationsCount { get; set; }
    public int DraftApplicationsCount { get; set; }
    public int TotalProjectsCount { get; set; }

    public List<CV> MyCvs { get; set; } = new();
    public List<Position> AvailablePositions { get; set; } = new();
    public List<Project> RecentProjects { get; set; } = new();
}
