using System.Linq;

namespace Web.DataAccess
{
    public interface IDataContext
    {
        IQueryable<T> Query<T>(string collectionName);
        void Save<T>(string collectionName, T document);
        void Drop(string collectionName);
    }
}