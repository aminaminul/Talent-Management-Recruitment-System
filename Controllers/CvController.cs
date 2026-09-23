using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

[Authorize]
public class CvController : Controller
{
    private readonly ICvService _cvService;
    private readonly ICandidateProfileService _candidateProfileService;
    private readonly ILogger<CvController> _logger;

    public CvController(
        ICvService cvService,
        ICandidateProfileService candidateProfileService,
        ILogger<CvController> logger)
    {
        _cvService = cvService;
        _candidateProfileService = candidateProfileService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.CandidateRole)]
    public async Task<IActionResult> MyCvs()
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null)
        {
            return RedirectToAction("Create", "Candidate");
        }

        var cvs = await _cvService.GetCandidateCvsAsync(profile.Id);
        return View(cvs);
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.CandidateRole)]
    public async Task<IActionResult> Create(int positionId)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null)
        {
            return RedirectToAction("Profile", "Candidate");
        }

        var model = await _cvService.BuildCvCreateEditViewModelAsync(positionId, profile.Id);
        if (model == null)
        {
            return NotFound();
        }

        return View("Edit", model);
    }

    [HttpGet]
    [Authorize(Roles = DbInitializer.CandidateRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null)
        {
            return RedirectToAction("Profile", "Candidate");
        }

        var cvDetails = await _cvService.GetCvDetailsAsync(id, profile.Id, false);
        if (cvDetails == null)
        {
            return NotFound();
        }

        var model = await _cvService.BuildCvCreateEditViewModelAsync(cvDetails.PositionId, profile.Id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = DbInitializer.CandidateRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CvCreateEditViewModel model, string submitType)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null)
        {
            return RedirectToAction("Profile", "Candidate");
        }

        model.IsPublishAttempt = string.Equals(submitType, "publish", StringComparison.OrdinalIgnoreCase);

        try
        {
            var (success, validationFailed, message, cvId) = await _cvService.SaveCvAsync(model, profile.Id);

            if (validationFailed)
            {
                ModelState.AddModelError(string.Empty, message);
                ViewBag.ValidationAlert = message;
                return View("Edit", model);
            }

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View("Edit", model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id = cvId });
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while updating CV for position {PositionId}.", model.PositionId);
            ViewBag.ConcurrencyConflict = true;
            ModelState.AddModelError(string.Empty, "Someone else modified this record while it was being edited. Your changes were not saved. Please reload the latest version and try again.");
            return View("Edit", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving CV.");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving your CV.");
            return View("Edit", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        int? candidateProfileId = null;
        var isRecruiter = User.IsInRole(DbInitializer.RecruiterRole);

        if (!isRecruiter)
        {
            var profile = await GetCurrentCandidateProfileAsync();
            if (profile == null)
            {
                return Forbid();
            }
            candidateProfileId = profile.Id;
        }

        var model = await _cvService.GetCvDetailsAsync(id, candidateProfileId, isRecruiter);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = DbInitializer.CandidateRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, byte[]? rowVersion)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null) return Forbid();

        try
        {
            await _cvService.DeleteCvAsync(id, profile.Id, rowVersion);
            TempData["SuccessMessage"] = "CV was deleted successfully.";
            return RedirectToAction(nameof(MyCvs));
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "This record was modified by another user and could not be deleted. Please reload the latest version.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<TalentManagement.Models.Entities.CandidateProfile?> GetCurrentCandidateProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await _candidateProfileService.GetProfileByUserIdAsync(userId);
    }
}
