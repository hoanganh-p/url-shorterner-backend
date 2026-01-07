using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Options
{
    public class JwtOptions
    {
    public const string SectionName = "JwtSettings";

    [Required]
    public string Secret { get; set; } = string.Empty;
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    public int ExpirationInMinutes { get; set; }
    }
}
