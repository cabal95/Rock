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
using Rock.Plugin;

namespace com.centralaz.RoomManagement.Migrations
{
    [MigrationNumber( 19, "1.6.0" )]
    public class ReservationType : Migration
    {
        public override void Up()
        {
            Sql( @"
                CREATE TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType](
	                [Id] [int] IDENTITY(1,1) NOT NULL,
                    [IsSystem] [bit] NOT NULL,
                    [Name] [nvarchar](50) NULL,
	                [Description] [nvarchar](max) NULL,
	                [IsActive] [bit] NOT NULL,
                    [IconCssClass] [nvarchar](50) NULL,
                    [FinalApprovalGroupId] [int] NULL,
                    [SuperAdminGroupId] [int] NULL,
                    [NotificationEmailId] [int] NULL,
                    [DefaultSetupTime] [int] NULL,
                    [IsCommunicationHistorySaved] [bit] NOT NULL,
                    [IsNumberAttendingRequired] [bit] NOT NULL,
                    [IsContactDetailsRequired] [bit] NOT NULL,
                    [IsSetupTimeRequired] [bit] NOT NULL,
	                [Guid] [uniqueidentifier] NOT NULL,
	                [CreatedDateTime] [datetime] NULL,
	                [ModifiedDateTime] [datetime] NULL,
	                [CreatedByPersonAliasId] [int] NULL,
	                [ModifiedByPersonAliasId] [int] NULL,
	                [ForeignKey] [nvarchar](50) NULL,
                    [ForeignGuid] [uniqueidentifier] NULL,
                    [ForeignId] [int] NULL,
                 CONSTRAINT [PK__com_centralaz_RoomManagement_ReservationType] PRIMARY KEY CLUSTERED 
                (
	                [Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_FinalApprovalGroupId] FOREIGN KEY([FinalApprovalGroupId])
                REFERENCES [dbo].[Group] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_FinalApprovalGroupId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_SuperAdminGroupId] FOREIGN KEY([SuperAdminGroupId])
                REFERENCES [dbo].[Group] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_SuperAdminGroupId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_NotificationEmail] FOREIGN KEY([NotificationEmailId])
                REFERENCES [dbo].[SystemEmail] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_NotificationEmail]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_CreatedByPersonAliasId] FOREIGN KEY([CreatedByPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_CreatedByPersonAliasId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_ModifiedByPersonAliasId] FOREIGN KEY([ModifiedByPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_ModifiedByPersonAliasId]
" );
            var now = DateTime.Now;
            var sqlQry = string.Format( @"
INSERT INTO [dbo].[_com_centralaz_RoomManagement_ReservationType](
	                [IsSystem],
                    [Name],
	                [Description],
	                [IsActive],
                    [FinalApprovalGroupId],
                    [SuperAdminGroupId],
                    [NotificationEmailId],
                    [DefaultSetupTime],
                    [IsCommunicationHistorySaved],
                    [IsNumberAttendingRequired],
                    [IsContactDetailsRequired],
                    [IsSetupTimeRequired],
	                [Guid])
VALUES
                    (0,
                    '{0}',
                    '{1}',
                    1,
                    {2},
                    {3},
                    {4},
                    {5},
                    {6},
                    {7},
                    {8},
                    {9},
                    '{10}')
                    "
                    , "Default Reservation Type" // Name
                    , "The default reservation type." // Description
                    , "NULL" //Final Approval Group Id
                    , "NULL" // SuperAdminGroupId
                    , "NULL" //NotificationEmailId
                    , "NULL" //DefaultSetupTime
                    , 1 // Is Communication History Saved
                    , 1 // Is Number Attending Required
                    , 1 // Is Contact Details Required
                    , 1 // Is Setup Time Required
                    , Guid.NewGuid() // Guid
);
            Sql( sqlQry );


            Sql( @"
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationWorkflowTrigger] ADD [ReservationTypeId] INT NULL
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationMinistry] ADD [ReservationTypeId] INT NULL
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_Reservation] ADD [ReservationTypeId] INT NULL

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationWorkflowTrigger] WITH CHECK ADD CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationWorkflowTrigger_ReservationTypeId] FOREIGN KEY([ReservationTypeId])
                REFERENCES [dbo].[_com_centralaz_RoomManagement_ReservationType] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationMinistry] WITH CHECK ADD CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationMinistry_ReservationTypeId] FOREIGN KEY([ReservationTypeId])
                REFERENCES [dbo].[_com_centralaz_RoomManagement_ReservationType] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_Reservation] WITH CHECK ADD CONSTRAINT [FK__com_centralaz_RoomManagement_Reservation_ReservationTypeId] FOREIGN KEY([ReservationTypeId])
                REFERENCES [dbo].[_com_centralaz_RoomManagement_ReservationType] ([Id])
" );
            Sql( @"
            Update [_com_centralaz_RoomManagement_ReservationWorkflowTrigger]
            Set ReservationTypeId = 1

            Update [_com_centralaz_RoomManagement_ReservationMinistry]
            Set ReservationTypeId = 1

            Update [_com_centralaz_RoomManagement_Reservation]
            Set ReservationTypeId = 1

" );

            Sql( @"
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationWorkflowTrigger] ALTER COLUMN [ReservationTypeId] INT NOT NULL
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationMinistry] ALTER COLUMN [ReservationTypeId] INT NOT NULL
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_Reservation] ALTER COLUMN [ReservationTypeId] INT NOT NULL

" );

        }
        public override void Down()
        {

        }
    }
}
