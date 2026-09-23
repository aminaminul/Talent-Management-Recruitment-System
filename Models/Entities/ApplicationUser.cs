using Microsoft.AspNetCore.Identity;

namespace TalentManagement.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual CandidateProfile? CandidateProfile { get; set; }
    public virtual ICollection<Position> CreatedPositions { get; set; } = new List<Position>();
}
