using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TalentManagement.Models.Common;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;

namespace TalentManagement.ViewModels;

public class AttributeListViewModel
{
    public PaginatedList<AttributeDefinition> Attributes { get; set; } = null!;
    public List<SelectListItem> CategoryOptions { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public string? SearchTerm { get; set; }
}

public class AttributeCreateViewModel
{
    [Required(ErrorMessage = "Attribute name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Attribute Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an attribute type.")]
    [Display(Name = "Attribute Type")]
    public AttributeType AttributeType { get; set; } = AttributeType.String;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    [Display(Name = "Options (for Dropdown type, one per line)")]
    public string? OptionsRaw { get; set; }
}

public class AttributeEditViewModel : IConcurrencyEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Attribute name is required.")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    [Display(Name = "Attribute Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Attribute Type")]
    public AttributeType AttributeType { get; set; }

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    [Display(Name = "Options (for Dropdown type, one per line)")]
    public string? OptionsRaw { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class AttributeDetailsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public List<AttributeOption> Options { get; set; } = new();
    public int PositionUsageCount { get; set; }
    public int CvUsageCount { get; set; }
    public bool CanDelete => PositionUsageCount == 0 && CvUsageCount == 0;
}
