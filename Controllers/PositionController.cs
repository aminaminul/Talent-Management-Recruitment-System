using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

public class PositionController : Controller
{
    private readonly IPositionService _positionService;
    private readonly ICandidateProfileService _candidateProfileService;
    private readonly ILogger<PositionController> _logger;

    public PositionController(
        IPositionService positionService,
        ICandidateProfileService candidateProfileService,
        ILogger<PositionController> logger)
    {
        _positionService = positionService;
        _candidateProfileService = candidateProfileService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, PositionStatus? status, int page = 1)
    {
        var pageSize = 10;
        var positions = await _positionService.GetPositionsAsync(search, status, page, pageSize);

        var model = new PositionListViewModel
        {
            Positions = positions,
            SearchTerm = search,
            StatusFilter = status
        };

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        int? candidateProfileId = null;
        if (User.Identity?.IsAuthenticated == true && User.IsInRole(DbInitializer.CandidateRole))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var profile = await _candidateProfileService.GetProfileByUserIdAsync(userId);
                candidateProfileId = profile?.Id;
            }
        }

        var model = await _positionService.GetPositionDetailsAsync(id, candidateProfileId);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    public async Task<IActionResult> Create()
    {
        var model = await _positionService.BuildCreateViewModelAsync();
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PositionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var recruiterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(recruiterId))
        {
            return Challenge();
        }

        try
        {
            var created = await _positionService.CreatePositionAsync(model, recruiterId);
            TempData["SuccessMessage"] = $"Position '{created.Title}' created successfully.";
            return RedirectToAction(nameof(Details), new { id = created.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating position.");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the position.");
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var model = await _positionService.BuildEditViewModelAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PositionEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _positionService.UpdatePositionAsync(model);
            TempData["SuccessMessage"] = $"Position '{model.Title}' updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict occurred when editing position {Id}.", model.Id);
            ViewBag.ConcurrencyConflict = true;
            ModelState.AddModelError(string.Empty, "Someone else modified this record while it was being edited. Your changes were not saved. Please reload the latest version and try again.");

            var latest = await _positionService.GetPositionByIdAsync(model.Id);
            if (latest != null)
            {
                ViewBag.LatestRowVersion = latest.RowVersion;
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating position {Id}.", model.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while saving the position changes.");
            return View(model);
        }
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _positionService.GetPositionDetailsAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, byte[]? rowVersion)
    {
        try
        {
            await _positionService.DeletePositionAsync(id, rowVersion);
            TempData["SuccessMessage"] = "Position was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict when deleting position {Id}.", id);
            TempData["ErrorMessage"] = "This record was modified by another user and could not be deleted. Please reload the latest version.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting position {Id}.", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the position.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.RecruiterRole)]
    public async Task<IActionResult> Candidates(int id)
    {
        var model = await _positionService.GetPositionCandidateCvsAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }
}
