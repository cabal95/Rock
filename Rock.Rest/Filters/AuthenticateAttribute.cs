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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
#if !IS_NET_CORE
using System.ServiceModel.Channels;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
#else
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
#endif

using Rock.Model;

namespace Rock.Rest.Filters
{
#if IS_NET_CORE
    public class AuthenticateAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute { }

    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware( RequestDelegate next )
        {
            _next = next;
        }

        public async Task InvokeAsync( HttpContext context )
        {
            if ( !string.IsNullOrEmpty( context.User.Identity.Name ) )
            {
                await _next( context );
                return;
            }

            string authToken = null;

            if ( context.Request.Headers.Keys.Contains( "Authorization-Token" ) )
            {
                authToken = context.Request.Headers["Authorization-Token"];
            }

            if ( string.IsNullOrWhiteSpace( authToken ) )
            {
                authToken = context.Request.Query["apikey"];
            }

            if ( !string.IsNullOrWhiteSpace( authToken ) )
            {
                var userLoginService = new UserLoginService( new Rock.Data.RockContext() );
                var userLogin = userLoginService.Queryable().Where( u => u.ApiKey == authToken ).FirstOrDefault();
                if ( userLogin != null )
                {
                    var claims = new List<Claim>
                        {
                            new Claim( ClaimTypes.Name, userLogin.UserName )
                        };

                    context.User = new ClaimsPrincipal( new ClaimsIdentity( claims, "login" ) );
                }
            }

            await _next( context );
        }
    }
#else
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.Web.Http.Filters.AuthorizationFilterAttribute" />
    public class AuthenticateAttribute : AuthorizationFilterAttribute
    {
        /// <summary>
        /// Calls when a process requests authorization.
        /// </summary>
        /// <param name="actionContext">The action context, which encapsulates information for using <see cref="T:System.Web.Http.Filters.AuthorizationFilterAttribute" />.</param>
        public override void OnAuthorization( HttpActionContext actionContext )
        {
            // See if user is logged in
            var principal = System.Threading.Thread.CurrentPrincipal;
            if ( principal != null && principal.Identity != null && !String.IsNullOrWhiteSpace(principal.Identity.Name))
            {
                //var userLoginService = new UserLoginService();
                //var user = userLoginService.GetByUserName(principal.Identity.Name);
                //if ( user != null )
                //{
                    actionContext.Request.SetUserPrincipal( principal );
                    return;
                //}
            }

            // If not, see if there's a valid token
            string authToken = null;
            if (actionContext.Request.Headers.Contains("Authorization-Token"))
                authToken = actionContext.Request.Headers.GetValues( "Authorization-Token" ).FirstOrDefault();
            if ( String.IsNullOrWhiteSpace( authToken ) )
            {
                string queryString = actionContext.Request.RequestUri.Query;
                authToken = System.Web.HttpUtility.ParseQueryString(queryString).Get("apikey");
            }

            if (! String.IsNullOrWhiteSpace( authToken ) )
            {
                var userLoginService = new UserLoginService( new Rock.Data.RockContext() );
                var userLogin = userLoginService.Queryable().Where( u => u.ApiKey == authToken ).FirstOrDefault();
                if ( userLogin != null )
                {
                    var identity = new GenericIdentity( userLogin.UserName );
                    principal = new GenericPrincipal(identity, null);
                    actionContext.Request.SetUserPrincipal( principal );
                    return;
                }
            }
        }
    }
#endif
}