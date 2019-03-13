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
using System.Linq;
using System.Net;
using System.Net.Http;
#if !IS_NET_CORE
using System.ServiceModel.Channels;
#endif
using System.Threading.Tasks;
#if !IS_NET_CORE
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
#endif

#if IS_NET_CORE
using Microsoft.AspNetCore.Mvc.Filters;
#endif
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace Rock.Rest.Filters
{
#if IS_NET_CORE
    public class SecuredAttribute : System.Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync( ActionExecutingContext actionContext, ActionExecutionDelegate next )
        {
            var controller = actionContext.RouteData.Values["controller"].ToString();
            string controllerClassName = actionContext.Controller.GetType().FullName;
            string actionMethod = actionContext.RouteData.Values["action"].ToString().ToUpper();
            var endpoint = actionContext.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IEndpointFeature>();
            string actionPath;
            if ( endpoint != null )
            {
                actionPath = ( ( Microsoft.AspNetCore.Routing.RouteEndpoint ) endpoint?.Endpoint )?.RoutePattern?.RawText ?? string.Empty;
            }
            else if ( actionContext.RouteData.Routers.Any( r => r.GetType() == typeof( Microsoft.AspNetCore.Routing.Route ) ) )
            {
                var route = ( Microsoft.AspNetCore.Routing.Route ) actionContext.RouteData.Routers.Single( r => r.GetType() == typeof( Microsoft.AspNetCore.Routing.Route ) );
                actionPath = route.RouteTemplate.Replace( "{controller}", controller );
            }
            else
            {
                throw new Exception( "Unknown route encountred" );
            }

            //// find any additional arguments that aren't part of the RouteTemplate that qualified the action method
            //// for example: ~/person/search?name={name}&includeHtml={includeHtml}&includeDetails={includeDetails}&includeBusinesses={includeBusinesses}
            //// is a different action method than ~/person/search?name={name}.
            //// Also exclude any ODataQueryOptions parameters (those don't end up as put of the apiId)
            var routeQueryParams = actionContext.ActionArguments.Where( a => !actionPath.Contains( "{" + a.Key + "}" ) && !( a.Value is Microsoft.AspNet.OData.Query.ODataQueryOptions ) );
#else
    /// <summary>
    /// Checks to see if the Logged-In person has authorization View (HttpMethod: GET) or Edit (all other HttpMethods) for the RestController and Controller's associated EntityType
    /// </summary>
    public class SecuredAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Occurs before the action method is invoked.
        /// </summary>
        /// <param name="actionContext">The action context.</param>
        public override void OnActionExecuting( HttpActionContext actionContext )
        {
            var controller = actionContext.ActionDescriptor.ControllerDescriptor;
            string controllerClassName = controller.ControllerType.FullName;
            string actionMethod = actionContext.Request.Method.Method;
            string actionPath = actionContext.Request.GetRouteData().Route.RouteTemplate.Replace( "{controller}", controller.ControllerName );

            //// find any additional arguments that aren't part of the RouteTemplate that qualified the action method
            //// for example: ~/person/search?name={name}&includeHtml={includeHtml}&includeDetails={includeDetails}&includeBusinesses={includeBusinesses}
            //// is a different action method than ~/person/search?name={name}.
            //// Also exclude any ODataQueryOptions parameters (those don't end up as put of the apiId)
            var routeQueryParams = actionContext.ActionArguments.Where(a => !actionPath.Contains("{" + a.Key + "}") && !(a.Value is System.Web.Http.OData.Query.ODataQueryOptions) );
#endif
            if ( routeQueryParams.Any())
            {
                var actionPathQueryString = routeQueryParams.Select( a => string.Format( "{0}={{{0}}}", a.Key ) ).ToList().AsDelimited( "&" );
                actionPath += "?" + actionPathQueryString;
            }

            ISecured item = RestActionCache.Get( actionMethod + actionPath );
            if ( item == null )
            {
                // if there isn't a RestAction in the database, use the Controller as the secured item
                item = RestControllerCache.Get( controllerClassName );
                if ( item == null )
                {
                    item = new RestController();
                }
            }

            Person person = null;

#if IS_NET_CORE
            if ( actionContext.HttpContext.Items.ContainsKey( "Person" ) )
            {
                person = actionContext.HttpContext.Items["Person"] as Person;
            }
#else
            if ( actionContext.Request.Properties.Keys.Contains( "Person" ) )
            {
                person = actionContext.Request.Properties["Person"] as Person;
            }
#endif
            else
            {
#if IS_NET_CORE
                var principal = actionContext.HttpContext.User;
                if ( principal != null && principal.Identity != null && !string.IsNullOrEmpty( principal.Identity.Name ) )
#else
                var principal = actionContext.Request.GetUserPrincipal();
                if ( principal != null && principal.Identity != null )
#endif
                {
                    using ( var rockContext = new RockContext() )
                    {
                        string userName = principal.Identity.Name;
                        UserLogin userLogin = null;
                        if ( userName.StartsWith( "rckipid=" ) )
                        {
                            Rock.Model.PersonService personService = new Model.PersonService( rockContext );
                            Rock.Model.Person impersonatedPerson = personService.GetByImpersonationToken( userName.Substring( 8 ) );
                            if ( impersonatedPerson != null )
                            {
                                userLogin = impersonatedPerson.GetImpersonatedUser();
                            }
                        }
                        else
                        {
                            var userLoginService = new Rock.Model.UserLoginService( rockContext );
                            userLogin = userLoginService.GetByUserName( userName );
                        }

                        if ( userLogin != null )
                        {
                            person = userLogin.Person;
#if IS_NET_CORE
                            actionContext.HttpContext.Items.Add( "Person", person );
#else
                            actionContext.Request.Properties.Add( "Person", person );
#endif
                        }
                    }
                }
            }

            string action = actionMethod.Equals( "GET", StringComparison.OrdinalIgnoreCase ) ?
                Rock.Security.Authorization.VIEW : Rock.Security.Authorization.EDIT;
            if ( !item.IsAuthorized( action, person ) )
            {
#if IS_NET_CORE
                actionContext.Result = new Microsoft.AspNetCore.Mvc.ChallengeResult();
            }
            else
            {
                await next();
#else
                actionContext.Response = new HttpResponseMessage( HttpStatusCode.Unauthorized );
#endif
            }
        }
    }
}