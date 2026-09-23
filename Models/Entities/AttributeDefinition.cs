using TalentManagement.Models.Common;
using TalentManagement.Models.Enums;

namespace TalentManagement.Models.Entities;

public class AttributeDefinition : IConcurrencyEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AttributeType AttributeType { get; set; }

    public int CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public byte[]? RowVersion { get; set; }

    public virtual ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
    public virtual ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public virtual ICollection<CVAttributeValue> CVAttributeValues { get; set; } = new List<CVAttributeValue>();
}
