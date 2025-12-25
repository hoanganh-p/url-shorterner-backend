using Microsoft.AspNetCore.Mvc;
using UrlShortener.Services.Interfaces;
using UrlShortener.DTOs;

namespace UrlShortener.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        //[HttpPost("login")]
        //public async Task<IActionResult> Login([FromBody] LoginRequest req)
        //{
        //    var user = await _authService.GetByEmailAsync(req.Email);
        //    if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        //        return Unauthorized();

        //    var token = _authService.GenerateJwt(user);
        //    return Ok(new { accessToken = token });
        //}
    }

}
