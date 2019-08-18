using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;

namespace OData.Mongo.Repositories
{
    public class ItemsRepository : IItemsRepository
    {
        private IMongoDatabase _database;

        public ItemsRepository(IMongoClient mongoClient, IConfiguration configuration)
        {
            _database = mongoClient.GetDatabase(configuration["DatabaseName"]);
        }

        public IFindFluent<Dictionary<string, object>, Dictionary<string, object>> Get(string collectionName)
        {
            var collection = _database.GetCollection<Dictionary<string, object>>(collectionName);
            return collection.Find(_ => true);
        }
    }

    public interface IItemsRepository
    {
        IFindFluent<Dictionary<string, object>, Dictionary<string, object>> Get(string collectionName);
    }
}
