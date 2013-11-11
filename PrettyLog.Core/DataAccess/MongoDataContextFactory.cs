using MongoDB.Driver;

namespace PrettyLog.Core.DataAccess
{
    public class MongoDataContextFactory : IDataContextFactory
    {
        private readonly string _databaseName;
        readonly MongoServer _server;

        public MongoDataContextFactory(MongoClient client, string databaseName)
        {
            _databaseName = databaseName;
            _server = client.GetServer();
            _server.Connect();
        }

        public void Dispose()
        {
            _server.Disconnect();
        }

        public IDataContext Create()
        {
            return new MongoDataContext(_server.GetDatabase(_databaseName));
        }
    }
}