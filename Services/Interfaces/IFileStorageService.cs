namespace TalentManagement.Services.Interfaces;

public interface IFileStorageService
{
    Task<(string Url, string? PublicId)> UploadImageAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string? publicIdOrUrl, CancellationToken cancellationToken = default);
}
