using System;

namespace PrettyLog.Core.DataAccess
{
    public interface IDataContextFactory : IDisposable
    {
        IDataContext Create();
    }
}