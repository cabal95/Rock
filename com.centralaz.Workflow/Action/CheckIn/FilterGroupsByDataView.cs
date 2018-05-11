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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;

namespace Rock.Workflow.Action.CheckIn
{
    /// <summary>
    /// Removes (or excludes) the groups for each selected family member that are not 
    /// not in the configured dataview.  This filter can be configured to skip all
    /// group types that are not of the configured group type.
    /// </summary>
    [ActionCategory( "com_centralaz: Check-In" )]
    [Description( "Removes (or excludes) the groups for each selected family member that are not in the configured dataview.  This filter can be configured to skip all other group types that are not of the configured group type." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Filter Groups By Data View" )]
    [BooleanField( "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", true )]
    [GroupTypeField( "Group Type", "Only check-in groups of this type will adjusted by this check-in action filter. All other groups will pass through uneffected.", false )]
    [DataViewField( "Data View", "Select a person dataview that a person must be in -- otherwise their groups will be removed an an option from their check-in.", true, "468E0376-CB13-49C9-895B-098AD4A961D2", "Rock.Model.Person" )]
    [IntegerField( "Timeout", "Number of seconds to wait before timing out.", false, 5 )]
    public class FilterGroupsByDataView : CheckInActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, Model.WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState == null )
            {
                return false;
            }

            Guid? groupTypeGuid = GetAttributeValue( action, "GroupType" ).AsGuidOrNull();
            Guid? dataViewGuid = GetAttributeValue( action, "DataView" ).AsGuidOrNull();

            if ( !groupTypeGuid.HasValue || !dataViewGuid.HasValue )
            {
                return true;
            }

            var family = checkInState.CheckIn.Families.FirstOrDefault( f => f.Selected );
            if ( family != null )
            {
                // Skip if no family members have any matching group types (such as Children's Check-in)
                if ( ! family.People.Where( p => p.GroupTypes.Any( gt => gt.GroupType.Guid == groupTypeGuid ) ).Any() )
                {
                    return true;
                }
                
                var dataViewService = new DataViewService( rockContext );
                var dataView = dataViewService.Get( dataViewGuid.Value );
                if ( dataView == null )
                {
                    return true;
                }

                var timeout = GetAttributeValue( action, "Timeout" ).AsIntegerOrNull();
                var qry = dataView.GetQuery( null, timeout, out errorMessages );

                var remove = GetAttributeValue( action, "Remove" ).AsBoolean();

                foreach ( var person in family.People )
                {
                    foreach ( var groupType in person.GroupTypes.ToList() )
                    {
                        // skip non-matching group types.
                        if ( groupType.GroupType.Guid != groupTypeGuid )
                        {
                            continue;
                        }

                        if ( qry != null && !qry.Where( e => e.Id == person.Person.Id ).Any() )
                        {
                            // remove person's groups.
                            foreach ( var group in groupType.Groups.ToList() )
                            {
                                if ( remove )
                                {
                                    groupType.Groups.Remove( group );
                                }
                                else
                                {
                                    group.ExcludedByFilter = true;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}