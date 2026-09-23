using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Enums;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

[Authorize(Roles = DbInitializer.RecruiterRole)]
public class RecruiterController : Controller
{
    private readonly ApplicationDbContext _context;

    public RecruiterController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var totalPositions = await _context.Positions.CountAsync();
        var activePositions = await _context.Positions.CountAsync(p => p.Status == PositionStatus.Active);
        var totalCandidates = await _context.CandidateProfiles.CountAsync();
        var publishedCvs = await _context.CVs.CountAsync(c => c.Status == CvStatus.Published);
        var draftCvs = await _context.CVs.CountAsync(c => c.Status == CvStatus.Draft);
        var totalAttributes = await _context.AttributeDefinitions.CountAsync();

        var recentPositions = await _context.Positions
            .AsNoTracking()
            .Include(p => p.CVs)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentApplications = await _context.CVs
            .AsNoTracking()
            .Include(c => c.CandidateProfile)
            .Include(c => c.Position)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Take(5)
            .ToListAsync();

        var model = new RecruiterDashboardViewModel
        {
            TotalPositions = totalPositions,
            ActivePositions = activePositions,
            TotalCandidates = totalCandidates,
            TotalPublishedCvs = publishedCvs,
            TotalDraftCvs = draftCvs,
            TotalAttributes = totalAttributes,
            RecentPositions = recentPositions,
            RecentApplications = recentApplications
        };

        return View(model);
    }
}
