using Microsoft.AspNetCore.Mvc;
using UrlShortener.Services.Interfaces;
using UrlShortener.DTOs;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShortenerController : ControllerBase
{
    private readonly IUrlService _urlService;

    public ShortenerController(IUrlService urlService)
    {
        _urlService = urlService;
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(CreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateShortUrl([FromBody] CreateRequest req)
    {
        if (!Uri.IsWellFormedUriString(req.OriginalUrl, UriKind.Absolute))
            return BadRequest("Invalid URL");

        var created = await _urlService.CreateAsync(req.OriginalUrl);

        var shortUrl = $"{Request.Scheme}://{Request.Host}/{created.ShortCode}";
        //var shortUrl = $"{Request.Scheme}://{Request.Host}/dev/api/shortener/{created.ShortCode}"; ;

        var response = new CreateResponse
        {
            ShortCode = created.ShortCode,
            ShortUrl = shortUrl
        };

        //return Ok(response);
        return CreatedAtAction(nameof(RedirectToOriginal), new { code = created.ShortCode }, response);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> RedirectToOriginal(string code)
    {
        var data = await _urlService.GetAsync(code);
        if (data == null) return NotFound();

        return Redirect(data.OriginalUrl);
    }
}
