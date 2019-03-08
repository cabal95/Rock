using System.IO;

namespace Rock.Migrations.Migrations
{
    public partial class RockMigrationSQL
    {
        private static string GetRealPath( string path )
        {
            string basePath = Path.GetDirectoryName( typeof( RockMigrationSQL ).Assembly.Location );
            return Path.Combine( basePath, path );
        }

        private static string GetSqlResource( string path )
        {
            return File.ReadAllText( GetRealPath( path ) );
        }

        private static byte[] GetBinaryResource( string path )
        {
            return File.ReadAllBytes( GetRealPath( path ) );
        }
    }
}
