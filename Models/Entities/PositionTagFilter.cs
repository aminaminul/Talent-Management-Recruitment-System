namespace TalentManagement.Models.Entities;

public class PositionTagFilter
{
    public int Id { get; set; }

    public int PositionId { get; set; }
    public virtual Position? Position { get; set; }

    public int TagId { get; set; }
    public virtual Tag? Tag { get; set; }
}
