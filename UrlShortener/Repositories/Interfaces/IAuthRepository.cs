using UrlShortener.Models;

namespace UrlShortener.Repositories
{
    public interface IAuthRepository
    {
        Task<User?> GetByEmailAsync(string email);
    }
}
