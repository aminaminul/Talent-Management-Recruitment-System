namespace TalentManagement.Models.Entities;

public class ProjectTag
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public virtual Project? Project { get; set; }

    public int TagId { get; set; }
    public virtual Tag? Tag { get; set; }
}
