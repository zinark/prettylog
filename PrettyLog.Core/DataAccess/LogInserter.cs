using System;

namespace PrettyLog.Core.DataAccess
{
    public class LogInserter
    {
        private readonly IDataContext _context;

        public LogInserter(IDataContext context)
        {
            _context = context;
        }

        public void Insert(string collection, LogItem item)
        {
            try
            {
                _context.Save(collection, item);
            }
            catch (Exception ex)
            {
#if DEBUG
                throw;
#endif
            }
            
        }
    }
}