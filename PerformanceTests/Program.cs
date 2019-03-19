using System;
using System.Collections.Generic;
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Model;

namespace PerformanceTests
{

    public class Program
    {
        public static void Main( string[] args )
        {
            int runCount = 1000;


            var sw = System.Diagnostics.Stopwatch.StartNew();
            Warmup();
            Warmup();
            sw.Stop();

            System.Threading.Thread.Sleep( 5000 );

            Console.WriteLine( $"Warmup took { sw.Elapsed }." );

            //
            // Do tests on Lava with Data access.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestLava( runCount );
            sw.Stop();
            Console.WriteLine( $"TestLava: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );

            //
            // Do tests on pure EF access.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestEFQuery( runCount );
            sw.Stop();
            Console.WriteLine( $"TestEFQuery: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );

            //
            // Do tests on pure EF access.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestEFQuerySingleContext( runCount );
            sw.Stop();
            Console.WriteLine( $"TestEFQuerySingleContext: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );

            Console.ReadLine();
        }

        public static void Warmup()
        {
            using ( var rockContext = new RockContext() )
            {
                new PersonService( rockContext ).Queryable().Count();
            }

            DotLiquid.Liquid.UseRubyDateFormat = false;
            DotLiquid.Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            DotLiquid.Template.RegisterSafeType( typeof( Enum ), o => o.ToString() );
            DotLiquid.Template.RegisterSafeType( typeof( DBNull ), o => null );
            DotLiquid.Template.RegisterFilter( typeof( Rock.Lava.RockFilters ) );
        }

        public static void TestLava( int runCount )
        {
            using ( var rockContext = new RockContext() )
            {
                var person = new GroupService( rockContext ).Queryable()
                    .Where( g => g.Id == 2 && g.GroupType.Id == 1 )
                    .SelectMany( g => g.Members )
                    .Select( m => m.Person )
                    .First();

                for ( int i = 0; i < runCount; i++ )
                {
                    string template = @"FullName: {{ CurrentPerson.FullName }}
Campus: {{ CurrentPerson | Campus | Property:'Name' }}
Groups: {{ CurrentPerson | Groups:'11','All','All' | Select:'Group' }}";

                    var mergeFields = new Dictionary<string, object>();
                    var lava = template.ResolveMergeFields( mergeFields, person );

                    if ( i > 0 && i % 100 == 0 )
                    {
                        Console.Write( "." );
                    }
                }
            }

            Console.WriteLine( "" );
        }

        public static void TestEFQuery( int runCount )
        {
            for ( int i = 0; i < runCount; i++ )
            {
                using ( var rockContext = new RockContext() )
                {
                    var person = new GroupService( rockContext ).Queryable()
                        .Where( g => g.Id == 2 && g.GroupType.Id == 1 )
                        .SelectMany( g => g.Members )
                        .Select( m => m.Person )
                        .First();
                }

                if ( i > 0 && i % 100 == 0 )
                {
                    Console.Write( "." );
                    GC.Collect();
                }
            }

            Console.WriteLine( "" );
        }

        public static void TestEFQuerySingleContext( int runCount )
        {
            using ( var rockContext = new RockContext() )
            {
                for ( int i = 0; i < runCount; i++ )
                {
                    var person = new GroupService( rockContext ).Queryable()
                        .Where( g => g.Id == 2 && g.GroupType.Id == 1 )
                        .SelectMany( g => g.Members )
                        .Select( m => m.Person )
                        .First();

                    if ( i > 0 && i % 100 == 0 )
                    {
                        Console.Write( "." );
                        GC.Collect();
                    }
                }
            }

            Console.WriteLine( "" );
        }
    }
}
