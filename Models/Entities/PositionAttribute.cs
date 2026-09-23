namespace TalentManagement.Models.Entities;

public class PositionAttribute
{
    public int Id { get; set; }

    public int PositionId { get; set; }
    public virtual Position? Position { get; set; }

    public int AttributeDefinitionId { get; set; }
    public virtual AttributeDefinition? AttributeDefinition { get; set; }

    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; } = 0;
}
