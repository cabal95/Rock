// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
#if !IS_NET_CORE
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Extensions;
using System.Web.Routing;

#else
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Builder;
#endif
using Rock;

namespace Rock.Rest
{
    /// <summary>
    ///
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Maps ODataService Route and registers routes for any controller actions that use a [Route] attribute
        /// </summary>
        /// <param name="config">The configuration.</param>
#if IS_NET_CORE
        public static void UseRockApi( this IApplicationBuilder app )
#else
        public static void Register( HttpConfiguration config )
#endif
        {
#if !IS_NET_CORE
            config.EnableCors( new Rock.Rest.EnableCorsFromOriginAttribute() );
            config.Filters.Add( new Rock.Rest.Filters.ValidateAttribute() );
            config.Services.Replace( typeof( IExceptionLogger ), new RockApiExceptionLogger() );
            config.Services.Replace( typeof( IExceptionHandler ), new RockApiExceptionHandler() );
            config.Formatters.Insert( 0, new Rock.Utility.RockJsonMediaTypeFormatter() );

            // Change DateTimeZoneHandling to Unspecified instead of the default of RoundTripKind since Rock doesn't store dates in a timezone aware format
            // So, since Rock doesn't do TimeZones, we don't want Transmission of DateTimes to specify TimeZone either.
            config.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Unspecified;

            // register Swagger and its routes first
            Rock.Rest.Swagger.SwaggerConfig.Register( config );
#else
            app.UseMvc( routeBuilder =>
            {
                var config = new
                {
                    Routes = routeBuilder
                };
#endif

            // Add API route for dataviews
            config.Routes.MapHttpRoute(
                name: "DataViewApi",
                routeTemplate: "api/{controller}/DataView/{id}",
                defaults: new
                {
                    action = "DataView"
                } );

            // Add API route for Launching a Workflow
            config.Routes.MapHttpRoute(
                name: "LaunchWorkflowApi",
                routeTemplate: "api/{controller}/LaunchWorkflow/{id}",
                defaults: new
                {
                    action = "LaunchWorkflow"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "POST" } ),
                } );

            // Add API route for DeleteAttributeValue
            config.Routes.MapHttpRoute(
                name: "DeleteAttributeValueApi",
                routeTemplate: "api/{controller}/AttributeValue/{id}",
                defaults: new
                {
                    action = "DeleteAttributeValue"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "DELETE" } ),
                } );

            // Add API route for SetAttributeValue
            config.Routes.MapHttpRoute(
                name: "SetAttributeValueApi",
                routeTemplate: "api/{controller}/AttributeValue/{id}",
                defaults: new
                {
                    action = "SetAttributeValue"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "POST" } ),
                } );

            // Add API route for setting context
            config.Routes.MapHttpRoute(
                name: "SetContextApi",
                routeTemplate: "api/{controller}/SetContext/{id}",
                defaults: new
                {
                    action = "SetContext"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "PUT", "OPTIONS" } ),
                } );

            // Add any custom HTTP API routes. Do this before the attribute route mapping to allow
            // derived classes to override the parent class route attributes.
            foreach ( var type in Reflection.FindTypes( typeof( IHasCustomHttpRoutes ) ) )
            {
                try
                {
                    var controller = Activator.CreateInstance( type.Value ) as IHasCustomHttpRoutes;
                    if ( controller != null )
                    {
                        controller.AddRoutes( config.Routes );
                    }
                }
                catch
                {
                    // ignore, and skip adding routes if the controller raises an exception
                }
            }

#if !IS_NET_CORE
            // finds all [Route] attributes on REST controllers and creates the routes
            config.MapHttpAttributeRoutes();
