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
            int runCount = 10000;


            var sw = System.Diagnostics.Stopwatch.StartNew();
            Warmup();
            sw.Stop();

            Console.WriteLine( $"Warmup took { sw.Elapsed }." );

            sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < runCount; i++)
            {
                TestLava();
            }
            sw.Stop();

            var runDuration = sw.Elapsed;
            var durationPerRun = sw.Elapsed.TotalMilliseconds / runCount;

            Console.WriteLine( $"Each run took { durationPerRun } milliseconds." );
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

        public static void TestLava()
        {
            string template = @"FullName: {{ CurrentPerson.FullName }}
Campus: {{ CurrentPerson | Campus | Property:'Name' }}
Groups: {{ CurrentPerson | Groups:'11','All','All' | Select:'Group' }}";

            var mergeFields = new Dictionary<string, object>();
            using ( var rockContext = new RockContext() )
            {
                var person = new GroupService( rockContext ).Queryable()
                    .Where( g => g.Id == 2 && g.GroupType.Id == 1 )
                    .SelectMany( g => g.Members )
                    .Select( m => m.Person )
                    .First();
                var lava = template.ResolveMergeFields( mergeFields, person );
            }
        }
    }
}
