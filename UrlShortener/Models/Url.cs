using Amazon.DynamoDBv2.DataModel;

namespace UrlShortener.Models;

[DynamoDBTable("Urls")]
public class Url
{
    [DynamoDBHashKey]
    public string ShortCode { get; set; } = null!;

    [DynamoDBProperty]
    public string OriginalUrl { get; set; } = null!;

    [DynamoDBProperty]
    public string? UserId { get; set; }

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DynamoDBProperty]
    public bool IsActive { get; set; } = true;

    [DynamoDBProperty]
    public long TotalClicks { get; set; } = 0;
}
