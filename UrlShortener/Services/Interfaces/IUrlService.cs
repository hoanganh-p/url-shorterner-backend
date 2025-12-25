using UrlShortener.Models;
namespace UrlShortener.Services.Interfaces;
public interface IUrlService
{
    Task<UrlMapping> CreateAsync(string originalUrl);
    Task<UrlMapping?> GetAsync(string shortCode);
}
