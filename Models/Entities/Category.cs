namespace TalentManagement.Models.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<AttributeDefinition> Attributes { get; set; } = new List<AttributeDefinition>();
}
