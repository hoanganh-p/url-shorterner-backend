using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<User?> GetByEmailAsync(string email);
    }

}
