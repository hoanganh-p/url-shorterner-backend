using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using UrlShortener.Models;

namespace UrlShortener.Services;

public class UrlService : IUrlService
{
    private readonly IDynamoDBContext _db;
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly string _tableName;
    private static readonly char[] Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    private const int ShortCodeLength = 7;
    private const int MaxCreateAttempts = 10;

    public UrlService(IAmazonDynamoDB dynamoDb, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _dynamoDb = dynamoDb;
        _db = new DynamoDBContext(dynamoDb);
        _tableName = configuration.GetSection("DynamoDB").GetValue<string>("TableName")
                     ?? Environment.GetEnvironmentVariable("DYNAMODB_TABLE")
                     ?? "UrlMappings";
    }

    private static string GenerateShortCode()
    {
        Span<byte> buffer = stackalloc byte[ShortCodeLength];
        RandomNumberGenerator.Fill(buffer);
        var sb = new StringBuilder(ShortCodeLength);
        for (int i = 0; i < ShortCodeLength; i++)
        {
            // Map byte to base62 index
            var idx = buffer[i] % Base62Chars.Length;
            sb.Append(Base62Chars[idx]);
        }

        return sb.ToString();
    }

    public async Task<UrlMapping> CreateAsync(string originalUrl)
    {
        if (string.IsNullOrWhiteSpace(originalUrl))
            throw new ArgumentException("originalUrl must be provided", nameof(originalUrl));

        // Basic normalization
        originalUrl = originalUrl.Trim();

        for (int attempt = 0; attempt < MaxCreateAttempts; attempt++)
        {
            var code = GenerateShortCode();
            var now = DateTime.UtcNow;

            var itemAttributes = new System.Collections.Generic.Dictionary<string, AttributeValue>
            {
                { "ShortCode", new AttributeValue { S = code } },
                { "OriginalUrl", new AttributeValue { S = originalUrl } },
                { "CreatedAt", new AttributeValue { S = now.ToString("o") } }
            };

            var put = new PutItemRequest
            {
                TableName = _tableName,
                Item = itemAttributes,
                ConditionExpression = "attribute_not_exists(ShortCode)"
            };

            try
            {
                await _dynamoDb.PutItemAsync(put);
                return new UrlMapping
                {
                    ShortCode = code,
                    OriginalUrl = originalUrl,
                    CreatedAt = now
                };
            }
            catch (ConditionalCheckFailedException)
            {
                // collision detected - generate a new code and retry
                // Small jitter/backoff could be added here
                await Task.Delay(50 * (attempt + 1));
                continue;
            }
            catch (Exception)
            {
                // Let caller handle/log unexpected exceptions (could wrap or log here)
                throw;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique short code after multiple attempts.");
    }

    public Task<UrlMapping?> GetAsync(string shortCode) =>
        _db.LoadAsync<UrlMapping>(shortCode);
}
