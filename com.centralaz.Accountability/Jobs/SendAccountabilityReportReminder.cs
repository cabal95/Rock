// <copyright>
// Copyright by Central Christian Church
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
using System.Web;
using System.IO;

using Quartz;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Data;
using com.centralaz.Accountability.Model;
using com.centralaz.Accountability.Data;
using Rock.Web.Cache;
using Rock.Web;
using Rock.Communication;

namespace com.centralaz.Accountability.Jobs
{
    /// <summary>
    /// Job to send reminders to accountability group members to submit a report.
    /// </summary>
    [SystemEmailField( "Template", "The system email to use for sending the reminder", true, "", "", 1 )]
    [PersonField("Sender", "The person who the emails will be send on account of.")]
    [DisallowConcurrentExecution]
    public class SendAccountabilityReportReminder : IJob
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommunications"/> class.
        /// </summary>
        public SendAccountabilityReportReminder()
        {
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public virtual void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();
            var emailTemplate = dataMap.Get( "Template" ).ToString().AsGuid();
            var senderAliasGuid = dataMap.Get( "Sender" ).ToString().AsGuid();
            var senderAlias = new PersonAliasService( rockContext ).Get( senderAliasGuid );
            if ( senderAlias == null )
            {
                return;
            }

            var pageId = ( new PageService( rockContext ).Get( "64B5B1F6-472A-4C64-85B5-1F6864FE1992".AsGuid() ) ).Id;

            foreach ( var groupType in new GroupTypeService( rockContext ).Queryable() )
            {
                if ( groupType.InheritedGroupType != null && groupType.InheritedGroupType.Guid == "DC99BF69-8A1A-411F-A267-1AE75FDC2341".AsGuid() )
                {
                    foreach ( Group group in groupType.Groups )
                    {
                        group.LoadAttributes();
                        DateTime reportStartDate = DateTime.Parse( group.GetAttributeValue( "ReportStartDate" ).ToString() );
                        if ( reportStartDate.DayOfWeek == DateTime.Now.DayOfWeek )
                        {
                            DateTime nextDueDate = NextReportDate( reportStartDate );
                            int daysUntilDueDate = ( nextDueDate - DateTime.Today ).Days;
                            foreach ( GroupMember groupMember in group.Members )
                            {
                                ResponseSetService responseSetService = new ResponseSetService( new AccountabilityContext() );
                                // All caught up case
                                if ( daysUntilDueDate == 0 && !responseSetService.DoesResponseSetExistWithSubmitDate( nextDueDate, groupMember.PersonId, group.Id ) )
                                {
                                    Send( groupMember, pageId, emailTemplate, senderAlias.Person.Email );
                                }
                            }
                        }
                    }
                }
            }
        }

        protected DateTime NextReportDate( DateTime reportStartDate )
        {
            DateTime today = DateTime.Now;
            DateTime reportDue = today;

            int daysElapsed = ( today.Date - reportStartDate ).Days;
            if ( daysElapsed >= 0 )
            {
                int remainder = daysElapsed % 7;
                if ( remainder != 0 )
                {
                    int daysUntil = 7 - remainder;
                    reportDue = today.AddDays( daysUntil );
                }
            }
            else
            {
                reportDue = today.AddDays( -( daysElapsed ) );
            }
            return reportDue;
        }


        /// <summary>
        /// Sends the specified recipient.
        /// </summary>
        /// <param name="recipient">The recipient.</param>
        /// <param name="pageId">The page identifier.</param>
        /// <param name="systemEmailGuid">The system email's unique identifier.</param>
        /// <param name="senderEmail">The sender email address.</param>
        private void Send( GroupMember groupMember, int pageId, Guid systemEmailGuid, string senderEmail )
        {
            if ( groupMember.Person.Email != string.Empty && groupMember.Person.IsEmailActive )
            {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "GroupName", groupMember.Group.Name );
                String url = VirtualPathUtility.ToAbsolute( String.Format( "~/page/{0}?GroupId={1}", pageId, groupMember.GroupId ) );
                mergeFields.Add( "ReportPageUrl", url );

                var emailMessage = new RockEmailMessage( systemEmailGuid );
                emailMessage.AddRecipient( new RecipientData( groupMember.Person.Email, mergeFields ) );
                emailMessage.FromEmail = senderEmail;
                emailMessage.Send();
            }
        }
    }
}