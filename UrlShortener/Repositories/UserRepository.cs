using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using UrlShortener.Models;

namespace UrlShortener.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDynamoDBContext _db;

        public UserRepository(IAmazonDynamoDB dynamoDb)
        {
            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
#pragma warning disable CS0618 // Type or member is obsolete
            _db = new DynamoDBContext(dynamoDb, config);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var search = _db.ScanAsync<User>(new[]
            {
                new ScanCondition("Username", ScanOperator.Equal, username)
            });

            var users = await search.GetNextSetAsync();
            return users.FirstOrDefault();
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            return await _db.LoadAsync<User>(userId);
        }

        public Task SaveAsync(User user)
        {
            return _db.SaveAsync(user);
        }
    }
}
