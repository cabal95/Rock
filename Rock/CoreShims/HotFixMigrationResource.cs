using System.IO;

namespace Rock.Plugin.HotFixes
{
    public partial class HotFixMigrationResource
    {
        private static string GetRealPath( string path )
        {
            string basePath = Path.GetDirectoryName( typeof( HotFixMigrationResource ).Assembly.Location );

            var segments = path.Split( '\\' );
            var realPath = basePath;

            foreach ( var segment in segments )
            {
                realPath = Path.Combine( realPath, segment );
            }

            return realPath;
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
