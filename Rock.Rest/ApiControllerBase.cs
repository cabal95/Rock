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
#if !IS_NET_CORE
using System.ServiceModel.Channels;
using System.Web.Http;
using System.Web.Http.OData;
#endif

#if IS_NET_CORE
using Microsoft.AspNet.OData;
using Microsoft.AspNetCore.Mvc;
#endif
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Security;

namespace Rock.Rest
{
    /*
     * NOTE: We could have inherited from System.Web.Http.OData.ODataController, but that changes 
     * the response format from vanilla REST to OData format. That breaks existing Rock Rest clients.
     * 
     */

    /// <summary>
    /// Base ApiController for Rock REST endpoints
    /// Supports ODataV3 Queries and ODataRouting
    /// </summary>
    /// <seealso cref="System.Web.Http.ApiController" />
    [ODataRouting]
#if IS_NET_CORE
    public class ApiControllerBase : ControllerBase
#else
    public class ApiControllerBase : ApiController
#endif
    {
        /// <summary>
        /// Gets the currently logged in Person
        /// </summary>
        /// <returns></returns>
        protected virtual Rock.Model.Person GetPerson()
        {
            return GetPerson( null );
        }

        /// <summary>
        /// Gets the currently logged in Person
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        protected virtual Rock.Model.Person GetPerson( RockContext rockContext )
        {
#if IS_NET_CORE
            if ( HttpContext.Items.ContainsKey( "Person" ) )
            {
                return HttpContext.Items["Person"] as Person;
            }
#else
            if ( Request.Properties.Keys.Contains( "Person" ) )
            {
                return Request.Properties["Person"] as Person;
            }
#endif

#if IS_NET_CORE
            var principal = User;
#else
            var principal = ControllerContext.Request.GetUserPrincipal();
#endif
            if ( principal != null && principal.Identity != null )
            {
                if ( principal.Identity.Name.StartsWith( "rckipid=" ) )
                {
                    var personService = new Model.PersonService( rockContext ?? new RockContext() );
                    Rock.Model.Person impersonatedPerson = personService.GetByImpersonationToken( principal.Identity.Name.Substring( 8 ), false, null );
                    if ( impersonatedPerson != null )
                    {
                        return impersonatedPerson;
                    }
                }
                else
                {
                    var userLoginService = new Rock.Model.UserLoginService( rockContext ?? new RockContext() );
                    var userLogin = userLoginService.GetByUserName( principal.Identity.Name );

                    if ( userLogin != null )
                    {
                        var person = userLogin.Person;
#if IS_NET_CORE
                        HttpContext.Items.Add( "Person", person );
#else
                        Request.Properties.Add( "Person", person );
#endif
                        return userLogin.Person;
                    }
                }
            }

            return null;
        }

#if IS_NET_CORE
        /// <summary>
        /// Ensures that the HttpContext has a CurrentPerson value.
        /// </summary>
        protected virtual void EnsureHttpContextHasCurrentPerson()
        {
            if ( !HttpContext.Items.ContainsKey( "CurrentPerson" ) )
            {
                HttpContext.Items.Add( "CurrentPerson", GetPerson() );
            }
        }
#endif

        /// <summary>
        /// Gets the primary person alias of the currently logged in person
        /// </summary>
        /// <returns></returns>
        protected virtual Rock.Model.PersonAlias GetPersonAlias()
        {
            return GetPersonAlias( null );
        }

        /// <summary>
        /// Gets the primary person alias of the currently logged in person
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        protected virtual Rock.Model.PersonAlias GetPersonAlias( RockContext rockContext )
        {
            var person = GetPerson( rockContext );
            if ( person != null )
            {
                return person.PrimaryAlias;
            }

            return null;
        }

        /// <summary>
        /// Gets the primary person alias ID of the currently logged in person
        /// </summary>
        /// <returns></returns>
        protected virtual int? GetPersonAliasId()
        {
            return GetPersonAliasId( null );
        }

        /// <summary>
        /// Gets the primary person alias ID of the currently logged in person
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        protected virtual int? GetPersonAliasId( RockContext rockContext )
        {
            var currentPersonAlias = GetPersonAlias( rockContext );
            return currentPersonAlias == null ? ( int? ) null : currentPersonAlias.Id;
        }
    }
}
