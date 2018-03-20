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
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace com.centralaz.DpsMatch.Migrations
{
    [MigrationNumber( 6, "1.6.0" )]
    public class AlgorithmImprovements : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {

            Sql( @"ALTER PROCEDURE [dbo].[_com_centralaz_spDpsMatch_Match]
            AS
            BEGIN
                SET NOCOUNT ON

                BEGIN TRANSACTION

                -- prevent stored proc from running simultaneously since we are using #temp tables
                EXEC sp_getapplock '_com_centralaz_spDpsMatch_Match'
                    ,'Exclusive'
                    ,'Transaction'
                    ,0


               DECLARE @cScoreWeightFirstName INT = 30
			        ,@cScoreWeightLastName INT = 30
                    ,@cScoreWeightPostalCode INT = 20
                    ,@cScoreWeightGenderMatch INT = 5
                    ,@cScoreWeightGenderConflict INT = -20
					,@cScoreWeightAgeMatch INT = 5
					,@cScoreWeightAgeConflict INT = -20


                -- Guids that this proc uses
                DECLARE @cGROUPTYPE_FAMILY_GUID UNIQUEIDENTIFIER = '790E3215-3B10-442B-AF69-616C0DCB998E'
                    ,@cLOCATION_TYPE_HOME_GUID UNIQUEIDENTIFIER = '8C52E53C-2A66-435A-AE6E-5EE307D9A0DC'


                -- Other Declarations
                DECLARE @processDateTime DATETIME = SYSDATETIME()
                    ,@cGROUPTYPE_FAMILY_ID INT = (
                        SELECT TOP 1 [Id]
                        FROM GroupType
                        WHERE [Guid] = @cGROUPTYPE_FAMILY_GUID
                        )
                    ,@cLOCATION_TYPE_HOME_ID INT = (
                        SELECT TOP 1 [Id]
                        FROM DefinedValue
                        WHERE [Guid] = @cLOCATION_TYPE_HOME_GUID
                        )

                /*
                Populate Temporary Tables for each match criteria (PartialName, Address)
                */

                -- Find Matches by looking at people with the exact same lastname
                CREATE TABLE #OffenderMatchTable (
                    Id INT NOT NULL IDENTITY(1, 1)
                    ,OffenderId INT NOT NULL
                    ,PersonAliasId INT NOT NULL
					,MatchPercentage INT NOT NULL
					,LastNameMatch BIT NOT NULL
					,FirstNameMatch  BIT NOT NULL
					,PostalCodeMatch  BIT NOT NULL
					,AgeMatch  BIT NOT NULL
					,AgeConflict  BIT NOT NULL
					,GenderMatch  BIT NOT NULL
					,GenderConflict  BIT NOT NULL
                    );

                INSERT INTO #OffenderMatchTable (
                    OffenderId
                    ,PersonAliasId
					,MatchPercentage 
					,LastNameMatch
					,FirstNameMatch 
					,PostalCodeMatch
					,AgeMatch  
					,AgeConflict 
					,GenderMatch 
					,GenderConflict
                    )
                SELECT [e].[OffenderId]
                    ,[pa].[Id] [PersonAliasId]
					,@cScoreWeightLastName
					,1
					,0
					,0
					,0
					,0
					,0
					,0
                FROM (
                    SELECT [a].[LastName]
			            , [a].[OffenderId]
                    FROM (
                        SELECT [LastName]
				            , [Id] [OffenderId]
                        FROM [_com_centralaz_DpsMatch_Offender] [o]
                        WHERE isnull([LastName], '') != ''
                        GROUP BY [LastName]
				            , [Id]
                        ) [a]
                    ) [e]
                JOIN [Person] [p] ON [p].[LastName] = [e].[LastName]
                JOIN [PersonAlias] [pa] ON [pa].[PersonId] = [p].[Id]
                WHERE [pa].[AliasPersonId] = [pa].[PersonId] -- limit to only the primary alias


                -- Increment the score on potential matches that have the same Address
                Declare @AddressTable Table ( Id INT);

                INSERT INTO @AddressTable
				Select Id
				From (
					SELECT SUBSTRING([l].[PostalCode],0, 6) AS PersonZip
						,SUBSTRING(CAST([so].[ResidentialZip] AS NVARCHAR(20)),0,6) as OffenderZip
						,omt.Id AS 'Id'
					FROM #OffenderMatchTable omt
					JOIN [_com_centralaz_DpsMatch_Offender] [so] on so.Id = omt.OffenderId
					JOIN PersonAlias pa ON pa.Id = omt.PersonAliasId
					Join Person p on p.Id = pa.PersonId
					JOIN [GroupMember] [gm] ON [gm].[PersonId] = [p].[Id]
					JOIN [Group] [g] ON [gm].[GroupId] = [g].[Id]
					JOIN [GroupLocation] [gl] ON [gl].[GroupId] = [g].[id]
					JOIN [Location] [l] ON [l].[Id] = [gl].[LocationId]
						AND [g].[GroupTypeId] = @cGROUPTYPE_FAMILY_ID
					WHERE [gl].[GroupLocationTypeValueId] = @cLOCATION_TYPE_HOME_ID
						AND [pa].[AliasPersonId] = [pa].[PersonId] -- limit to only the primary alias
					) a
				Where a.PersonZip = a.OffenderZip

				UPDATE #OffenderMatchTable
                SET [MatchPercentage] = [MatchPercentage] + @cScoreWeightPostalCode
				,PostalCodeMatch = 1
                WHERE Id IN (
                        SELECT Id
                        FROM @AddressTable
                        )

                -- Increment the score on potential matches that have the same FirstName (or NickName)
                DECLARE @FirstNameTable TABLE (id INT);

                INSERT INTO @FirstNameTable
                Select distinct omt.Id
				From #OffenderMatchTable omt
				Join PersonAlias pa on omt.PersonAliasId = pa.Id
				Join Person p on pa.PersonId = p.Id
				Join _com_centralaz_DpsMatch_Offender o on omt.OffenderId = o.Id
				Join MetaNickNameLookup l on 
					(o.FirstName = l.FirstName or o.FirstName = l.NickName) and
					(
					p.FirstName = l.FirstName or
					p.FirstName = l.NickName or
					p.NickName = l.FirstName or
					p.NickName = l.NickName
					)

                UPDATE #OffenderMatchTable
                SET [MatchPercentage] = [MatchPercentage] + @cScoreWeightFirstName
				,FirstNameMatch = 1
                WHERE Id IN (
                        SELECT Id
                        FROM @FirstNameTable
                        )

				-- Increase the score on potential matches that have the same gender
                DECLARE @GenderMatch TABLE (id INT);

                INSERT INTO @GenderMatch
                SELECT pm.Id
					FROM #OffenderMatchTable pm
					JOIN PersonAlias pa ON pa.Id = pm.PersonAliasId
					JOIN _com_centralaz_DpsMatch_Offender so ON so.Id = pm.OffenderId
					JOIN Person p ON p.Id = pa.PersonId
					WHERE ((p.Gender=1 and so.Sex='M') or (p.Gender=2 and so.Sex='F'))

                UPDATE #OffenderMatchTable
                SET [MatchPercentage] = [MatchPercentage] + @cScoreWeightGenderMatch
				,GenderMatch = 1
                WHERE Id IN (
                        SELECT Id
                        FROM @GenderMatch
                        )

                -- Decrease the score on potential matches that have a different gender
                DECLARE @GenderConflict TABLE (id INT);

                INSERT INTO @GenderConflict
                SELECT pm.Id
					FROM #OffenderMatchTable pm
					JOIN PersonAlias pa ON pa.Id = pm.PersonAliasId
					JOIN _com_centralaz_DpsMatch_Offender so ON so.Id = pm.OffenderId
					JOIN Person p ON p.Id = pa.PersonId
					WHERE ((p.Gender=2 and so.Sex='M') or (p.Gender=1 and so.Sex='F'))

                UPDATE #OffenderMatchTable
                SET [MatchPercentage] = [MatchPercentage] + @cScoreWeightGenderConflict
				,GenderConflict = 1
                WHERE Id IN (
                        SELECT Id
                        FROM @GenderConflict
                        )

				-- Increase the score on potential matches that have an age difference of less than 10 years
                DECLARE @AgeMatch TABLE (id INT);

                INSERT INTO @AgeMatch
                SELECT pm.Id
					FROM #OffenderMatchTable pm
					JOIN PersonAlias pa ON pa.Id = pm.PersonAliasId
					JOIN _com_centralaz_DpsMatch_Offender so ON so.Id = pm.OffenderId
					JOIN Person p ON p.Id = pa.PersonId
					WHERE ABS((YEAR(GETDATE()) - p.BirthYear) - so.Age) < 10

                UPDATE #OffenderMatchTable
                SET [MatchPercentage] = [MatchPercentage] + @cScoreWeightAgeMatch
				,AgeMatch = 1
                WHERE Id IN (
                        SELECT Id
                        FROM @AgeMatch
                        )

				-- Decrease the score on potential matches that have an age difference of 10 years or greater
                DECLARE @AgeConflict TABLE (id INT);

                INSERT INTO @AgeConflict
                SELECT pm.Id
					FROM #OffenderMatchTable pm
					JOIN PersonAlias pa ON pa.Id = pm.PersonAliasId
					JOIN _com_centralaz_DpsMatch_Offender so ON so.Id = pm.OffenderId
					JOIN Person p ON p.Id = pa.PersonId
					WHERE ABS((YEAR(GETDATE()) - p.BirthYear) - so.Age) >= 10

                UPDATE #OffenderMatchTable
                SET [MatchPercentage] = [MatchPercentage] + @cScoreWeightAgeConflict
				,AgeConflict = 1
                WHERE Id IN (
                        SELECT Id
                        FROM @AgeConflict
                        )

				-- Delete any existing unchanged matches or matches below 60%
				
				Delete
				From #OffenderMatchTable
				Where MatchPercentage < 60
				Or Id in (
					Select omt.Id
					From #OffenderMatchTable omt
					Join _com_centralaz_DpsMatch_Match m on 
						(
						omt.OffenderId = m.OffenderId and 
						omt.PersonAliasId = m.PersonAliasId and 
						omt.MatchPercentage = m.MatchPercentage
						)
					)

                /*
                Merge Results of Matches into Match Table
                */

                -- Update Match table with results of match
                MERGE [_com_centralaz_DpsMatch_Match] AS TARGET
                USING (
                    SELECT [e1].[PersonAliasId] [PersonAliasId]
                        ,[e1].[OffenderId] [OffenderId]
						,[e1].[MatchPercentage][MatchPercentage]
                    FROM #OffenderMatchTable [e1]
                    ) AS source(PersonAliasId, OffenderId, MatchPercentage)
                    ON (target.PersonAliasId = source.PersonAliasId)
			            AND (target.OffenderId = source.OffenderId)
                WHEN MATCHED
                    THEN
                        UPDATE
			            SET [MatchPercentage] = source.[MatchPercentage]
				            ,[ModifiedDateTime] = @processDateTime
				WHEN NOT MATCHED
					THEN
						INSERT (
							PersonAliasId
							,OffenderId
							,MatchPercentage
							,ModifiedDateTime
							,CreatedDateTime
							,[Guid]
							)
						VALUES (
							source.PersonAliasId
							,source.OffenderId
							,source.MatchPercentage
							,@processDateTime
							,@processDateTime
							,NEWID()
							);

				Delete
				From _com_centralaz_DpsMatch_Match
				Where MatchPercentage < 60
				And IsMatch is null
										               
                /*
                Explicitly clean up temp tables before the proc exits (vs. have SQL Server do it for us after the proc is done)
                */
                DROP TABLE #OffenderMatchTable;



                COMMIT
            END


" );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
