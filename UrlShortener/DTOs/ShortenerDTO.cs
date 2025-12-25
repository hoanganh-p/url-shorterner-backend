using System.ComponentModel.DataAnnotations;

namespace UrlShortener.DTOs
{
    public class CreateRequest
    {
        [Required]
        public string OriginalUrl { get; set; } = null!;
    }

    public class CreateResponse
    {
        public string ShortCode { get; set; } = null!;
        public string ShortUrl { get; set; } = null!;
    }
}
