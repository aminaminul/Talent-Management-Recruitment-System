using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models;
using TalentManagement.Models.Enums;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var activePositions = await _context.Positions
            .AsNoTracking()
            .Include(p => p.TagFilters)
                .ThenInclude(tf => tf.Tag)
            .Where(p => p.Status == PositionStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .ToListAsync();

        ViewBag.TotalPositions = await _context.Positions.CountAsync(p => p.Status == PositionStatus.Active);
        ViewBag.TotalCandidates = await _context.CandidateProfiles.CountAsync();

        return View(activePositions);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exceptionFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorMessage = exceptionFeature?.Error?.Message,
            ExceptionType = exceptionFeature?.Error?.GetType().Name
        });
    }
}
