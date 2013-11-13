using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace PrettyLog.Core.DataAccess
{
    public interface IDataContext
    {
        IQueryable<T> Query<T>(string collectionName);
        IQueryable<T> Query<T>(string collectionName, object queryObject);
        IQueryable<T> Query<T>(string collectionName, string queryJson);
        IEnumerable<BsonDocument> Aggregate(string collectionName, params BsonDocument[] docs);
        void Save<T>(string collectionName, T document);
        void Drop(string collectionName);
    }
}