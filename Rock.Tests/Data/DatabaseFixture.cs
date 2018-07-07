using System.Linq;

using Effort.DataLoaders;

using Rock.Data;

namespace Rock.Tests.Data
{
    public class DatabaseFixture
    {
        static DatabaseFixture()
        {
            Effort.Provider.EffortProviderConfiguration.RegisterProvider();
        }

        public DatabaseFixture()
        {
            var source = this.GetType().GetCustomAttributes( typeof( LoadDataAttribute ), true ).Cast<LoadDataAttribute>().FirstOrDefault();

            if ( source != null )
            {
                IDataLoader loader = new ZipDataLoader( source.Source );
                loader = new CachingDataLoader( loader );
                TestProviderFactory.ResetDb( loader );
            }
            else
            {
                TestProviderFactory.ResetDb( null );
            }

            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.CreateIfNotExists();
            }
        }
    }
}
