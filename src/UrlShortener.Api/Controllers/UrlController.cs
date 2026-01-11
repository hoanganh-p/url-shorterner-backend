using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Services.Interfaces;
using UrlShortener.Api.DTOs;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UrlShortener.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UrlController : ControllerBase
{
    private readonly IUrlService _urlService;
    private readonly IMapper _mapper;
    private readonly ILogger<UrlController> _logger;

    public UrlController(IUrlService urlService, IMapper mapper, ILogger<UrlController> logger)
    {
        _urlService = urlService;
        _mapper = mapper;
        _logger = logger;
    }

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShortUrl([FromBody] CreateRequest req)
    {
        // Validate if the input string is a valid absolute URL
        if (!Uri.IsWellFormedUriString(req.OriginalUrl, UriKind.Absolute))
            return BadRequest("Invalid URL");

        var userId = GetUserId();
        var created = await _urlService.CreateAsync(req.OriginalUrl, userId);

        var response = _mapper.Map<CreateResponse>(created, opts => opts.Items["BaseUrl"] = GetBaseUrl());
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
        await _urlService.IncrementClicksAsync(code);

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
            var response = _mapper.Map<IEnumerable<UrlResponse>>(urls, opts => opts.Items["BaseUrl"] = GetBaseUrl());
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

            var response = _mapper.Map<UrlResponse>(url, opts => opts.Items["BaseUrl"] = GetBaseUrl());
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
        // Validate URL format
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

            var response = _mapper.Map<UrlResponse>(updated, opts => opts.Items["BaseUrl"] = GetBaseUrl());
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
