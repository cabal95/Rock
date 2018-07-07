using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Effort.DataLoaders;

namespace Rock.Tests.Data
{
    public class ZipFileReference : IFileReference
    {
        private readonly ZipArchiveEntry Entry;

        public ZipFileReference( ZipArchiveEntry entry )
        {
            Entry = entry;
        }

        public ZipFileReference( string path, string filename )
        {
            var archive = new ZipArchive( System.IO.File.OpenRead( path ) );
            Entry = archive.GetEntry( filename );
        }

        public bool Exists => Entry != null;

        public Stream Open()
        {
            return Entry?.Open();
        }
    }
}
