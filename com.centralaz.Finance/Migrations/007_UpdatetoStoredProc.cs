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
namespace com.centralaz.Finance.Migrations
{
    [MigrationNumber( 7, "1.5.1" )]
    public class UpdateToStoredProc : Migration
    {
        public override void Up()
        {
            Sql( @"ALTER FUNCTION [dbo].[_com_centralaz_Finance_GetGivingIdsAndRowNumbers]
(
	  @StartDate datetime
	, @EndDate datetime
	, @MinimumAmount decimal
	, @CampusList nvarchar(max)
	, @StatementFrequencyAttributeId int
	, @DefinedValueList nvarchar(max)
	, @ExcludedGroupId int
)
RETURNS 
@GivingIdsAndRowNumbers TABLE 
(
	GivingId varchar(50),
	rowNumber int
)
AS
BEGIN
	-- Fill the table variable with the rows for your result set
	DECLARE @cTRANSACTION_TYPE_CONTRIBUTION uniqueidentifier = '2D607262-52D6-4724-910D-5C6E8FB89ACC'
	DECLARE @transactionTypeContributionId int = (select top 1 Id from DefinedValue where [Guid] = @cTRANSACTION_TYPE_CONTRIBUTION)
	DECLARE @cLOCATION_TYPE_HOME uniqueidentifier = '8C52E53C-2A66-435A-AE6E-5EE307D9A0DC'
	DECLARE @cGROUPTYPE_FAMILY uniqueidentifier = '790E3215-3B10-442B-AF69-616C0DCB998E'
	Declare @cRecordType_Business uniqueidentifier = 'BF64ADD3-E70A-44CE-9C4B-E76BBED37550'
	DECLARE @cLOCATION_TYPE_WORK uniqueidentifier = 'E071472A-F805-4FC4-917A-D5E3C095C35C'

	------------------------------------------------- Excluded Giving Ids Table
	Declare @excludedGivingIds table(
	GivingId nvarchar(50)
	)

	If @ExcludedGroupId is not null
	Begin
	Insert into @excludedGivingIds
	Select GivingId
	From GroupMember gm
	Join Person p on gm.PersonId = p.Id
	Where gm.GroupId = @ExcludedGroupId;
	End

	------------------------------------------------- Included Statement Preferences Table
	declare @IncludedStatementPreferences table(
	DefinedValueGuid uniqueidentifier
	)
	insert into @IncludedStatementPreferences
	Select [Guid]
	From DefinedValue
	Where Id in (SELECT * FROM [dbo].[ufnUtility_CsvToTable](@DefinedValueList))

	insert into @GivingIdsAndRowNumbers
	select GivingId,
	row_number() over (order by PostalCode) as 'rowNumber'
	from (
		select p.GivingId, max(l.PostalCode) as 'PostalCode'
		from FinancialTransaction ft
		join FinancialTransactionDetail ftd on ftd.TransactionId = ft.Id
		join PersonAlias pa on ft.AuthorizedPersonAliasId = pa.Id
		join Person p on pa.PersonId = p.Id		
		JOIN [GroupMember] [gm] ON [gm].[PersonId] = [p].[Id]
		JOIN [Group] [g] ON [gm].[GroupId] = [g].[Id]
		Join [GroupLocation] gl on gl.GroupId = g.Id
		JOIN [Location] [l] ON [l].[Id] = [gl].[LocationId]
		JOIN DefinedValue rt on p.RecordTypeValueId = rt.Id
		Left Join AttributeValue av on av.EntityId = p.Id and av.AttributeId = @StatementFrequencyAttributeId

		Where ft.TransactionTypeValueId = @transactionTypeContributionId
		and (@startDate is null or ft.TransactionDateTime >= @StartDate)
		and ( @endDate is null or ft.TransactionDateTime < @EndDate)				
		and ( ( p.GivingGroupId is null AND [g].[GroupTypeId] = (SELECT Id FROM GroupType WHERE [Guid] = @cGROUPTYPE_FAMILY)) or p.GivingGroupId = g.Id)
		and g.CampusId in (SELECT * FROM [dbo].[ufnUtility_CsvToTable](@CampusList))
		and [gl].[IsMailingLocation] = 1
		AND (
			(rt.[Guid] = @cRecordType_Business and [gl].[GroupLocationTypeValueId] = (SELECT Id FROM DefinedValue WHERE [Guid] = @cLOCATION_TYPE_WORK)) OR
			(rt.[Guid] != @cRecordType_Business and [gl].[GroupLocationTypeValueId] = (SELECT Id FROM DefinedValue WHERE [Guid] = @cLOCATION_TYPE_HOME))
			)
		and GivingId not in (select * from @excludedGivingIds)
		and (av.Value is null or not ( av.Value <> '' and  convert(uniqueidentifier,av.Value) not in (select * from @IncludedStatementPreferences)))
		group by GivingId
		having sum(ftd.Amount) >= @MinimumAmount		
		) g
	RETURN 
END
" );

            Sql( @"ALTER PROCEDURE [dbo].[_com_centralaz_Finance_GetGivingGroupTransactions]
	@StartDate datetime = NULL
	, @EndDate datetime = NULL
	, @Account1Id int = NULL
	, @Account2Id int = NULL
	, @Account3Id int = NULL
	, @Account4Id int = NULL
	, @MinimumAmount decimal = NULL
	, @ChapterNumber int = NULL
	, @ChapterSize int = NULL
	, @CampusList nvarchar(max) = NULL
	, @DefinedTypeAttributeId int = NULL
	, @DefinedValueList nvarchar(max) = NULL
	, @ExcludedGroupId int = NULL
	, @GivingId nvarchar(50) = NULL
	, @ReturnTransactions bit = NULL
	, @ReturnAddresses bit = NULL
	, @IsBusiness bit = NULL

AS
BEGIN
DECLARE @cTRANSACTION_TYPE_CONTRIBUTION uniqueidentifier = '2D607262-52D6-4724-910D-5C6E8FB89ACC'
DECLARE @transactionTypeContributionId int = (select top 1 Id from DefinedValue where [Guid] = @cTRANSACTION_TYPE_CONTRIBUTION)
DECLARE @cLOCATION_TYPE_HOME uniqueidentifier = '8C52E53C-2A66-435A-AE6E-5EE307D9A0DC'
DECLARE @cGROUPTYPE_FAMILY uniqueidentifier = '790E3215-3B10-442B-AF69-616C0DCB998E'
DECLARE @cLOCATION_TYPE_WORK uniqueidentifier = 'E071472A-F805-4FC4-917A-D5E3C095C35C'
Declare @cRecordType_Business uniqueidentifier = 'BF64ADD3-E70A-44CE-9C4B-E76BBED37550'

Declare @SelectedLocationType uniqueIdentifier;
If @IsBusiness = 1
Set @SelectedLocationType = @cLOCATION_TYPE_WORK
Else Set @SelectedLocationType = @cLOCATION_TYPE_HOME

Declare @givingIds table(
GivingId nvarchar(50)
)


If @GivingId is null
Begin
	insert into @givingIds
	select GivingId from (
		select * from [dbo].[_com_centralaz_Finance_GetGivingIdsAndRowNumbers](@StartDate, @EndDate, @MinimumAmount, @CampusList, @DefinedTypeAttributeId, @DefinedValueList, @ExcludedGroupId)
	)og
	where rowNumber between (@ChapterNumber-1)*@ChapterSize+1 and @ChapterNumber*@ChapterSize
end
else
Begin
	insert into @givingIds
	select @GivingId
End;

If @ReturnTransactions = 1
Begin 
Select
	p.GivingId as 'GivingId',
	ft.TransactionCode as 'TransactionCode',
	ft.TransactionDateTime as 'TransactionDateTime',
	Sum(case when ftd.AccountId = @Account1Id then ftd.Amount end) as 'Account 1 Sum',
	Sum(case when ftd.AccountId = @Account2Id then ftd.Amount end) as 'Account 2 Sum',
	Sum(case when ftd.AccountId = @Account3Id then ftd.Amount end) as 'Account 3 Sum',
	Sum(case when ftd.AccountId = @Account4Id then ftd.Amount end) as 'Account 4 Sum',
	Sum(case when (ftd.AccountId != @Account1Id and
	 ftd.AccountId != @Account2Id and
	  ftd.AccountId != @Account3Id and
	   ftd.AccountId != @Account4Id  )then ftd.Amount end) as 'Other Sum',
	Sum(ftd.Amount) as 'Total Sum'
from FinancialTransaction ft
join FinancialTransactionDetail ftd on ftd.TransactionId = ft.Id
join PersonAlias pa on ft.AuthorizedPersonAliasId = pa.Id
join Person p on pa.PersonId = p.Id
Where ft.TransactionTypeValueId = @transactionTypeContributionId
and (@startDate is null or ft.TransactionDateTime >= @StartDate)
and ( @endDate is null or ft.TransactionDateTime < @EndDate)
and p.GivingId in (select * from @givingIds)
group by ft.TransactionCode, ft.TransactionDateTime, p.GivingId
order by TransactionDateTime
End;

If @ReturnAddresses = 1
Begin
SELECT * FROM (
    SELECT 
		pg.GivingId	
		, [pt].[PersonNames] [AddressPersonNames]
		, [pn].[PersonNames] [GreetingPersonNames]
        , case when l.Id is null then 0 else 1 end [HasAddress]
		, [l].[Street1]
		, [l].[Street2]
		, [l].[City]
		, [l].[State]
		, [l].[PostalCode]
	FROM (
		-- Get distinct Giving Groups for Persons that have a specific GivingGroupId and have transactions that match the filter
		-- These are Persons that give as part of a Group.  For example, Husband and Wife
		SELECT DISTINCT
			null [PersonId] 
			, [g].[Id] [GroupId]
			,p.GivingId
			,p.RecordTypeValueId
		FROM  [Person] [p]
		INNER JOIN  [Group] [g] ON [p].[GivingGroupId] = [g].[Id]
        AND [p].GivingId in (SELECT GivingId FROM @givingIds)
		UNION
		-- Get Persons and their GroupId(s) that do not have GivingGroupId and have transactions that match the filter.        
		-- These are the persons that give as individuals vs as part of a group. We need the Groups (families they belong to) in order 
		-- to determine which address(es) the statements need to be mailed to 
		SELECT  
			[p].[Id] [PersonId],
			[g].[Id] [GroupId],
			p.givingId,
			p.RecordTypeValueId
		FROM [Person] [p]
		JOIN [GroupMember] [gm] ON [gm].[PersonId] = [p].[Id]
		JOIN [Group] [g] ON [gm].[GroupId] = [g].[Id]
		WHERE [p].[GivingGroupId] is null
		AND [g].[GroupTypeId] = (SELECT Id FROM GroupType WHERE [Guid] = @cGROUPTYPE_FAMILY)
		AND [p].GivingId IN (SELECT GivingId FROM @givingIds)
	) [pg]
	CROSS APPLY [ufnCrm_GetFamilyTitle]([pg].[PersonId], [pg].[GroupId], default, default) [pt]
	CROSS APPLY [ufnCrm_GetFamilyFirstOrNickNames]([pg].[PersonId], [pg].[GroupId], default, default) [pn]
	LEFT OUTER JOIN (
    SELECT l.*, gl.GroupId, gl.GroupLocationTypeValueId
	from [GroupLocation] [gl] 
	LEFT OUTER JOIN [Location] [l] ON [l].[Id] = [gl].[LocationId]
	WHERE [gl].[IsMailingLocation] = 1
        ) [l] 
        ON [l].[GroupId] = [pg].[GroupId]
	AND (
		(RecordTypeValueId = (SELECT Id FROM DefinedValue WHERE [Guid] = @cRecordType_Business) and l.[GroupLocationTypeValueId] = (SELECT Id FROM DefinedValue WHERE [Guid] = @cLOCATION_TYPE_WORK)) OR
		(RecordTypeValueId != (SELECT Id FROM DefinedValue WHERE [Guid] = @cRecordType_Business) and l.[GroupLocationTypeValueId] = (SELECT Id FROM DefinedValue WHERE [Guid] = @cLOCATION_TYPE_HOME))
		)
    ) n
    WHERE n.HasAddress = 1
    ORDER BY PostalCode
	OPTION (RECOMPILE)
End;

END" );
        }
        public override void Down()
        {
        }
    }
}
