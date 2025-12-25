using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UrlShortener.Models;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDynamoDBContext _context;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IDynamoDBContext context, JwtSettings jwtSettings)
        {
            _context = context;
            _jwtSettings = jwtSettings;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var users = await _context.QueryAsync<User>(email, new DynamoDBOperationConfig { IndexName = "EmailIndex" }).GetRemainingAsync();

            return users.FirstOrDefault();
        }


        public string GenerateJwt(User user)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}
