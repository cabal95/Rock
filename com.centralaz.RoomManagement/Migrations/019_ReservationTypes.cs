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
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
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

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_NotificationEmailId] FOREIGN KEY([NotificationEmailId])
                REFERENCES [dbo].[SystemEmail] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_NotificationEmailId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_CreatedByPersonAliasId] FOREIGN KEY([CreatedByPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_CreatedByPersonAliasId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]  WITH CHECK ADD  CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_ModifiedByPersonAliasId] FOREIGN KEY([ModifiedByPersonAliasId])
                REFERENCES [dbo].[PersonAlias] ([Id])

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] CHECK CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_ModifiedByPersonAliasId]
" );

            string sqlQry = GenerateDefaultReservationTypeSql();
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
            RockMigrationHelper.DeleteBlock( "2B864E89-27DE-41F9-A24B-8D2EA5C40D10" );
            RockMigrationHelper.DeleteBlockType( "6931E212-A76A-4DBB-9B97-86E5CDD0793A" );
            RockMigrationHelper.DeletePage( "CFF84B6D-C852-4FC4-B602-9F045EDC8854" ); //  Page: Reservation Configuration

            // Page: Reservation Types
            RockMigrationHelper.AddPage( true, "0FF1D7F4-BF6D-444A-BD71-645BD764EC40", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Reservation Types", "", "CFF84B6D-C852-4FC4-B602-9F045EDC8854", "fa fa-gear" ); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Reservation Type List", "Block to display the reservation types.", "~/Plugins/com_centralaz/RoomManagement/ReservationTypeList.ascx", "com_centralaz > Room Management", "F28B44CF-D49D-4A45-8189-381A1F942C86" );
            // Add Block to Page: Reservation Types, Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "CFF84B6D-C852-4FC4-B602-9F045EDC8854", "", "F28B44CF-D49D-4A45-8189-381A1F942C86", "Reservation Type List", "Main", "", "", 0, "87FCDDF1-D938-4BC9-AEA3-32F2B3F86494" );
            // Attrib for BlockType: Reservation Type List:Detail Page
            RockMigrationHelper.UpdateBlockTypeAttribute( "F28B44CF-D49D-4A45-8189-381A1F942C86", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "Page used to view details of a reservation type.", 0, @"", "A56ED80C-A8EC-44DD-84BA-03F12F281B9C" );
            // Attrib Value for Block:Reservation Type List, Attribute:Detail Page Page: Reservation Types, Site: Rock RMS
            RockMigrationHelper.AddBlockAttributeValue( "87FCDDF1-D938-4BC9-AEA3-32F2B3F86494", "A56ED80C-A8EC-44DD-84BA-03F12F281B9C", @"dc6d7ace-e23f-4ce6-9d66-a63348a1ef4e" );


            // Page: Reservation Type Detail
            RockMigrationHelper.AddPage( true, "CFF84B6D-C852-4FC4-B602-9F045EDC8854", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Reservation Type Detail", "", "DC6D7ACE-E23F-4CE6-9D66-A63348A1EF4E", "" ); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Reservation Type Detail", "Displays the details of the given Reservation Type for editing.", "~/Plugins/com_centralaz/RoomManagement/ReservationTypeDetail.ascx", "com_centralaz > Room Management", "CBAAEC6D-9B97-4FCB-96A9-5C53FB4E030E" );
            // Add Block to Page: Reservation Type Detail, Site: Rock RMS
            RockMigrationHelper.AddBlock( true, "DC6D7ACE-E23F-4CE6-9D66-A63348A1EF4E", "", "CBAAEC6D-9B97-4FCB-96A9-5C53FB4E030E", "Reservation Type Detail", "Main", "", "", 0, "160ED605-4BC3-46FD-8C24-A1BB9AD4ECB4" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( "160ED605-4BC3-46FD-8C24-A1BB9AD4ECB4" );
            RockMigrationHelper.DeleteBlockType( "CBAAEC6D-9B97-4FCB-96A9-5C53FB4E030E" );
            RockMigrationHelper.DeletePage( "DC6D7ACE-E23F-4CE6-9D66-A63348A1EF4E" ); //  Page: Reservation Type Detail

            RockMigrationHelper.DeleteAttribute( "A56ED80C-A8EC-44DD-84BA-03F12F281B9C" );
            RockMigrationHelper.DeleteBlock( "87FCDDF1-D938-4BC9-AEA3-32F2B3F86494" );
            RockMigrationHelper.DeleteBlockType( "F28B44CF-D49D-4A45-8189-381A1F942C86" );
            RockMigrationHelper.DeletePage( "CFF84B6D-C852-4FC4-B602-9F045EDC8854" ); //  Page: Reservation Types

            // Page: Reservation Configuration
            RockMigrationHelper.AddPage( "0FF1D7F4-BF6D-444A-BD71-645BD764EC40", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Reservation Configuration", "", "CFF84B6D-C852-4FC4-B602-9F045EDC8854", "fa fa-gear" ); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Reservation Configuration", "Displays the details of the given Connection Type for editing.", "~/Plugins/com_centralaz/RoomManagement/ReservationConfiguration.ascx", "com_centralaz > Room Management", "6931E212-A76A-4DBB-9B97-86E5CDD0793A" );
            RockMigrationHelper.AddBlock( "CFF84B6D-C852-4FC4-B602-9F045EDC8854", "", "6931E212-A76A-4DBB-9B97-86E5CDD0793A", "Reservation Configuration", "Main", "", "", 0, "2B864E89-27DE-41F9-A24B-8D2EA5C40D10" );


            Sql( @"
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationMinistry] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_Reservation_ReservationTypeId]
                ALTER TABLE[dbo].[_com_centralaz_RoomManagement_Reservation] DROP COLUMN[ReservationTypeId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_Reservation] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationMinistry_ReservationTypeId]
                ALTER TABLE[dbo].[_com_centralaz_RoomManagement_Reservation] DROP COLUMN[ReservationTypeId]

                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationWorkflowTrigger] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationWorkflowTrigger_ReservationTypeId]
                ALTER TABLE[dbo].[_com_centralaz_RoomManagement_Reservation] DROP COLUMN[ReservationTypeId]
