using System.Security.Cryptography;
using System.Text;
using UrlShortener.Models;
using UrlShortener.Repositories;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Services;

public class UrlService : IUrlService
{
    private readonly IUrlRepository _repository;
    private static readonly char[] Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private const int ShortCodeLength = 7;
    private readonly ILogger<UrlService> _logger;

    public UrlService(IUrlRepository repository, ILogger<UrlService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    private static string GenerateShortCode()
    {
        Span<byte> buffer = stackalloc byte[ShortCodeLength];
        RandomNumberGenerator.Fill(buffer);
        var sb = new StringBuilder(ShortCodeLength);
        for (int i = 0; i < ShortCodeLength; i++)
        {
            var idx = buffer[i] % Base62Chars.Length;
            sb.Append(Base62Chars[idx]);
        }

        return sb.ToString();
    }

    public async Task<Url> CreateAsync(string originalUrl, string? userId = null)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var uriResult))
            throw new ArgumentException("Invalid URL format", nameof(originalUrl));

        originalUrl = originalUrl.Trim();

        var now = DateTime.UtcNow;
        var code = GenerateShortCode();

        var url = new Url
        {
            ShortCode = code,
            OriginalUrl = originalUrl,
            UserId = userId,
            CreatedAt = now,
            IsActive = true,
            TotalClicks = 0
        };

        try
        {
            await _repository.AddAsync(url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while saving URL");
            throw;
        }
    }

    public Task<Url?> GetAsync(string shortCode) =>
        _repository.GetByShortCodeAsync(shortCode);

    public async Task<IEnumerable<Url>> GetUserUrlsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        try
        {
            return await _repository.GetAllForUserAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving user URLs for userId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Url?> GetUserUrlAsync(string shortCode, string userId)
    {
        if (string.IsNullOrEmpty(shortCode))
            throw new ArgumentException("ShortCode cannot be empty", nameof(shortCode));

        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        try
        {
            var url = await GetAsync(shortCode);
            if (url == null)
                return null;

            if (url.UserId != userId)
                return null;

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving URL for shortCode: {ShortCode}", shortCode);
            throw;
        }
    }

    public async Task<Url?> UpdateAsync(string shortCode, string originalUrl, string userId)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out var uriResult))
            throw new ArgumentException("Invalid URL format", nameof(originalUrl));

        if (string.IsNullOrEmpty(shortCode) || string.IsNullOrEmpty(userId))
            throw new ArgumentException("ShortCode and UserId cannot be empty");

        try
        {
            var url = await GetUserUrlAsync(shortCode, userId);
            if (url == null)
                return null;

            url.OriginalUrl = originalUrl.Trim();
            await _repository.UpdateAsync(url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while updating URL for shortCode: {ShortCode}", shortCode);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string shortCode, string userId)
    {
        if (string.IsNullOrEmpty(shortCode) || string.IsNullOrEmpty(userId))
            throw new ArgumentException("ShortCode and UserId cannot be empty");

        try
        {
            var url = await GetUserUrlAsync(shortCode, userId);
            if (url == null)
                return false;

            await _repository.DeleteAsync(url);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting URL for shortCode: {ShortCode}", shortCode);
            throw;
        }
    }

    public async Task<Url?> IncrementClicksAsync(string shortCode)
    {
        if (string.IsNullOrEmpty(shortCode))
            throw new ArgumentException("ShortCode cannot be empty", nameof(shortCode));

        try
        {
            var url = await GetAsync(shortCode);
            if (url == null)
                return null;

            url.TotalClicks++;
            await _repository.UpdateAsync(url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while incrementing clicks for shortCode: {ShortCode}", shortCode);
            throw;
        }
    }
}