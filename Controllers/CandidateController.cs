using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

[Authorize(Roles = DbInitializer.CandidateRole)]
public class CandidateController : Controller
{
    private readonly ICandidateProfileService _profileService;
    private readonly ILogger<CandidateController> _logger;

    public CandidateController(
        ICandidateProfileService profileService,
        ILogger<CandidateController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var model = await _profileService.GetDashboardDataAsync(userId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var model = await _profileService.GetProfileViewModelByUserIdAsync(userId);
        if (model == null)
        {
            return RedirectToAction(nameof(EditProfile));
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var model = await _profileService.GetProfileViewModelByUserIdAsync(userId);
        if (model == null)
        {
            model = new CandidateProfileViewModel
            {
                FullName = User.Identity?.Name ?? string.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
            };
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(CandidateProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _profileService.UpdateProfileAsync(model, userId);
            TempData["SuccessMessage"] = "Your profile was updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict editing profile for user {UserId}.", userId);
            ViewBag.ConcurrencyConflict = true;
            ModelState.AddModelError(string.Empty, "Someone else modified this profile while it was being edited. Your changes were not saved. Please reload the latest version and try again.");
            return View(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("ProfileImageFile", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}.", userId);
            ModelState.AddModelError(string.Empty, "An error occurred while saving your profile.");
            return View(model);
        }
    }
}