" );
            Sql( @"
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_FinalApprovalGroupId]
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_SuperAdminGroupId]
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_NotificationEmailId]
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_CreatedByPersonAliasId]
                ALTER TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType] DROP CONSTRAINT [FK__com_centralaz_RoomManagement_ReservationType_ModifiedByPersonAliasId]
                DROP TABLE [dbo].[_com_centralaz_RoomManagement_ReservationType]" );
        }


        private string GenerateDefaultReservationTypeSql()
        {
            string finalApprovalGroupValue = null;
            string superAdminGroupValue = null;
            string notificationEmailValue = null;
            int? defaultSetupTime = -1;
            bool isCommunicationHistorySaved = false;
            bool isNumberAttendingRequired = true;
            bool isContactDetailsRequired = true;
            bool isSetupTimeRequired = true;

            var blockGuid = "65091E04-77CE-411C-989F-EAD7D15778A0".AsGuid();
            var rockContext = new RockContext();
            int? blockId = new BlockService( rockContext ).Queryable().Where( b => b.Guid == blockGuid ).Select( b => b.Id ).FirstOrDefault();
            if ( blockId.HasValue )
            {
                finalApprovalGroupValue = GetAttributeValueFromBlock( blockId.Value, "E715D25F-CA53-4B16-B8B2-4A94FD3A3560".AsGuid() );
                superAdminGroupValue = GetAttributeValueFromBlock( blockId.Value, "BBA41563-5379-43FA-955B-93C1926A4F66".AsGuid() );
                notificationEmailValue = GetAttributeValueFromBlock( blockId.Value, "F3FBDD84-5E9B-40C2-B199-3FAE1C2308DC".AsGuid() );

                var defaultSetupTimeValue = GetAttributeValueFromBlock( blockId.Value, "2FA0C64D-9511-4278-9445-BD0A847EA299".AsGuid() );
                if ( defaultSetupTimeValue != null )
                {
                    defaultSetupTime = defaultSetupTimeValue.AsIntegerOrNull();
                }

                var isCommunicationHistorySavedValue = GetAttributeValueFromBlock( blockId.Value, "B90006F5-9B17-48DD-B455-5BAA2BE1A9A2".AsGuid() );
                if ( isCommunicationHistorySavedValue != null )
                {
                    isCommunicationHistorySaved = isCommunicationHistorySavedValue.AsBoolean();
                }

                var isNumberAttendingRequiredValue = GetAttributeValueFromBlock( blockId.Value, "7162CFE4-FACD-4D75-8F09-2D42DBF1A887".AsGuid() );
                if ( isNumberAttendingRequiredValue != null )
                {
                    isNumberAttendingRequired = isNumberAttendingRequiredValue.AsBoolean();
                }

                var isContactDetailsRequiredValue = GetAttributeValueFromBlock( blockId.Value, "1C8DE8CB-E078-4483-9648-7C2CC57E6985".AsGuid() );
                if ( isContactDetailsRequiredValue != null )
                {
                    isContactDetailsRequired = isContactDetailsRequiredValue.AsBoolean();
                }

                var isSetupTimeRequiredValue = GetAttributeValueFromBlock( blockId.Value, "A184337B-BB99-4261-A295-0F54447CF0C6".AsGuid() );
                if ( isSetupTimeRequiredValue != null )
                {
                    isSetupTimeRequired = isSetupTimeRequiredValue.AsBoolean();
                }
            }

            var sqlQry = string.Format( @"
DECLARE @finalApprovalGroupId INT = NULL;
DECLARE @superAdminGroupId INT = NULL;
DECLARE @notificationEmailId INT = NULL;

SET @finalApprovalGroupId = (Select Id From [Group] Where [Guid] = '{2}')
SET @superAdminGroupId = (Select Id From [Group] Where [Guid] = '{3}')
SET @notificationEmailId = (Select Id From [SystemEmail] Where [Guid] = '{4}')

INSERT INTO [dbo].[_com_centralaz_RoomManagement_ReservationType](
	                [IsSystem],
                    [Name],
	                [Description],
	                [IsActive],
                    [IconCssClass],
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
                    (1,
                    '{0}',
                    '{1}',
                    1,
                    'fa fa-home',
                    @finalApprovalGroupId,
                    @superAdminGroupId,
                    @notificationEmailId,
                    {5},
                    {6},
                    {7},
                    {8},
                    {9},
                    'E443F926-0882-41D5-91EF-480EA366F660')"
                    , "Standard Reservation Type" // Name
                    , "The default reservation type." // Description
                    , !String.IsNullOrWhiteSpace( finalApprovalGroupValue ) ? finalApprovalGroupValue : Guid.Empty.ToString() //Final Approval Group Id
                    , !String.IsNullOrWhiteSpace( superAdminGroupValue ) ? superAdminGroupValue : Guid.Empty.ToString() // SuperAdminGroupId
                    , !String.IsNullOrWhiteSpace( notificationEmailValue ) ? notificationEmailValue : Guid.Empty.ToString() //NotificationEmailId
                    , defaultSetupTime.HasValue ? defaultSetupTime.ToString() : "NULL" //DefaultSetupTime
                    , isCommunicationHistorySaved ? 1 : 0 // Is Communication History Saved
                    , isNumberAttendingRequired ? 1 : 0 // Is Number Attending Required
                    , isContactDetailsRequired ? 1 : 0 // Is Contact Details Required
                    , isSetupTimeRequired ? 1 : 0 // Is Setup Time Required
);
            return sqlQry;
        }
        private string GetAttributeValueFromBlock( int blockId, Guid attributeGuid )
        {
            var value = new AttributeValueService( new RockContext() ).Queryable().Where( av =>
                   av.Attribute.Guid == attributeGuid &&
                   av.EntityId == blockId )
                .Select( av => av.Value )
                .FirstOrDefault();
            return value;
        }
    }
}
