using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories
{
    public interface IAuthRepository
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
