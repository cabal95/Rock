﻿using System.IO;

namespace Rock.Migrations.Migrations
{
    public partial class RockMigrationSQL
    {
        private static string GetRealPath( string path )
        {
            string basePath = Path.GetDirectoryName( typeof( RockMigrationSQL ).Assembly.Location );

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
