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
using System.Data.Entity;
using System.Linq;

using Quartz;

using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace Rock.Jobs
{
    /// <summary>
    /// A run once job for V8
    /// </summary>
    /// <seealso cref="Quartz.IJob" />
    [DisallowConcurrentExecution]
    [DisplayName( "Data Migrations for v8.0" )]
    [Description( "This job will take care of any data migrations that need to occur after updating to v80. After all the operations are done, this job will delete itself." )]
    public class PostV80DataMigrations : IJob
    {
        /// <summary>
        /// Executes the specified context. When updating large data sets SQL will burn a lot of time updating the indexes. If performing multiple inserts/updates
        /// consider dropping the related indexes first and re-creating them once the opoeration is complete.
        /// Put all index creation method calls at the end of this method.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Execute( IJobExecutionContext context )
        {
            UpdateBulkUpdateSecurity();

            UpdateInteractionSummaryForPageViews();
            UpdateSlugForContentChannelItems();
            CreateIndexInteractionPersonAliasSession();

            // Keep these two last.
            CreateIndexInteractionsForeignKey();
            DeleteJob( context.GetJobId() );
        }

        /// <summary>
        /// Deletes the job.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        public static void DeleteJob( int jobId )
        {
            using ( var rockContext = new RockContext() )
            {
                var jobService = new ServiceJobService( rockContext );
                var job = jobService.Get( jobId );

                if ( job != null )
                {
                    jobService.Delete( job );
                    rockContext.SaveChanges();
                    return;
                }
            }
        }

        /// <summary>
        /// Add an index to help with performance of the Session List block
        /// </summary>
        private static void CreateIndexInteractionPersonAliasSession()
        {
            using ( RockContext rockContext = new RockContext() )
            {
                rockContext.Database.ExecuteSqlCommand( @"
    IF EXISTS ( SELECT * FROM sys.indexes WHERE NAME = 'IX_PersonAliasId_InteractionSessionId' AND object_id = OBJECT_ID('Interaction') )
	DROP INDEX [IX_PersonAliasId_InteractionSessionId] ON [dbo].[Interaction]

    CREATE NONCLUSTERED INDEX [IX_PersonAliasId_InteractionSessionId]
    ON [dbo].[Interaction] ([PersonAliasId],[InteractionSessionId])
    INCLUDE ([InteractionDateTime],[InteractionComponentId])
" );
            };
        }

        /// <summary>
        /// Creates the index on Interactions.ForeignKey.
        /// Includes were reccomended by Query Analyzer
        /// </summary>
        public static void CreateIndexInteractionsForeignKey()
        {
            using ( RockContext rockContext = new RockContext() )
            {
                rockContext.Database.CommandTimeout = 7200;
                rockContext.Database.ExecuteSqlCommand( @"IF NOT EXISTS( SELECT * FROM sys.indexes WHERE name = 'IX_ForeignKey' AND object_id = OBJECT_ID( N'[dbo].[Interaction]' ) ) 
                    BEGIN
                    CREATE NONCLUSTERED INDEX [IX_ForeignKey]
                    ON [dbo].[Interaction] ([ForeignKey])
                    INCLUDE ([Id]
	                    ,[InteractionDateTime]
	                    ,[Operation]
	                    ,[InteractionComponentId]
	                    ,[EntityId]
	                    ,[PersonAliasId]
	                    ,[InteractionSessionId]
	                    ,[InteractionSummary]
	                    ,[InteractionData]
	                    ,[CreatedDateTime]
	                    ,[ModifiedDateTime]
	                    ,[CreatedByPersonAliasId]
	                    ,[ModifiedByPersonAliasId]
	                    ,[Guid]
	                    ,[ForeignId]
	                    ,[ForeignGuid]
	                    ,[PersonalDeviceId]
	                    ,[RelatedEntityTypeId]
	                    ,[RelatedEntityId]
	                    ,[Source]
	                    ,[Medium]
	                    ,[Campaign]
	                    ,[Content]
	                    ,[InteractionEndDateTime])
                    END" );
            }
        }

        /// <summary>
        /// Updates the bulk update security.
        /// </summary>
        private static void UpdateBulkUpdateSecurity()
        {
            var rockContext = new RockContext();
            var authService = new Rock.Model.AuthService( rockContext );

            var bulkUpdateBlockType = BlockTypeCache.Get( Rock.SystemGuid.BlockType.BULK_UPDATE.AsGuid() );
            var bulkUpdateBlocks = new BlockService( rockContext ).Queryable().Where( a => a.BlockTypeId == bulkUpdateBlockType.Id ).ToList();
            foreach ( var bulkUpdateBlock in bulkUpdateBlocks )
            {
                var alreadyUpdated = authService.Queryable().Where( a =>
                    ( a.Action == "EditConnectionStatus" || a.Action == "EditRecordStatus" )
                    && a.EntityTypeId == bulkUpdateBlock.TypeId
                    && a.EntityId == bulkUpdateBlock.Id ).Any();

                if ( alreadyUpdated )
                {
                    // EditConnectionStatus and/or EditRecordStatus has already been set, so don't copy VIEW auth to it
                    continue;
                }

                var groupIdAuthRules = new HashSet<int>();
                var personIdAuthRules = new HashSet<int>();
                var specialRoleAuthRules = new HashSet<SpecialRole>();
                var authRulesToAdd = new List<AuthRule>();

                Dictionary<ISecured, List<AuthRule>> parentAuthRulesList = new Dictionary<ISecured, List<AuthRule>>();
                ISecured secured = bulkUpdateBlock;
                while ( secured != null )
                {
                    var entityType = secured.TypeId;
                    List<AuthRule> authRules = Authorization.AuthRules( secured.TypeId, secured.Id, Authorization.VIEW ).OrderBy( a => a.Order ).ToList();

                    foreach ( var rule in authRules )
                    {
                        if ( rule.GroupId.HasValue )
                        {
                            if ( !groupIdAuthRules.Contains(rule.GroupId.Value ) )
                            {
                                groupIdAuthRules.Add( rule.GroupId.Value );
                                authRulesToAdd.Add( rule );
                            }
                        }

                        else if ( rule.PersonId.HasValue )
                        {
                            if ( !personIdAuthRules.Contains( rule.PersonId.Value ) )
                            {
                                personIdAuthRules.Add( rule.PersonId.Value );
                                authRulesToAdd.Add( rule );
                            }
                        }
                        else if ( rule.SpecialRole != SpecialRole.None )
                        {
                            if ( !specialRoleAuthRules.Contains( rule.SpecialRole ) )
                            {
                                specialRoleAuthRules.Add( rule.SpecialRole );
                                authRulesToAdd.Add( rule );
                            }
                        }
                    }

                    secured = secured.ParentAuthority;
                }

                List<Auth> authsToAdd = new List<Auth>();

                foreach ( var auth in authRulesToAdd )
                {
                    authsToAdd.Add( AddAuth( bulkUpdateBlock, auth, "EditConnectionStatus" ) );
                    authsToAdd.Add( AddAuth( bulkUpdateBlock, auth, "EditRecordStatus" ) );
                }

                int authOrder = 0;
                authsToAdd.ForEach( a => a.Order = authOrder++ );

                authService.AddRange( authsToAdd );
                Authorization.RefreshAction( bulkUpdateBlock.TypeId, bulkUpdateBlock.Id, "EditConnectionStatus" );
                Authorization.RefreshAction( bulkUpdateBlock.TypeId, bulkUpdateBlock.Id, "EditRecordStatus" );
            }

            rockContext.SaveChanges();
        }

        private static Auth AddAuth( Block bulkUpdateBlock, AuthRule authRule, string authAction )
        {
            var auth = new Auth();
            auth.EntityTypeId = bulkUpdateBlock.TypeId;
            auth.EntityId = bulkUpdateBlock.Id;
            auth.Order = authRule.Order;
            auth.Action = authAction;
            auth.AllowOrDeny = authRule.AllowOrDeny.ToString();
            auth.GroupId = authRule.GroupId;
            auth.PersonAliasId = authRule.PersonAliasId;
            auth.SpecialRole = authRule.SpecialRole;

            return auth;
        }

        /// <summary>
        /// Updates the interaction summary on Interactions for page views.
        /// </summary>
        public static void UpdateInteractionSummaryForPageViews()
        {
            using ( RockContext rockContext = new RockContext() )
            {
                rockContext.Database.CommandTimeout = 7200;
                string sqlQuery = $@"DECLARE @ChannelMediumValueId INT = (SELECT [Id] FROM [DefinedValue] WHERE [Guid]='{SystemGuid.DefinedValue.INTERACTIONCHANNELTYPE_WEBSITE}')

                    UPDATE [Interaction]
                    SET [Interaction].[InteractionSummary] = [InteractionComponent].[Name]
                    FROM [Interaction]
                    INNER JOIN [InteractionComponent] ON [Interaction].[InteractionComponentId] = [InteractionComponent].[Id]
                    WHERE [InteractionComponent].[ChannelId] IN (SELECT [Id] FROM [InteractionChannel] WHERE [ChannelTypeMediumValueId] = @ChannelMediumValueId)";

                rockContext.Database.ExecuteSqlCommand( sqlQuery );
            }
        }

        /// <summary>
        /// Inserts the slug for Content Channel Items.
        /// </summary>
        public static void UpdateSlugForContentChannelItems()
        {
            int recordsToProcess = 1000;
            bool isProcess = true;

            do
            {
                using ( var rockContext = new RockContext() )
                {
                    var contentChannelItems = new ContentChannelItemService( rockContext )
                                        .Queryable()
                                        .AsNoTracking()
                                        .Where( a => !a.ContentChannelItemSlugs.Any() )
                                        .Take( recordsToProcess )
                                        .Select( a => new
                                        {
                                            a.Id,
                                            a.Title
                                        } ).ToList();

                    var slugService = new ContentChannelItemSlugService( rockContext );
                    if ( contentChannelItems.Any() )
                    {
                        foreach ( var item in contentChannelItems )
                        {
                            slugService.SaveSlug( item.Id, item.Title, null );
                        }
                    }
                    else
                    {
                        isProcess = false;
                    }
                }
            }
            while ( isProcess );
        }
    }
}
