namespace UrlShortener.Api.Services.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string username, IEnumerable<string>? roles = null);
    }
}
