using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure;

namespace Rock.Tests.Data
{
    public class TestProviderFactory : IDbConnectionFactory
    {
        [ThreadStatic]
        private static DbConnection _connection;

        [ThreadStatic]
        private static Effort.DataLoaders.IDataLoader _loader;

        private readonly static object _lock = new object();

        public static void ResetDb( Effort.DataLoaders.IDataLoader loader )
        {
            lock ( _lock )
            {
                _connection = null;
                _loader = loader;
            }
        }

        public DbConnection CreateConnection( string nameOrConnectionString )
        {
            lock ( _lock )
            {
                if ( _connection == null )
                {
                    _connection = Effort.DbConnectionFactory.CreateTransient( _loader );
                }

                return _connection;
            }
        }
    }
}
