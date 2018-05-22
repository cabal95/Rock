// <copyright>
// Copyright by the Central Christian Church
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
using System.Data.Entity;
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Model;

namespace com.centralaz.RoomManagement.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class ReservationTypeService : Service<ReservationType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReservationTypeService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public ReservationTypeService( RockContext context ) : base( context ) { }

        public bool CanDelete( ReservationType item, out string errorMessage )
        {
            errorMessage = string.Empty;

            if ( new Service<Reservation>( Context ).Queryable().Any( a => a.ReservationTypeId == item.Id ) )
            {
                errorMessage = string.Format( "This {0} is assigned to a {1}.", ReservationType.FriendlyTypeName, Reservation.FriendlyTypeName );
                return false;
            }
            return true;
        }

        public static bool IsPersonInGroupWithId( Person person, int? groupId )
        {
            bool isInGroup = false;
            if ( groupId != null )
            {
                if ( person.Members.Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active && gm.Group.IsActive == true ).Select( m => m.GroupId ).Distinct().ToList().Contains( groupId.Value ) )
                {
                    isInGroup = true;
                }
            }

            return isInGroup;
        }

        public static bool IsPersonInGroupWithGuid( Person person, Guid? groupGuid )
        {
            bool isInGroup = false;
            if ( groupGuid != null )
            {
                if ( person.Members.Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active && gm.Group.IsActive == true ).Select( m => m.Group.Guid ).Distinct().ToList().Contains( groupGuid.Value ) )
                {
                    isInGroup = true;
                }
            }

            return isInGroup;
        }

    }
}
