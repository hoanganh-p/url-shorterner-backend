using UrlShortener.Api.Models;
namespace UrlShortener.Api.Services.Interfaces;
public interface IUrlService
{
    Task<Url> CreateAsync(string originalUrl, string? userId = null);
    Task<Url?> GetAsync(string shortCode);
    Task<IEnumerable<Url>> GetUserUrlsAsync(string userId);
    Task<Url?> UpdateAsync(string shortCode, string originalUrl, string userId);
    Task<bool> DeleteAsync(string shortCode, string userId);
    Task<Url?> GetUserUrlAsync(string shortCode, string userId);
    Task<Url?> IncrementClicksAsync(string shortCode);
}
