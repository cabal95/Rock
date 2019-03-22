using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;

#if IS_NET_CORE
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
#endif

using Rock;
using Rock.Data;
using Rock.Model;

namespace PerformanceTests
{

    public class Program
    {
        #region Lava

        private static string _pageNavLava = @"{%- if Page.DisplayChildPages == 'true' and Page.Pages != empty -%}
    <ul class='nav nav-stacked navbar-side'>
        <li class='navbar-logo'>
        {%- if CurrentPage.Layout.Site.SiteLogoBinaryFileId != null -%}
            <a href='{{ '~' | ResolveRockUrl }}' title='Rock RMS' class='navbar-brand-side has-logo'>
                <img src='{{ '~' | ResolveRockUrl }}GetImage.ashx?id={{ CurrentPage.Layout.Site.SiteLogoBinaryFileId }}&w=48&h=48' alt='{{ 'Global' | Attribute:'OrganizationName' }}' class='logo'>
            </a>
        {%- else -%}
            <a href='{{ '~' | ResolveRockUrl }}' title='Rock RMS' class='navbar-brand-side no-logo'></a>
        {%- endif -%}
        </li>
		{%- for childPage in Page.Pages -%}
            {%- if childPage.IsParentOfCurrent == 'true' -%}
				<li class='current {% if childPage.DisplayChildPages == 'true' and childPage.Pages != empty %}has-children{% endif %}'>
			{%- else -%}
				<li {% if childPage.DisplayChildPages == 'true' and childPage.Pages != empty %}class='has-children'{% endif %}>
			{%- endif -%}
				<i class='{{ childPage.IconCssClass }}'></i>

				{%- if childPage.DisplayChildPages == 'true' and childPage.Pages != empty -%}
                    <ul class='nav nav-childpages'>
                        <li class='title'>{{ childPage.Title }}</li>
						{%- for grandchildPage in childPage.Pages -%}
                            <li class='header'>{{ grandchildPage.Title }}</li>
                            {%- if grandchildPage.DisplayChildPages == 'true' -%}
                                {%- for greatgrandchildPage in grandchildPage.Pages -%}
                                    {%- if greatgrandchildPage.IsParentOfCurrent == 'true' or greatgrandchildPage.Current == 'true' -%}
                                        <li class='current'>
                                    {%- else -%}
                                        <li>
                                    {%- endif -%}
                                        <a role='menu-item' href='{{ greatgrandchildPage.Url }}'>{{ greatgrandchildPage.Title }}</a>
                                    </li>
                                {%- endfor -%}
                            {%- endif -%}
                        {%- endfor -%}
                    </ul>
                {%- endif -%}
            </li>
        {%- endfor -%}
    </ul>
{%- endif -%}";

        #endregion

