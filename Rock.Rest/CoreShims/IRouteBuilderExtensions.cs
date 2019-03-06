using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using System.Dynamic;

namespace Rock.Rest
{
    public static class IRouteBuilderExtensions
    {
        public static void MapHttpRoute( this IRouteBuilder routeBuilder, string name, string routeTemplate )
        {
            routeBuilder.MapRoute( name, routeTemplate );
        }

        public static void MapHttpRoute( this IRouteBuilder routeBuilder, string name, string routeTemplate, object defaults )
        {
            MapHttpRoute( routeBuilder, name, routeTemplate, defaults, null );
        }

        public static void MapHttpRoute( this IRouteBuilder routeBuilder, string name, string routeTemplate, object defaults, object constraints )
        {
            if ( defaults == null )
            {
                routeBuilder.MapRoute( name, routeTemplate, defaults, constraints );
                return;
            }

            var expando = new ExpandoObject();
            var expandoDict = ( IDictionary<string, object> ) expando;
            string template = routeTemplate;

            foreach ( var pi in defaults.GetType().GetProperties() )
            {
                if ( pi.CanRead )
                {
                    var value = pi.GetValue( defaults ) as string;

                    if ( value == System.Web.Http.RouteParameter.Optional )
                    {
                        template = template.Replace( $"{{{ pi.Name }}}", $"{{{ pi.Name }?}}" );
                    }
                    else
                    {
                        expandoDict.Add( pi.Name, value );
                    }
                }
            }

            routeBuilder.MapRoute( name, template, expando, constraints );
        }
    }
}
