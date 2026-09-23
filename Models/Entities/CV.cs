using TalentManagement.Models.Common;
using TalentManagement.Models.Enums;

namespace TalentManagement.Models.Entities;

public class CV : IConcurrencyEntity
{
    public int Id { get; set; }

    public int CandidateProfileId { get; set; }
    public virtual CandidateProfile? CandidateProfile { get; set; }

    public int PositionId { get; set; }
    public virtual Position? Position { get; set; }

    public CvStatus Status { get; set; } = CvStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<CVAttributeValue> AttributeValues { get; set; } = new List<CVAttributeValue>();
}