        public static void Main( string[] args )
        {
            int runCount = 10000;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Warmup();
            sw.Stop();

            Thread.Sleep( 5000 );

            Console.WriteLine( $"Warmup took { sw.Elapsed }." );

            //
            // Do tests on context creation speed.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestContextCreation( runCount );
            sw.Stop();
            Console.WriteLine( $"TestContextCreation: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );

            //
            // Do tests on context connection speed.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestContextConnection( runCount );
            sw.Stop();
            Console.WriteLine( $"TestContextConnection: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );

#if IS_NET_CORE
            //
            // Do tests on Fluid rendering.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestFluidPageNav( runCount );
            sw.Stop();
            Console.WriteLine( $"TestFluidPageNav: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );

            //
            // Do tests on Fluid rendering.
            //
            sw = System.Diagnostics.Stopwatch.StartNew();
            TestDotLiquidPageNav( runCount );
            sw.Stop();
            Console.WriteLine( $"TestDotLiquidPageNav: Each run took {sw.Elapsed.TotalMilliseconds / runCount} milliseconds." );
#endif

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

#if !IS_NET_CORE
            DotLiquid.Template.RegisterFilter( typeof( Rock.Lava.RockFilters ) );
#endif
        }

        public static void TestContextCreation( int runCount )
        {
            for ( int i = 0; i < runCount; i++ )
            {
                using ( var rockContext = new RockContext() )
                {
                }

                if ( i > 0 && i % 100 == 0 )
                {
                    Console.Write( "." );
                }
            }

            Console.WriteLine();
        }

        public static void TestContextConnection( int runCount )
        {
            for ( int i = 0; i < runCount; i++ )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.ExecuteSqlCommand( "SELECT 1" );
                }

                if ( i > 0 && i % 100 == 0 )
                {
                    Console.Write( "." );
                }
            }

            Console.WriteLine();
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

                    var mergeFields = new Dictionary<string, object>()
                    {
                        { "CurrentPerson", person }
                    };

                    var dotTemplate = DotLiquid.Template.Parse( template );
                    var parameters = new DotLiquid.RenderParameters
                    {
                        LocalVariables = DotLiquid.Hash.FromDictionary( mergeFields ),
                        Filters = new[] { typeof( Rock.Lava.RockFilters ) }
                    };

                    var lava = dotTemplate.Render( parameters );

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

#if IS_NET_CORE
        public static void TestDotLiquidPageNav( int runCount )
        {
            using ( var rockContext = new RockContext() )
            {
                var person = new PersonService( rockContext ).Get( 1 );
                var currentPage = Rock.Web.Cache.PageCache.Get( 12 );
                var rootPage = currentPage;
                NameValueCollection queryString = null;
                Dictionary<string, string> pageParameters = null;
                var pageHeirarchy = currentPage.GetPageHierarchy().Select( p => p.Id ).ToList();

                var httpContext = new DefaultHttpContext();
                var httpContextAccessor = new HttpContextAccessor();
                httpContextAccessor.HttpContext = httpContext;
                System.Web.HttpContext.Configure( httpContextAccessor );

                var mergeFields = new Dictionary<string, object>
                {
                    { "CurrentPerson", person },
                    { "CurrentPage", currentPage },
                    { "Page", rootPage.GetMenuProperties( 3, person, rockContext, pageHeirarchy, pageParameters, queryString ) }
                };

                for ( int i = 0; i < runCount; i++ )
                {
                    var template = DotLiquid.Template.Parse( _pageNavLava );
                    var parameters = new DotLiquid.RenderParameters
                    {
                        LocalVariables = DotLiquid.Hash.FromDictionary( mergeFields ),
                        Filters = new[] { typeof( DotLiquidLavaFilters ) }
                    };

                    var lava = template.Render( parameters );

                    //var hash = Rock.Security.Encryption.GetSHA1Hash( lava );

                    if ( i > 0 && i % 100 == 0 )
                    {
                        Console.Write( "." );
                    }
                }
            }

            Console.WriteLine( "" );
        }

        public static void TestFluidPageNav( int runCount )
        {
            using ( var rockContext = new RockContext() )
            {
                var person = new PersonService( rockContext ).Get( 1 );
                var currentPage = Rock.Web.Cache.PageCache.Get( 12 );
                var rootPage = currentPage;
                NameValueCollection queryString = null;
                Dictionary<string, string> pageParameters = null;
                var pageHeirarchy = currentPage.GetPageHierarchy().Select( p => p.Id ).ToList();

                var httpContext = new DefaultHttpContext();
                var httpContextAccessor = new HttpContextAccessor();
                httpContextAccessor.HttpContext = httpContext;
                System.Web.HttpContext.Configure( httpContextAccessor );

                var mergeFields = new Dictionary<string, object>
                {
                    { "CurrentPerson", person },
                    { "CurrentPage", currentPage },
                    { "Page", rootPage.GetMenuProperties( 3, person, rockContext, pageHeirarchy, pageParameters, queryString ) }
                };

                for ( int i = 0; i < runCount; i++ )
                {
                    var template = Fluid.FluidTemplate.Parse( _pageNavLava );
                    var context = new Fluid.TemplateContext();
                    context.Filters.AddFilter( "ResolveRockUrl", FluidLavaFilters.ResolveRockUrl );

                    foreach ( var f in mergeFields )
                    {
                        context.SetValue( f.Key, f.Value );
                    }

                    var lava = Fluid.FluidTemplateExtensions.Render( template, context );

                    //var hash = Rock.Security.Encryption.GetSHA1Hash( lava );

                    if ( i > 0 && i % 100 == 0 )
                    {
                        Console.Write( "." );
                    }
                }

                Console.WriteLine( "" );
            }
        }
#endif
    }

#if IS_NET_CORE
    public static class DotLiquidLavaFilters
    {
        /// <summary>
        /// Resolves the rock address.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public static string ResolveRockUrl( string input )
        {
            if ( input.StartsWith( "~~" ) )
            {
                string theme = "Rock";

                input = "~/Themes/" + theme + ( input.Length > 2 ? input.Substring( 2 ) : string.Empty );
            }
            else if ( input == "~" )
            {
                input = "/";
            }
            else if ( input.StartsWith( "~" ) )
            {
                input = input.Substring( 1 );
            }

            return input;
        }
    }

    public static class FluidLavaFilters
    {
        public static Fluid.Values.FluidValue ResolveRockUrl( Fluid.Values.FluidValue input, Fluid.FilterArguments arguments, Fluid.TemplateContext context )
        {
            var url = input.ToStringValue();

            if ( url.StartsWith( "~~" ) )
            {
                string theme = "Rock";

                url = "~/Themes/" + theme + ( url.Length > 2 ? url.Substring( 2 ) : string.Empty );
            }
            else if ( url == "~" )
            {
                url = "/";
            }
            else if ( url.StartsWith( "~" ) )
            {
                url = url.Substring( 1 );
            }

            return Fluid.Values.FluidValue.Create( url );
        }
    }
#endif
}
