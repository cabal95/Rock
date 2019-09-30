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
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web.Http;

#if IS_NET_CORE
using Microsoft.AspNetCore.Authentication;
#endif
using Microsoft.IdentityModel.Tokens;
using Rock.Data;
using Rock.Model;
using Rock.Security;

namespace Rock.Rest.Controllers
{
    /// <summary>
    /// 
    /// </summary>
#if IS_NET_CORE
    public class AuthController : Microsoft.AspNetCore.Mvc.ControllerBase
#else
    public class AuthController : ApiController
#endif
    {
        /// <summary>
        /// Use this to Login a user and return an AuthCookie which can be used in subsequent REST calls
        /// </summary>
        /// <param name="loginParameters">The login parameters.</param>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        [HttpPost]
        [System.Web.Http.Route( "api/Auth/Login" )]
        public void Login( [FromBody]LoginParameters loginParameters )
        {
            if ( !IsLoginValid( loginParameters ) )
            {
                throw new HttpResponseException( HttpStatusCode.Unauthorized );
            }

#if IS_NET_CORE
            // EFTODO: Move this into the SetAuthCookie method.

            var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim( System.Security.Claims.ClaimTypes.Name, loginParameters.Username )
            };
            var userIdentity = new System.Security.Claims.ClaimsIdentity( claims, "login" );
            var principal = new System.Security.Claims.ClaimsPrincipal( userIdentity );

            HttpContext.SignInAsync( principal );
#else
            Rock.Security.Authorization.SetAuthCookie( loginParameters.Username, loginParameters.Persisted, false );
#endif
        }

        /// <summary>
        /// Check if the login parameters are valid
        /// </summary>
        /// <param name="loginParameters"></param>
        /// <returns></returns>
        private bool IsLoginValid( LoginParameters loginParameters )
        {
            if ( loginParameters == null || loginParameters.Username.IsNullOrWhiteSpace() )
            {
                return false;
            }

            var rockContext = new RockContext();
            var userLoginService = new UserLoginService( rockContext );
            var userLogin = userLoginService.GetByUserName( loginParameters.Username );

            if ( userLogin == null || userLogin.EntityType == null )
            {
                return false;
            }

            var component = AuthenticationContainer.GetComponent( userLogin.EntityType.Name );

            if ( component == null || !component.IsActive )
            {
                return false;
            }

            return component.Authenticate( userLogin, loginParameters.Password );
        }
    }
}
