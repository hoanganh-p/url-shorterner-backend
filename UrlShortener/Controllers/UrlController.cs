using Microsoft.AspNetCore.Mvc;
using UrlShortener.Services.Interfaces;
using UrlShortener.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlController : ControllerBase
{
    private readonly IUrlService _urlService;
    private readonly ILogger<UrlController> _logger;

    public UrlController(IUrlService urlService, ILogger<UrlController> logger)
    {
        _urlService = urlService;
        _logger = logger;
    }

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShortUrl([FromBody] CreateRequest req)
    {
        if (!Uri.IsWellFormedUriString(req.OriginalUrl, UriKind.Absolute))
            return BadRequest("Invalid URL");

        var userId = GetUserId();
        var created = await _urlService.CreateAsync(req.OriginalUrl, userId);

        var shortUrl = $"{Request.Scheme}://{Request.Host}/{created.ShortCode}";

        var response = new CreateResponse
        {
            ShortCode = created.ShortCode,
            ShortUrl = shortUrl
        };

        return CreatedAtAction(nameof(RedirectToOriginal), new { code = created.ShortCode }, response);
    }

    [HttpGet("{code}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RedirectToOriginal(string code)
    {
        var data = await _urlService.GetAsync(code);
        if (data == null)
            return NotFound();

        // Increment clicks asynchronously without blocking redirect
        _ = _urlService.IncrementClicksAsync(code);

        return Redirect(data.OriginalUrl);
    }

    [HttpGet("user/all")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUserUrls()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated");

        try
        {
            var urls = await _urlService.GetUserUrlsAsync(userId);
            var response = urls.Select(u => new UrlResponse
            {
                ShortCode = u.ShortCode,
                OriginalUrl = u.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{u.ShortCode}",
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive,
                TotalClicks = u.TotalClicks
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user URLs");
            return StatusCode(500, "Error retrieving URLs");
        }
    }

    [HttpGet("{code}/details")]
    [Authorize]
    [ProducesResponseType(typeof(UrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlDetails(string code)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated");

        try
        {
            var url = await _urlService.GetUserUrlAsync(code, userId);
            if (url == null)
                return NotFound("URL not found or you don't have access");

            var response = new UrlResponse
            {
                ShortCode = url.ShortCode,
                OriginalUrl = url.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{url.ShortCode}",
                CreatedAt = url.CreatedAt,
                IsActive = url.IsActive,
                TotalClicks = url.TotalClicks
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving URL details");
            return StatusCode(500, "Error retrieving URL details");
        }
    }

    [HttpPut("{code}")]
    [Authorize]
    [ProducesResponseType(typeof(UrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUrl(string code, [FromBody] UpdateUrlRequest req)
    {
        if (!Uri.IsWellFormedUriString(req.OriginalUrl, UriKind.Absolute))
            return BadRequest("Invalid URL");

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated");

        try
        {
            var updated = await _urlService.UpdateAsync(code, req.OriginalUrl, userId);
            if (updated == null)
                return NotFound("URL not found or you don't have access");

            var response = new UrlResponse
            {
                ShortCode = updated.ShortCode,
                OriginalUrl = updated.OriginalUrl,
                ShortUrl = $"{Request.Scheme}://{Request.Host}/{updated.ShortCode}",
                CreatedAt = updated.CreatedAt,
                IsActive = updated.IsActive,
                TotalClicks = updated.TotalClicks
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating URL");
            return StatusCode(500, "Error updating URL");
        }
    }

    [HttpDelete("{code}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUrl(string code)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("User not authenticated");

        try
        {
            var deleted = await _urlService.DeleteAsync(code, userId);
            if (!deleted)
                return NotFound("URL not found or you don't have access");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting URL");
            return StatusCode(500, "Error deleting URL");
        }
    }
}
