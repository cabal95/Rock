﻿// <copyright>
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
using System.Collections.Generic;
#if !IS_NET_CORE
using System.Web.Http.Routing;
#else
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
#endif

namespace Rock.Rest.Constraints
{
    /// <summary>
    /// 
    /// </summary>
#if IS_NET_CORE
    public class ValidControllerNameConstraint : IRouteConstraint
#else
    public class ValidControllerNameConstraint : IHttpRouteConstraint
#endif
    {
        /// <summary>
        /// Determines whether this instance equals a specified route.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="route">The route to compare.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="values">A list of parameter values.</param>
        /// <param name="routeDirection">The route direction.</param>
        /// <returns>
        /// True if this instance equals a specified route; otherwise, false.
        /// </returns>
#if IS_NET_CORE
        public bool Match( HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection )
#else
        public bool Match( System.Net.Http.HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection )
#endif
        {
            if (values.ContainsKey("controller"))
            {
                string controllerName = values["controller"] as string;
                if (controllerName.Length > 0)
                {
                    // make sure the Controller parameter starts with an Alpha character (mainly so that api/$metadata will routed correctly)
                    return char.IsLetter( controllerName[0] );
                }
            }

            return true;
        }
    }
}
