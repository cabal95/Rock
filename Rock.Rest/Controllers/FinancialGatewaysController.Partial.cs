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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using Rock.Financial;

namespace Rock.Rest.Controllers
{
    /// <summary>
    ///
    /// </summary>
    public partial class FinancialGatewaysController
    {
        /// <summary>
        /// Allows a IWebhookGatewayComponent to handle a webhook request from a gateway. This
        /// endpoint does not require authentication. If used, it is recommended to limit network
        /// traffic to this endpoint specifically to the gateway's URL
        /// </summary>
        /// <param name="guid">The FinancialGateway guid. This is a query parameter because
        /// those are encrypted by HTTPS.
        /// https://stackoverflow.com/a/2629241
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Route( "api/FinancialGateways/Webhook" )]
#if IS_NET_CORE
        public Microsoft.AspNetCore.Mvc.IActionResult HandleWebhook( [FromUri]Guid guid )
#else
        public HttpResponseMessage HandleWebhook( [FromUri]Guid guid )
#endif
        {
            if ( guid.IsEmpty() )
            {
#if IS_NET_CORE
                return NotFound();
#else
                return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
#endif
            }

            var financialGateway = Service.Get( guid );

            if ( financialGateway == null || !financialGateway.IsActive )
            {
#if IS_NET_CORE
                return NotFound();
#else
                return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
#endif
            }

            var webhookGatewayComponent = financialGateway.GetGatewayComponent() as IWebhookGatewayComponent;

            if ( webhookGatewayComponent == null )
            {
#if IS_NET_CORE
                return NotFound();
#else
                return ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
#endif
            }

#if IS_NET_CORE
            var success = webhookGatewayComponent.HandleWebhook( financialGateway, Request.Headers, Request );

            if ( success )
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
#else
            var success = webhookGatewayComponent.HandleWebhook( financialGateway, Request.Headers, HttpContext.Current.Request );
            var statusCode = success ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
            return ControllerContext.Request.CreateResponse( statusCode );
#endif
        }
    }
}