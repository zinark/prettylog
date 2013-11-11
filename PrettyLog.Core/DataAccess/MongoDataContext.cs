using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PrettyLog.Core.DataAccess
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
            return Queryable.AsQueryable<T>(_db.GetCollection<T>(collectionName).FindAll());
        }

        public IQueryable<T> Query<T>(string collectionName, object queryObject)
        {
            return Queryable.AsQueryable<T>(_db.GetCollection<T>(collectionName).FindAs<T>(new QueryDocument(queryObject.ToBsonDocument())));
        }

        public IQueryable<T> Query<T>(string collectionName, string queryJson)
        {
            return Queryable.AsQueryable<T>(_db.GetCollection<T>(collectionName).FindAs<T>(new QueryDocument(BsonDocument.Parse(queryJson))));
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