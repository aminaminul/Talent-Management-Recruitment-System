namespace TalentManagement.Models.Entities;

public class AttributeOption
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }
    public virtual AttributeDefinition? AttributeDefinition { get; set; }

    public string Value { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
}
