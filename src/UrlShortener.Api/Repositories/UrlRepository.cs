using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories
{
    public class UrlRepository : IUrlRepository
    {
        private readonly IDynamoDBContext _db;

        public UrlRepository(IAmazonDynamoDB dynamoDb)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
            _db = new DynamoDBContext(dynamoDb, config);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public async Task<Url?> GetByShortCodeAsync(string shortCode)
        {
            return await _db.LoadAsync<Url>(shortCode);
        }

        public Task AddAsync(Url url)
        {
            return _db.SaveAsync(url);
        }

        public async Task<IEnumerable<Url>> GetAllForUserAsync(string userId)
        {
            var conditions = new List<ScanCondition>
            {
                new ScanCondition("UserId", ScanOperator.Equal, userId)
            };

            var search = _db.ScanAsync<Url>(conditions);
            var results = await search.GetRemainingAsync();
            return results.OrderByDescending(x => x.CreatedAt);
        }

        public Task UpdateAsync(Url url)
        {
            return _db.SaveAsync(url);
        }

        public Task DeleteAsync(Url url)
        {
            return _db.DeleteAsync(url);
        }
    }
}
