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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;

#if IS_NET_CORE
using Microsoft.EntityFrameworkCore;
#endif

using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.Workflow.Action.CheckIn
{
    /// <summary>
    /// Finds people with a relationship to members of family
    /// </summary>
    [ActionCategory( "Check-In" )]
    [Description( "Finds people with a relationship to members of family" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Find Relationships" )]
    public class FindRelationships : CheckInActionComponent
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
            List<int> roles = GetRoles( rockContext );

            var checkInState = GetCheckInState( entity, out errorMessages );
            if ( checkInState != null )
            {
                if ( !roles.Any() )
                {
                    return true;
                }

                var family = checkInState.CheckIn.CurrentFamily;
                if ( family != null )
                {
                    return ProcessForFamily( rockContext, family, checkInState.CheckInType != null && checkInState.CheckInType.PreventInactivePeople );
                }
                else
                {
                    errorMessages.Add( "There is not a family that is selected" );
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Gets the roles.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private static List<int> GetRoles( RockContext rockContext )
        {
            string cacheKey = "Rock.FindRelationships.Roles";

            List<int> roles = RockCache.Get( cacheKey ) as List<int>;

            if ( roles == null )
            {
                roles = new List<int>();

                foreach ( var role in new GroupTypeRoleService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( r => r.GroupType.Guid.Equals( new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS ) ) ) )
                {
                    role.LoadAttributes( rockContext );
                    if ( role.Attributes.ContainsKey( "CanCheckin" ) )
                    {
                        bool canCheckIn = false;
                        if ( bool.TryParse( role.GetAttributeValue( "CanCheckin" ), out canCheckIn ) && canCheckIn )
                        {
                            roles.Add( role.Id );
                        }
                    }
                }

                RockCache.AddOrUpdate( cacheKey, null, roles, RockDateTime.Now.AddSeconds( 300 ) );
            }

            return roles;
        }

        /// <summary>
        /// Processes for family.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="family">The family.</param>
        /// <param name="preventInactive">if set to <c>true</c> [prevent inactive]. Use CurrentCheckInState.CheckInType.PreventInactivePeople</param>
        /// <returns></returns>
        public static bool ProcessForFamily( RockContext rockContext, CheckInFamily family, bool preventInactive )
        {
            var dvInactive = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() );
            var roles = GetRoles( rockContext );

            var groupMemberService = new GroupMemberService( rockContext );

            var familyMemberIds = family.People.Select( p => p.Person.Id ).ToList();

            var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid() );
            if ( knownRelationshipGroupType != null )
            {
                var ownerRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid() );
                if ( ownerRole != null )
                {
                    // Get the Known Relationship group id's for each person in the family
                    var relationshipGroupIds = groupMemberService
                        .Queryable().AsNoTracking()
                        .Where( g =>
                            g.GroupRoleId == ownerRole.Id &&
                            familyMemberIds.Contains( g.PersonId ) )
                        .Select( g => g.GroupId );

                    // Get anyone in any of those groups that has a role with the canCheckIn attribute set
                    var personIds = groupMemberService
                        .Queryable().AsNoTracking()
                        .Where( g =>
                            relationshipGroupIds.Contains( g.GroupId ) &&
                            roles.Contains( g.GroupRoleId ) )
                        .Select( g => g.PersonId )
                        .ToList();

                    foreach ( var person in new PersonService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( p => personIds.Contains( p.Id ) )
                        .ToList() )
                    {
                        if ( !family.People.Any( p => p.Person.Id == person.Id ) )
                        {
                            if ( !preventInactive || dvInactive == null || person.RecordStatusValueId != dvInactive.Id )
                            {
                                var relatedPerson = new CheckInPerson();
                                relatedPerson.Person = person.Clone( false );
                                relatedPerson.FamilyMember = false;
                                family.People.Add( relatedPerson );
                            }
                        }
                    }
                }
            }

            return true;
        }

    }
}