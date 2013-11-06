using System.Linq;
using MongoDB.Driver;

namespace Web.DataAccess
{
    public class MongoDataContext : IDataContext
    {
        readonly MongoDatabase _db;

        public MongoDataContext(MongoDatabase db)
        {
            _db = db;
        }

        public IQueryable<T> Query<T>(string collectionName)
        {
            return _db.GetCollection<T>(collectionName).FindAll().AsQueryable();
        }

        public void Save<T>(string collectionName, T document)
        {
            _db.GetCollection(collectionName).Save<T>(document);
        }

        public void Drop(string collectionName)
        {
            _db.DropCollection(collectionName);
        }
    }
}