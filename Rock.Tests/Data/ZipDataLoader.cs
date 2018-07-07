using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Effort.DataLoaders;

namespace Rock.Tests.Data
{
    public class ZipDataLoader : IDataLoader
    {
        private string Path;

        public ZipDataLoader()
        {
        }

        public ZipDataLoader( string path )
        {
            Path = path;
        }

        string IDataLoader.Argument
        {
            get
            {
                return Path;
            }
            set
            {
                Path = value;
            }
        }

        public ITableDataLoaderFactory CreateTableDataLoaderFactory()
        {
            return new ZipTableDataLoaderFactory( Path );
        }
    }
}
