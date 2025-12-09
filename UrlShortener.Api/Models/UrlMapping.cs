using Amazon.DynamoDBv2.DataModel;

namespace UrlShortener.Api.Models;

[DynamoDBTable("UrlMappings")]
public class UrlMapping
{
    [DynamoDBHashKey]
    public string ShortCode { get; set; }

    public string OriginalUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
