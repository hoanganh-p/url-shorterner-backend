using UrlShortener.Api.Models;
namespace UrlShortener.Api.Services;
public interface IUrlService
{
    Task<UrlMapping> CreateAsync(string originalUrl);
    Task<UrlMapping?> GetAsync(string shortCode);
}
