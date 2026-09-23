using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using TalentManagement.Services.Interfaces;

namespace TalentManagement.Services.Implementations;

public class CloudinaryStorageService : IFileStorageService
{
    private readonly Cloudinary? _cloudinary;
    private readonly IFileStorageService _fallbackLocalStorage;
    private readonly ILogger<CloudinaryStorageService> _logger;

    public CloudinaryStorageService(
        IConfiguration configuration,
        LocalStorageService fallbackLocalStorage,
        ILogger<CloudinaryStorageService> logger)
    {
        _fallbackLocalStorage = fallbackLocalStorage;
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (!string.IsNullOrWhiteSpace(cloudName) &&
            !string.IsNullOrWhiteSpace(apiKey) &&
            !string.IsNullOrWhiteSpace(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
            _logger.LogInformation("Cloudinary service initialized with cloud {CloudName}", cloudName);
        }
        else
        {
            _logger.LogInformation("Cloudinary credentials not configured; using LocalStorageService fallback.");
        }
    }

    public async Task<(string Url, string? PublicId)> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        if (_cloudinary == null)
        {
            return await _fallbackLocalStorage.UploadImageAsync(file, folder, cancellationToken);
        }

        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file was provided.", nameof(file));
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            throw new InvalidOperationException("File size exceeds the 2MB limit.");
        }

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = $"talent_management/{folder}",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);
        if (uploadResult.Error != null)
        {
            _logger.LogError("Cloudinary upload error: {Message}", uploadResult.Error.Message);
            throw new InvalidOperationException($"Image upload failed: {uploadResult.Error.Message}");
        }

        return (uploadResult.SecureUrl?.ToString() ?? uploadResult.Url.ToString(), uploadResult.PublicId);
    }

    public async Task<bool> DeleteImageAsync(string? publicIdOrUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicIdOrUrl))
        {
            return false;
        }

        if (_cloudinary == null || publicIdOrUrl.StartsWith("/"))
        {
            return await _fallbackLocalStorage.DeleteImageAsync(publicIdOrUrl, cancellationToken);
        }

        try
        {
            var deletionParams = new DeletionParams(publicIdOrUrl);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Cloudinary asset {PublicId}", publicIdOrUrl);
            return false;
        }
    }
}
