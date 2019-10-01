using System.IO;

namespace Rock.Migrations.Migrations
{
    public partial class RockMigrationSQL
    {
        private static string GetSqlResource( string path )
        {
            var resources = typeof( RockMigrationSQL ).Assembly.GetManifestResourceNames();
            using ( var sr = new StreamReader( typeof( RockMigrationSQL ).Assembly.GetManifestResourceStream( path ) ) )
            {
                return sr.ReadToEnd();
            }
        }

        private static byte[] GetBinaryResource( string path )
        {
            return typeof( RockMigrationSQL ).Assembly.GetManifestResourceStream( path ).ReadBytesToEnd();
        }
    }
}
