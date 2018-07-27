using System;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Rock.Cache;

namespace Rock.Tests.Data
{
    public class DatabaseFixture : IDisposable
    {
        static DatabaseFixture()
        {
        }

        public DatabaseFixture()
        {
            var testSource = ConfigurationManager.AppSettings["RockUnitTestSource"];

            ResetDatabase( testSource );
        }

        public void Dispose()
        {
            DeleteDatabase();
        }

        private static string GetDataPath()
        {
            string path = Path.Combine( Directory.GetCurrentDirectory(), "Data" );

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory( path );
            }

            return path;
        }

        protected virtual void ResetDatabase( string archivePath )
        {
            var cs = ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString;
            var csb = new SqlConnectionStringBuilder( cs );
            var dbName = csb.InitialCatalog;
            csb.InitialCatalog = "master";

            using ( var archive = new ZipArchive( File.Open( archivePath, FileMode.Open ) ) )
            {
                using ( var connection = new SqlConnection( csb.ConnectionString ) )
                {
                    connection.Open();
                    CreateDatabase( connection, dbName, archive );
                }
            }

            RockCache.ClearAllCachedItems();
        }

        protected virtual void DeleteDatabase()
        {
            var cs = ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString;
            var csb = new SqlConnectionStringBuilder( cs );
            var dbName = csb.InitialCatalog;
            csb.InitialCatalog = "master";

            using ( var connection = new SqlConnection( csb.ConnectionString ) )
            {
                connection.Open();

                if ( dbName != "master" )
                {
                    DeleteDatabase( connection, dbName );
                }
            }
        }

        private static void CreateDatabase( DbConnection connection, string dbName, ZipArchive archive )
        {
            var mdf = archive.Entries.Where( e => e.Name.EndsWith( ".mdf" ) ).First();
            var ldf = archive.Entries.Where( e => e.Name.EndsWith( ".ldf" ) ).First();

            using ( var writer = File.Create( Path.Combine( GetDataPath(), string.Format( "{0}_Data.mdf", dbName ) ) ) )
            {
                using ( var reader = mdf.Open() )
                {
                    reader.CopyTo( writer );
                }
            }

            using ( var writer = File.Create( Path.Combine( GetDataPath(), string.Format( "{0}_Log.ldf", dbName ) ) ) )
            {
                using ( var reader = ldf.Open() )
                {
                    reader.CopyTo( writer );
                }
            }

            string sql = string.Format( @"CREATE DATABASE [{0}]   
    ON (FILENAME = '{1}\{0}_Data.mdf'),  
    (FILENAME = '{1}\{0}_Log.ldf')  
    FOR ATTACH;", dbName, GetDataPath() );

            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private void DeleteDatabase( DbConnection connection, string name )
        {
            string sql = string.Format( @"USE master;
ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [{0}] ;", name );

            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            File.Delete( Path.Combine( GetDataPath(), string.Format( "{0}_Data.mdf", name ) ) );
            File.Delete( Path.Combine( GetDataPath(), string.Format( "{0}_Log.mdf", name ) ) );
        }
    }
}
