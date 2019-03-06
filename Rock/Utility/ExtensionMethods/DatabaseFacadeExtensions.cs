using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;

namespace Rock
{
    public static class DatabaseFacadeExtensions
    {
        public static IEnumerable<T> SqlQuery<T>( this Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade databaseFacade, string query, params object[] args )
        {
            var connection = databaseFacade.GetDbConnection();
            if ( connection.State == System.Data.ConnectionState.Closed )
            {
                connection.Open();
            }

            using ( var command = connection.CreateCommand() )
            {
                command.CommandText = string.Format( query, args );
                using ( var reader = command.ExecuteReader() )
                {
                    while ( reader.Read() )
                    {
                        yield return (T)reader[0];
                    }
                }
            }
        }

        public static int ExecuteSqlCommand( this Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade databaseFacade, string query, params object[] args )
        {
            var connection = databaseFacade.GetDbConnection();
            if ( connection.State == System.Data.ConnectionState.Closed )
            {
                connection.Open();
            }

            using ( var command = connection.CreateCommand() )
            {
                command.CommandText = string.Format( query, args );
                return command.ExecuteNonQuery();
            }
        }
    }
}
