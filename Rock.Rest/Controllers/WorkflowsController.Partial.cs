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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Net.Http;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Web.UI.Controls;
using System.Net;
using System;
using Rock.Web.Cache;

namespace Rock.Rest.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WorkflowsController
    {
        /// <summary>
        /// Initiates a new workflow
        /// </summary>
        /// <param name="workflowTypeId">The workflow type identifier.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/Workflows/WorkflowEntry/{workflowTypeId}" )]
#if IS_NET_CORE
        public Microsoft.AspNetCore.Mvc.IActionResult WorkflowEntry( int workflowTypeId )
#else
        public Rock.Model.Workflow WorkflowEntry( int workflowTypeId )
#endif
        {
            var rockContext = new Rock.Data.RockContext();
            var workflowType = WorkflowTypeCache.Get( workflowTypeId );

            if ( workflowType != null && ( workflowType.IsActive ?? true ) )
            {
                var workflow = Rock.Model.Workflow.Activate( workflowType, "Workflow From REST" );

                // set workflow attributes from querystring
#if IS_NET_CORE
                foreach ( var parm in Request.Query )
                {
#else
                foreach(var parm in Request.GetQueryStrings()){
#endif
                    workflow.SetAttributeValue( parm.Key, parm.Value );
                }

                // save -> run workflow
                List<string> workflowErrors;
                new Rock.Model.WorkflowService( rockContext ).Process( workflow, out workflowErrors );

#if IS_NET_CORE
                return StatusCode( ( int ) HttpStatusCode.Created, string.Empty );
#else
                var response = ControllerContext.Request.CreateResponse( HttpStatusCode.Created );
                return workflow;
#endif
            }
            else 
            {
#if IS_NET_CORE
                return NotFound();
#else
                var response = ControllerContext.Request.CreateResponse( HttpStatusCode.NotFound );
#endif
            }

            return null;

        }
    }
}
