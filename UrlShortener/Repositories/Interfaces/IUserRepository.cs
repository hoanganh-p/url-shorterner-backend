using UrlShortener.Models;

namespace UrlShortener.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(string userId);
        Task SaveAsync(User user);
    }
}
