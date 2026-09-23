using TalentManagement.Models.Entities;

namespace TalentManagement.ViewModels;

public class RecruiterDashboardViewModel
{
    public int TotalPositions { get; set; }
    public int ActivePositions { get; set; }
    public int TotalCandidates { get; set; }
    public int TotalPublishedCvs { get; set; }
    public int TotalDraftCvs { get; set; }
    public int TotalAttributes { get; set; }

    public List<Position> RecentPositions { get; set; } = new();
    public List<CV> RecentApplications { get; set; } = new();
}
