using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Entities;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;

    public ProjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Project>> GetProjectsForCandidateAsync(int candidateProfileId)
    {
        return await _context.Projects
            .AsNoTracking()
            .Include(p => p.ProjectTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.CandidateProfileId == candidateProfileId)
            .OrderByDescending(p => p.EndDate ?? p.StartDate)
            .ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id, int candidateProfileId)
    {
        return await _context.Projects
            .Include(p => p.ProjectTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id && p.CandidateProfileId == candidateProfileId);
    }

    public async Task<ProjectViewModel?> BuildEditViewModelAsync(int id, int candidateProfileId)
    {
        var project = await GetProjectByIdAsync(id, candidateProfileId);
        if (project == null) return null;

        var tagsString = string.Join(", ", project.ProjectTags.Select(pt => pt.Tag?.Name).Where(t => !string.IsNullOrEmpty(t)));

        return new ProjectViewModel
        {
            Id = project.Id,
            Title = project.Title,
            Description = project.Description,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            TagsRaw = tagsString,
            RowVersion = project.RowVersion
        };
    }

    public async Task<Project> CreateProjectAsync(ProjectViewModel model, int candidateProfileId)
    {
        var project = new Project
        {
            CandidateProfileId = candidateProfileId,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            CreatedAt = DateTime.UtcNow
        };

        await SyncProjectTagsAsync(project, model.TagsRaw);

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }

    public async Task UpdateProjectAsync(ProjectViewModel model, int candidateProfileId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectTags)
            .FirstOrDefaultAsync(p => p.Id == model.Id && p.CandidateProfileId == candidateProfileId);

        if (project == null)
        {
            throw new KeyNotFoundException("Project not found or unauthorized.");
        }

        if (model.RowVersion != null)
        {
            _context.Entry(project).Property(p => p.RowVersion).OriginalValue = model.RowVersion;
        }

        project.Title = model.Title.Trim();
        project.Description = model.Description.Trim();
        project.StartDate = model.StartDate;
        project.EndDate = model.EndDate;
        project.UpdatedAt = DateTime.UtcNow;

        await SyncProjectTagsAsync(project, model.TagsRaw);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteProjectAsync(int id, int candidateProfileId, byte[]? rowVersion)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.CandidateProfileId == candidateProfileId);

        if (project == null) return;

        if (rowVersion != null)
        {
            _context.Entry(project).Property(p => p.RowVersion).OriginalValue = rowVersion;
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    private async Task SyncProjectTagsAsync(Project project, string? tagsRaw)
    {
        project.ProjectTags.Clear();

        if (string.IsNullOrWhiteSpace(tagsRaw)) return;

        var tagNames = tagsRaw
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var name in tagNames)
        {
            var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == name);
            if (tag == null)
            {
                tag = new Tag { Name = name };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
            }

            project.ProjectTags.Add(new ProjectTag
            {
                Project = project,
                Tag = tag,
                TagId = tag.Id
            });
        }
    }
}
