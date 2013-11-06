using System;

namespace Web.DataAccess
{
    public interface IDataContextFactory : IDisposable
    {
        IDataContext Create();
    }
}