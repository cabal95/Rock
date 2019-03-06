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
#if !IS_NET_CORE
using System.Data.Entity.Validation;
#endif
using System.Linq;
using Rock.Data;
#if IS_NET_CORE
using Rock.Data.Validation;
#endif
using Rock.Model;

namespace Rock.Transactions
{
    /// <summary>
    /// Writes entity audits 
    /// </summary>
    public class AuditTransaction : ITransaction
    {
        /// <summary>
        /// Gets or sets the audits.
        /// </summary>
        /// <value>
        /// The audits.
        /// </value>
        public List<Audit> Audits { get; set; }

        /// <summary>
        /// Execute method to write transaction to the database.
        /// </summary>
        public void Execute()
        {
            if ( Audits != null && Audits.Count > 0 )
            {
                try {
                    var auditsToAdd = Audits.Where( a => a.Details.Any() );
                    if ( auditsToAdd.Any() )
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var auditService = new AuditService( rockContext );

                            auditService.AddRange( auditsToAdd );

                            rockContext.SaveChanges( true );
                        }
                    }
                }
#if IS_NET_CORE
                catch ( EntityValidationException devx )
#else
                catch ( DbEntityValidationException devx )
#endif
                {
                    string errorMessage = string.Empty;

                    foreach (var eve in devx.EntityValidationErrors )
                    {
                        errorMessage += string.Format("Entity of type \"{0}\" in state \"{1}\" has the following validation errors: {", eve.Entry.Entity.GetType().Name, eve.Entry.State );
                        foreach ( var ve in eve.ValidationErrors )
                        {
                            errorMessage += string.Format( "Property: \"{0}\", Error: \"{1}\"",ve.PropertyName, ve.ErrorMessage );
                        }
                        errorMessage += "}";
                    }

                    throw new System.Exception( errorMessage, devx );
                }
            }
        }
    }
}