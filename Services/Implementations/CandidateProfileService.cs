using Microsoft.EntityFrameworkCore;
using TalentManagement.Data;
using TalentManagement.Models.Entities;
using TalentManagement.Models.Enums;
using TalentManagement.Services.Interfaces;
using TalentManagement.ViewModels;

namespace TalentManagement.Services.Implementations;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<CandidateProfileService> _logger;

    public CandidateProfileService(
        ApplicationDbContext context,
        IFileStorageService fileStorage,
        ILogger<CandidateProfileService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<CandidateProfile?> GetProfileByUserIdAsync(string userId)
    {
        return await _context.CandidateProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);
    }

    public async Task<CandidateProfileViewModel?> GetProfileViewModelByUserIdAsync(string userId)
    {
        var profile = await GetProfileByUserIdAsync(userId);
        if (profile == null) return null;

        return new CandidateProfileViewModel
        {
            Id = profile.Id,
            FullName = profile.FullName,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            Bio = profile.Bio,
            ProfileImageUrl = profile.ProfileImageUrl,
            RowVersion = profile.RowVersion
        };
    }

    public async Task<CandidateDashboardViewModel> GetDashboardDataAsync(string userId)
    {
        var profile = await _context.CandidateProfiles
            .AsNoTracking()
            .Include(cp => cp.CVs)
                .ThenInclude(c => c.Position)
            .Include(cp => cp.Projects)
                .ThenInclude(p => p.ProjectTags)
                    .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile == null)
        {
            return new CandidateDashboardViewModel();
        }

        var availablePositions = await _context.Positions
            .AsNoTracking()
            .Where(p => p.Status == PositionStatus.Active)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        return new CandidateDashboardViewModel
        {
            CandidateName = profile.FullName,
            Email = profile.Email,
            ProfileImageUrl = profile.ProfileImageUrl,
            TotalApplicationsCount = profile.CVs.Count,
            PublishedApplicationsCount = profile.CVs.Count(c => c.Status == CvStatus.Published),
            DraftApplicationsCount = profile.CVs.Count(c => c.Status == CvStatus.Draft),
            TotalProjectsCount = profile.Projects.Count,
            MyCvs = profile.CVs.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt).ToList(),
            AvailablePositions = availablePositions,
            RecentProjects = profile.Projects.OrderByDescending(p => p.EndDate ?? p.StartDate).Take(4).ToList()
        };
    }

    public async Task UpdateProfileAsync(CandidateProfileViewModel model, string userId)
    {
        var profile = await _context.CandidateProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile == null)
        {
            throw new KeyNotFoundException("Profile not found.");
        }

        if (model.RowVersion != null)
        {
            _context.Entry(profile).Property(cp => cp.RowVersion).OriginalValue = model.RowVersion;
        }

        profile.FullName = model.FullName.Trim();
        profile.PhoneNumber = model.PhoneNumber?.Trim();
        profile.Bio = model.Bio?.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        if (model.ProfileImageFile != null && model.ProfileImageFile.Length > 0)
        {
            var (url, publicId) = await _fileStorage.UploadImageAsync(model.ProfileImageFile, "profiles");

            if (!string.IsNullOrWhiteSpace(profile.ImagePublicId))
            {
                await _fileStorage.DeleteImageAsync(profile.ImagePublicId);
            }
            else if (!string.IsNullOrWhiteSpace(profile.ProfileImageUrl))
            {
                await _fileStorage.DeleteImageAsync(profile.ProfileImageUrl);
            }

            profile.ProfileImageUrl = url;
            profile.ImagePublicId = publicId;
        }

        if (profile.User != null)
        {
            profile.User.FullName = profile.FullName;
        }

        await _context.SaveChangesAsync();
    }
}
