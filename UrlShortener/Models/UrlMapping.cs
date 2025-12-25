using Amazon.DynamoDBv2.DataModel;

namespace UrlShortener.Models;

[DynamoDBTable("UrlMapping")]
public class UrlMapping
{
    [DynamoDBHashKey]
    public string ShortCode { get; set; } = null!;

    [DynamoDBProperty]
    public string OriginalUrl { get; set; } = null!;

    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
