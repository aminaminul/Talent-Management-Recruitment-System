using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

[Authorize(Roles = DbInitializer.CandidateRole)]
public class ProjectController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ICandidateProfileService _candidateProfileService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IProjectService projectService,
        ICandidateProfileService candidateProfileService,
        ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _candidateProfileService = candidateProfileService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null) return RedirectToAction("Profile", "Candidate");

        var projects = await _projectService.GetProjectsForCandidateAsync(profile.Id);
        return View(projects);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProjectViewModel { StartDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectViewModel model)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null) return RedirectToAction("Profile", "Candidate");

        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _projectService.CreateProjectAsync(model, profile.Id);
            TempData["SuccessMessage"] = $"Project '{model.Title}' added successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project.");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the project.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null) return RedirectToAction("Profile", "Candidate");

        var model = await _projectService.BuildEditViewModelAsync(id, profile.Id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectViewModel model)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null) return RedirectToAction("Profile", "Candidate");

        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
        {
            ModelState.AddModelError("EndDate", "End date cannot be earlier than start date.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _projectService.UpdateProjectAsync(model, profile.Id);
            TempData["SuccessMessage"] = $"Project '{model.Title}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict editing project {Id}.", model.Id);
            ViewBag.ConcurrencyConflict = true;
            ModelState.AddModelError(string.Empty, "Someone else modified this record while it was being edited. Your changes were not saved. Please reload the latest version and try again.");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {Id}.", model.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the project.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, byte[]? rowVersion)
    {
        var profile = await GetCurrentCandidateProfileAsync();
        if (profile == null) return Forbid();

        try
        {
            await _projectService.DeleteProjectAsync(id, profile.Id, rowVersion);
            TempData["SuccessMessage"] = "Project deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["ErrorMessage"] = "This project was modified by another user and could not be deleted. Please reload the latest version.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task<TalentManagement.Models.Entities.CandidateProfile?> GetCurrentCandidateProfileAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await _candidateProfileService.GetProfileByUserIdAsync(userId);
    }
}
