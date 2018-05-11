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
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;

namespace Rock.Workflow.Action.CheckIn
{
    /// <summary>
    /// Loads the child groups (of the configured parent-group) for which the person is an active member. 
    /// </summary>
    [ActionCategory( "com_centralaz: Check-In" )]
    [Description( @"Loads the child groups (under the configured parent-group) for which the person is an active member and then attach them to the check-in group type so they can be 
selected (if there is more than one) during check-in and so that the attendance is recorded on the actual serving team. Additionally the group type's locations and those location's 
schedules will also be attached/copied to the groups so the check-in system works correctly. This check-in action should schedules be placed after the LoadLocation action.<br/><br/>
This action solves the problem of having hundreds of serving groups and that you don't want to manage as individual check-in groups--each with the same locations and schedules.
You can set up ONE general 'serving' check-in group (synced to a dataview of all your 'authorized' volunteers) and then use this action to point to your real serving teams (groups)
so you don't have to manage hundreds of group/location/schedules." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Load Child Groups Where Group Member" )]
    [GroupTypeField("Group Type", "Only check-in groups of this type will adjusted by this check-in action filter. All other groups will pass through uneffected.", false )]
    [GroupField("Parent Group", "Select a group from which the child groups will be selected from.", true, "1EFEA42D-27CF-480C-9815-3C16F4F8DD6C" )]
    public class LoadChildGroupsWhereGroupMember : CheckInActionComponent
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
            if ( checkInState != null )
            {
                Guid? groupTypeGuid = GetAttributeValue( action, "GroupType" ).AsGuidOrNull();
                Guid? parentGroupGuid = GetAttributeValue( action, "ParentGroup" ).AsGuidOrNull();

                if ( ! groupTypeGuid.HasValue || ! parentGroupGuid.HasValue )
                {
                    return true;
                }

                GroupService groupService = new GroupService( rockContext );
                var parentGroup = groupService.GetByGuid( parentGroupGuid.Value );

                foreach ( var family in checkInState.CheckIn.GetFamilies( true ) )
                {
                    foreach ( var person in family.GetPeople( selectedOnly: false) )
                    {
                        var groupType = person.GetGroupTypes( selectedOnly: false ).Where( x => x.GroupType.Guid == groupTypeGuid.Value ).FirstOrDefault();
                        if ( groupType != null )
                        {
                            var kioskGroupType = checkInState.Kiosk.ActiveGroupTypes( checkInState.ConfiguredGroupTypes )
                                .Where( g => g.GroupType.Id == groupType.GroupType.Id )
                                .FirstOrDefault();

                            // Now get all child groups of the configured parent group where the person is an active member
                            var qryGroups = groupService.GetAllDescendents( parentGroup.Id ).Where( g => g.IsActive )
                                .Where( g => g.Members.Any( gm => gm.PersonId == person.Person.Id && gm.GroupMemberStatus == GroupMemberStatus.Active ) )
                                .OrderBy( g => g.Name );

                            foreach ( var group in qryGroups )
                            {
                                var checkInGroup = new CheckInGroup();
                                checkInGroup.Group = group.Clone( false );
                                checkInGroup.Group.CopyAttributesFrom( group );
                                groupType.Groups.Add( checkInGroup );

                                // Copy all the kiosk's group types group's locations over to this checkInGroup
                                if ( kioskGroupType != null )
                                {
                                    foreach ( var kioskGroup in kioskGroupType.KioskGroups.Where( g => g.IsCheckInActive ) )
                                    {
                                        foreach ( var kioskLocation in kioskGroup.KioskLocations.Where( l => l.IsCheckInActive && l.IsActiveAndNotFull ) )
                                        {
                                            if ( !checkInGroup.Locations.Any( l => l.Location.Id == kioskLocation.Location.Id ) )
                                            {
                                                var checkInLocation = new CheckInLocation();
                                                checkInLocation.Location = kioskLocation.Location.Clone( false );
                                                checkInLocation.Location.CopyAttributesFrom( kioskLocation.Location );
                                                checkInLocation.CampusId = kioskLocation.CampusId;
                                                checkInLocation.Order = kioskLocation.Order;
                                                checkInGroup.Locations.Add( checkInLocation );

                                                // Now attach all possible schedules for this kiosk into the location and person
                                                foreach ( var kioskSchedule in kioskLocation.KioskSchedules.Where( s => s.IsCheckInActive ) )
                                                {
                                                    if ( !checkInLocation.Schedules.Any( s => s.Schedule.Id == kioskSchedule.Schedule.Id ) )
                                                    {
                                                        var checkInSchedule = new CheckInSchedule();
                                                        checkInSchedule.Schedule = kioskSchedule.Schedule.Clone( false );
                                                        checkInSchedule.StartTime = kioskSchedule.StartTime;
                                                        checkInLocation.Schedules.Add( checkInSchedule );
                                                    }

                                                    if ( checkInState.CheckInType != null &&
                                                        checkInState.CheckInType.TypeOfCheckin == TypeOfCheckin.Family &&
                                                        !person.PossibleSchedules.Any( s => s.Schedule.Id == kioskSchedule.Schedule.Id ) )
                                                    {
                                                        var checkInSchedule = new CheckInSchedule();
                                                        checkInSchedule.Schedule = kioskSchedule.Schedule.Clone( false );
                                                        checkInSchedule.StartTime = kioskSchedule.StartTime;
                                                        person.PossibleSchedules.Add( checkInSchedule );
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}