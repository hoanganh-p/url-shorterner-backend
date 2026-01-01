using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(string userId);
        Task SaveAsync(User user);
    }
}
