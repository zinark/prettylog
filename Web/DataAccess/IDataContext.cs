using System.Linq;

namespace Web.DataAccess
{
    public interface IDataContext
    {
        IQueryable<T> Query<T>(string collectionName);
        IQueryable<T> Query<T>(string collectionName, object queryObject);
        IQueryable<T> Query<T>(string collectionName, string queryJson);
        void Save<T>(string collectionName, T document);
        void Drop(string collectionName);
    }
}