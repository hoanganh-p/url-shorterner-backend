using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDynamoDBContext _db;

        public AuthRepository(IAmazonDynamoDB dynamoDb)
        {
            var config = new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 };
#pragma warning disable CS0618 // Type or member is obsolete
            _db = new DynamoDBContext(dynamoDb, config);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var users = await _db.QueryAsync<User>(email, new DynamoDBOperationConfig { IndexName = "EmailIndex" }).GetRemainingAsync();
#pragma warning restore CS0618 // Type or member is obsolete
            return users.FirstOrDefault();
        }
    }
}
