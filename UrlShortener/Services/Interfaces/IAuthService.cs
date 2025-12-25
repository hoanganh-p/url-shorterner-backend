using UrlShortener.Models;

namespace UrlShortener.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> GetByEmailAsync(string email);
        string GenerateJwt(User user);
    }

}
