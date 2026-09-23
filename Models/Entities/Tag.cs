namespace TalentManagement.Models.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<PositionTagFilter> PositionTagFilters { get; set; } = new List<PositionTagFilter>();
    public virtual ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
}
