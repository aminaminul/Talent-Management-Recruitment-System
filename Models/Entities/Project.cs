using TalentManagement.Models.Common;

namespace TalentManagement.Models.Entities;

public class Project : IConcurrencyEntity
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }
    public virtual CandidateProfile? CandidateProfile { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
}
