using TalentManagement.Models.Common;

namespace TalentManagement.Models.Entities;

public class CVAttributeValue : IConcurrencyEntity
{
    public int Id { get; set; }

    public int CVId { get; set; }
    public virtual CV? CV { get; set; }

    public int AttributeDefinitionId { get; set; }
    public virtual AttributeDefinition? AttributeDefinition { get; set; }

    public string? Value { get; set; }

    public byte[]? RowVersion { get; set; }
}
