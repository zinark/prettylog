using System.Configuration;
using System.Threading;
using MongoDB.Driver;
using PrettyLog.Core.DataAccess;

namespace PrettyLog.Agents
{
    public class MainApplication
    {
        static readonly string Mongoconnection = ConfigurationManager.AppSettings.Get("mongoconnection");
        static readonly string Mongodatabase = ConfigurationManager.AppSettings.Get("mongodatabase");

        static readonly IDataContextFactory ContextFactory = new MongoDataContextFactory(new MongoClient(Mongoconnection), Mongodatabase);

        public static void Main()
        {
            new PrettyAgent(ContextFactory).Start();
        }
    }
}