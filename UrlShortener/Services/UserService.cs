using UrlShortener.Models;
using UrlShortener.Repositories;
using UrlShortener.Services.Interfaces;

namespace UrlShortener.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public Task<User?> GetUserByUsernameAsync(string username)
        {
            return _repository.GetByUsernameAsync(username);
        }

        public Task<User?> GetUserByIdAsync(string userId)
        {
            return _repository.GetByIdAsync(userId);
        }

        public async Task<bool> CreateUserAsync(User user)
        {
            try
            {
                // Check if username already exists
                var existingUser = await GetUserByUsernameAsync(user.Username);
                if (existingUser != null)
                {
                    return false;
                }

                user.UserId = Guid.NewGuid().ToString();
                user.CreatedAt = DateTime.UtcNow;
                user.PasswordHash = HashPassword(user.PasswordHash); // Hash password before saving

                await _repository.SaveAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateUserCredentialsAsync(string username, string password)
        {
            var user = await GetUserByUsernameAsync(username);

            if (user == null || !user.IsActive)
            {
                return false;
            }

            return VerifyPassword(password, user.PasswordHash);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
