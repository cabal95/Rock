using System;
using System.IO;
using System.IO.Compression;

using Effort.DataLoaders;

namespace Rock.Tests.Data
{
    public class ZipTableDataLoaderFactory : ITableDataLoaderFactory
    {
        private ZipArchive Archive;

        public ZipTableDataLoaderFactory( string path )
        {
            if ( !File.Exists(path) )
            {
                throw new ArgumentException( string.Format( "Path '{0}' does not exist.", path ), "path" );
            }

            Archive = new ZipArchive( File.OpenRead( path ) );
        }

        public void Dispose()
        {
            Archive.Dispose();
            Archive = null;
        }

        public ITableDataLoader CreateTableDataLoader( TableDescription table )
        {
            var entry = Archive.GetEntry( table.Name + ".csv" );

            if ( entry != null )
            {
                return new CsvTableDataLoader( new ZipFileReference( entry ), table );
            }

            return new EmptyTableDataLoader();
        }
    }
}
