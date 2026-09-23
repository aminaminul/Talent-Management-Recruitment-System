using TalentManagement.Models.Common;
using TalentManagement.Models.Enums;

namespace TalentManagement.Models.Entities;

public class Position : IConcurrencyEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string CreatedByRecruiterId { get; set; } = string.Empty;
    public virtual ApplicationUser? CreatedByRecruiter { get; set; }

    public PositionStatus Status { get; set; } = PositionStatus.Active;
    public int MaxRecentProjects { get; set; } = 4;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public virtual ICollection<PositionTagFilter> TagFilters { get; set; } = new List<PositionTagFilter>();
    public virtual ICollection<CV> CVs { get; set; } = new List<CV>();
}
