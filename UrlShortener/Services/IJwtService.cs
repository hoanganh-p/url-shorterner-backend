namespace UrlShortener.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string username, IEnumerable<string> roles = null);
    }
}