#endif

            // Add any custom api routes
            foreach ( var type in Rock.Reflection.FindTypes(
                typeof( Rock.Rest.IHasCustomRoutes ) ) )
            {
                try
                {
                    var controller = (Rock.Rest.IHasCustomRoutes)Activator.CreateInstance( type.Value );
                    if ( controller != null )
                    {
#if IS_NET_CORE
                        controller.AddRoutes( routeBuilder );
#else
                        controller.AddRoutes( RouteTable.Routes );
#endif
                    }
                }
                catch
                {
                    // ignore, and skip adding routes if the controller raises an exception
                }
            }

            //// Add Default API Service routes
            //// Instead of being able to use one default route that gets action from http method, have to
            //// have a default route for each method so that other actions do not match the default (i.e. DataViews).
            //// Also, this will make controller routes case-insensitive (vs the odata routing)
            config.Routes.MapHttpRoute(
                name: "DefaultApiGetById",
                routeTemplate: "api/{controller}/{id}",
                defaults: new
                {
                    action = "GetById"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "GET", "OPTIONS" } ),
                    controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
                } );

            config.Routes.MapHttpRoute(
                name: "DefaultApiGetFunction",
                routeTemplate: "api/{controller}({key})",
                defaults: new
                {
                    action = "GET"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "GET", "OPTIONS" } ),
                    controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
                } );

            config.Routes.MapHttpRoute(
                name: "DefaultApiGetList",
                routeTemplate: "api/{controller}",
                defaults: new
                {
                    action = "GET"
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "GET", "OPTIONS" } ),
                    controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
                } );

            config.Routes.MapHttpRoute(
               name: "DefaultApiPut",
               routeTemplate: "api/{controller}/{id}",
               defaults: new
               {
                   action = "PUT",
                   id = System.Web.Http.RouteParameter.Optional
               },
               constraints: new
               {
                   httpMethod = new HttpMethodConstraint( new string[] { "PUT", "OPTIONS" } ),
                   controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
               } );

            config.Routes.MapHttpRoute(
               name: "DefaultApiPatch",
               routeTemplate: "api/{controller}/{id}",
               defaults: new
               {
                   action = "PATCH",
                   id = System.Web.Http.RouteParameter.Optional
               },
               constraints: new
               {
                   httpMethod = new HttpMethodConstraint( new string[] { "PATCH", "OPTIONS" } ),
                   controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
               } );

            config.Routes.MapHttpRoute(
                name: "DefaultApiPost",
                routeTemplate: "api/{controller}/{id}",
                defaults: new
                {
                    action = "POST",
                    id = System.Web.Http.RouteParameter.Optional,
                    controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "POST", "OPTIONS" } )
                } );

            config.Routes.MapHttpRoute(
                name: "DefaultApiDelete",
                routeTemplate: "api/{controller}/{id}",
                defaults: new
                {
                    action = "DELETE",
                    id = System.Web.Http.RouteParameter.Optional
                },
                constraints: new
                {
                    httpMethod = new HttpMethodConstraint( new string[] { "DELETE", "OPTIONS" } ),
                    controllerName = new Rock.Rest.Constraints.ValidControllerNameConstraint()
                } );

#if IS_NET_CORE
            } );
#endif

            // build OData model and create service route (mainly for metadata)
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();

            var entityTypeList = Reflection.FindTypes( typeof( Rock.Data.IEntity ) )
                .Where( a => !a.Value.IsAbstract && ( a.Value.GetCustomAttribute<NotMappedAttribute>() == null ) && ( a.Value.GetCustomAttribute<DataContractAttribute>() != null ) )
                .OrderBy( a => a.Key ).Select( a => a.Value );

            foreach ( var entityType in entityTypeList )
            {
#if IS_NET_CORE
                var entityTypeConfig = builder.AddEntityType( entityType );
#else
                var entityTypeConfig = builder.AddEntity( entityType );
#endif
                
                var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
                string name;
                if ( tableAttribute != null )
                {
                    name = tableAttribute.Name.Pluralize();
                }
                else
                {
                    name = entityType.Name.Pluralize();
                }

#if IS_NET_CORE
                foreach ( var ignoredProperties in entityType.GetCustomAttributes<Rock.Data.IgnorePropertiesAttribute>( true ) )
                {
                    foreach ( var propertyName in ignoredProperties.Properties )
                    {
                        var pi = entityType.GetProperty( propertyName );
                        if ( pi != null )
                        {
                            entityTypeConfig.RemoveProperty( pi );
                        }
                    }
                }
#endif

                var entitySetConfig = builder.AddEntitySet( name, entityTypeConfig );
            }

#if IS_NET_CORE
            app.UseMvc( routeBuilder =>
            {
                routeBuilder.Count().Filter().OrderBy().Expand().Select().MaxTop( null );
                routeBuilder.MapODataServiceRoute( "api", "api", builder.GetEdmModel() );
            } );
#else
            config.Routes.MapODataServiceRoute( "api", "api", builder.GetEdmModel() );
#endif
        }
    }
}