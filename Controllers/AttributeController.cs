using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Controllers;

[Authorize(Roles = DbInitializer.RecruiterRole)]
public class AttributeController : Controller
{
    private readonly IAttributeService _attributeService;
    private readonly ILogger<AttributeController> _logger;

    public AttributeController(IAttributeService attributeService, ILogger<AttributeController> logger)
    {
        _attributeService = attributeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? categoryId, string? search, int page = 1)
    {
        var pageSize = 10;
        var attributes = await _attributeService.GetAttributesAsync(categoryId, search, page, pageSize);
        var categories = await _attributeService.GetCategorySelectListAsync();

        var model = new AttributeListViewModel
        {
            Attributes = attributes,
            CategoryOptions = categories,
            SelectedCategoryId = categoryId,
            SearchTerm = search
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var model = await _attributeService.GetAttributeDetailsAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new AttributeCreateViewModel
        {
            CategoryOptions = await _attributeService.GetCategorySelectListAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AttributeCreateViewModel model)
    {
        if (model.AttributeType == AttributeType.Dropdown && string.IsNullOrWhiteSpace(model.OptionsRaw))
        {
            ModelState.AddModelError("OptionsRaw", "Please provide at least one option for dropdown attributes.");
        }

        if (!ModelState.IsValid)
        {
            model.CategoryOptions = await _attributeService.GetCategorySelectListAsync();
            return View(model);
        }

        try
        {
            var created = await _attributeService.CreateAttributeAsync(model);
            TempData["SuccessMessage"] = $"Attribute '{created.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating attribute.");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving the attribute.");
            model.CategoryOptions = await _attributeService.GetCategorySelectListAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var attribute = await _attributeService.GetAttributeByIdAsync(id);
        if (attribute == null)
        {
            return NotFound();
        }

        var optionsRaw = string.Join(Environment.NewLine, attribute.Options.OrderBy(o => o.DisplayOrder).Select(o => o.Value));

        var model = new AttributeEditViewModel
        {
            Id = attribute.Id,
            Name = attribute.Name,
            AttributeType = attribute.AttributeType,
            CategoryId = attribute.CategoryId,
            OptionsRaw = optionsRaw,
            RowVersion = attribute.RowVersion,
            CategoryOptions = await _attributeService.GetCategorySelectListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AttributeEditViewModel model)
    {
        if (model.AttributeType == AttributeType.Dropdown && string.IsNullOrWhiteSpace(model.OptionsRaw))
        {
            ModelState.AddModelError("OptionsRaw", "Please provide at least one option for dropdown attributes.");
        }

        if (!ModelState.IsValid)
        {
            model.CategoryOptions = await _attributeService.GetCategorySelectListAsync();
            return View(model);
        }

        try
        {
            await _attributeService.UpdateAttributeAsync(model);
            TempData["SuccessMessage"] = $"Attribute '{model.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict occurred updating attribute {Id}.", model.Id);
            ViewBag.ConcurrencyConflict = true;
            ModelState.AddModelError(string.Empty, "Someone else modified this record while it was being edited. Your changes were not saved. Please reload the latest version and try again.");

            var latest = await _attributeService.GetAttributeByIdAsync(model.Id);
            if (latest != null)
            {
                ViewBag.LatestRowVersion = latest.RowVersion;
            }

            model.CategoryOptions = await _attributeService.GetCategorySelectListAsync();
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating attribute {Id}.", model.Id);
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while saving changes.");
            model.CategoryOptions = await _attributeService.GetCategorySelectListAsync();
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var model = await _attributeService.GetAttributeDetailsAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, byte[]? rowVersion)
    {
        try
        {
            var (success, message) = await _attributeService.DeleteAttributeAsync(id, rowVersion);
            if (!success)
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while attempting to delete attribute {Id}.", id);
            TempData["ErrorMessage"] = "This record was modified by another user and could not be deleted. Please reload the latest version.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attribute {Id}.", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the attribute. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
