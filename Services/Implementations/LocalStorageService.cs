using TalentManagement.Services.Interfaces;

namespace TalentManagement.Services.Implementations;

public class LocalStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string[] _allowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxFileSizeInBytes = 2 * 1024 * 1024;

    public LocalStorageService(IWebHostEnvironment environment, ILogger<LocalStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<(string Url, string? PublicId)> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file was uploaded.", nameof(file));
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            throw new InvalidOperationException("File size exceeds the 2MB limit.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Invalid file extension '{extension}'. Allowed extensions: {string.Join(", ", _allowedExtensions)}.");
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only image files are allowed.");
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", folder);
        Directory.CreateDirectory(uploadsRoot);

        var publicId = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}";
        var fileName = $"{publicId}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativeUrl = $"/uploads/{folder}/{fileName}";
        _logger.LogInformation("Saved local image at {Path} with public ID {PublicId}", filePath, publicId);

        return (relativeUrl, publicId);
    }

    public Task<bool> DeleteImageAsync(string? publicIdOrUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicIdOrUrl))
        {
            return Task.FromResult(false);
        }

        try
        {
            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string localPath;

            if (publicIdOrUrl.StartsWith("/"))
            {
                var relativePath = publicIdOrUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                localPath = Path.Combine(webRoot, relativePath);
            }
            else
            {
                var uploadsDir = Path.Combine(webRoot, "uploads");
                var matches = Directory.GetFiles(uploadsDir, $"{publicIdOrUrl}.*", SearchOption.AllDirectories);
                if (matches.Length == 0)
                {
                    return Task.FromResult(false);
                }
                localPath = matches[0];
            }

            if (File.Exists(localPath))
            {
                File.Delete(localPath);
                _logger.LogInformation("Deleted image file at {Path}", localPath);
                return Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {Identifier}", publicIdOrUrl);
        }

        return Task.FromResult(false);
    }
}
