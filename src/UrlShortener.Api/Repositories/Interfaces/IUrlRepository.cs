using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories
{
    public interface IUrlRepository
    {
        Task<Url?> GetByShortCodeAsync(string shortCode);
        Task AddAsync(Url url);
        Task<IEnumerable<Url>> GetAllForUserAsync(string userId);
        Task UpdateAsync(Url url);
        Task DeleteAsync(Url url);
    }
}
