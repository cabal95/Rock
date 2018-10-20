/****** Object:  StoredProcedure [dbo].[_com_centralaz_spDiscoverMoreDashboard]    Script Date: 6/28/2018 11:57:26 AM ******/
IF EXISTS ( SELECT * FROM [sysobjects] WHERE ID = object_id(N'[dbo].[_com_centralaz_spDiscoverMoreDashboard]') and OBJECTPROPERTY(id, N'IsProcedure') = 1 )
BEGIN
    DROP PROCEDURE [dbo].[_com_centralaz_spDiscoverMoreDashboard]
END
GO

CREATE PROCEDURE [dbo].[_com_centralaz_spDiscoverMoreDashboard]
	@CampusId int = NULL,
	@SundayDate datetime = NULL

AS

-- Constants
DECLARE @DiscovereMoreAttrId int = 16374
DECLARE @BaptismInterestDateAttrId int = 16371
DECLARE @LifeGroupInterestDateAttrId int = 16372
DECLARE @ServingInterestDateAttrId int = 16923
DECLARE @BaptismDateAttrId int = 174
DECLARE @ServingGroupTypeId int = 23
DECLARE @LifeGroupTypeId int = 42

-- Get the month start/end date
DECLARE @MonthStart datetime = COALESCE( @SundayDate, GETDATE() )
SET @MonthStart = DATEADD( month, DATEDIFF(month, 0, @MonthStart ), 0) 
DECLARE @MonthEnd datetime = DATEADD( month, 1, @MonthStart)

-- Get the fiscal year start/end dates
-- Finally, grab the ministry year start for this year and last year
DECLARE @YearStart DATETIME = DATEADD(mm, 7, DATEADD(yy,DATEDIFF(yy,0,@MonthStart),0));
IF(@MonthStart < @YearStart)
BEGIN
	SET @YearStart = DATEADD(yy,-1, @YearStart);
END
DECLARE @YearEnd DATETIME = DATEADD(dd,-1, DATEADD(yy,1,@YearStart));

DECLARE @PersonTbl TABLE (
	PersonId int NOT NULL,
	CampusId int NULL,
	DiscoverMoreClassDate datetime NULL,
	BaptismInterestDate datetime NULL,
	ServingInterestDate datetime NULL,
	LifeGroupInterestDate datetime NULL,
	BaptismDate datetime NULL,
	ServingGroupDate datetime NULL,
	LifeGroupDate datetime NULL,
	ThisMonth bit,
	BaptismInterest int,
	ServingInterest int,
	LifeGroupInterest int,
	BaptismEngagementDays int,
	ServingEngagementDays int,
	LifeGroupEngagementDays int
)

-- Find those people with discover more date in selected month
INSERT INTO @PersonTbl ( [PersonId], [DiscoverMoreClassDate], [ThisMonth] )
SELECT 
	[EntityId], 
	[ValueAsDateTime],
	CASE WHEN [ValueAsDateTime] BETWEEN @MonthStart AND @MonthEnd THEN CAST( 1 as bit ) ELSE CAST( 0 as bit ) END
FROM [AttributeValue]
WHERE [AttributeId] = @DiscovereMoreAttrId
AND [ValueAsDateTime] IS NOT NULL
AND [ValueAsDateTime] BETWEEN @YearStart and @YearEnd

-- Check for Campus Filter
IF @CampusId IS NOT NULL AND @CampusId <> 0
BEGIN
	UPDATE PT SET [CampusId] = PC.[CampusId]
	FROM @PersonTbl PT
	CROSS APPLY ( 
		SELECT TOP 1 
			G.[CampusId]
		FROM [GroupMember] M
		INNER JOIN [Group] G ON G.[Id] = M.[GroupId] 
		WHERE G.[GroupTypeId] = 10
		AND G.[CampusId] IS NOT NULL
		AND M.[PersonId] = PT.[PersonId]
		ORDER BY M.[GroupOrder]
	) PC

	DELETE @PersonTbl
	WHERE ( [CampusId] IS NULL OR [CampusId] <> @CampusId )
END

-- Set Baptism Interest Date
UPDATE PT SET [BaptismInterestDate] = [ValueAsDateTime]
FROM @PersonTbl PT
INNER JOIN  [AttributeValue]
ON [AttributeId] = @BaptismInterestDateAttrId
AND [EntityId] = PT.[PersonID]
AND [ValueAsDateTime] IS NOT NULL

-- Set Serving Interest Date
UPDATE PT SET [ServingInterestDate] = [ValueAsDateTime]
FROM @PersonTbl PT
INNER JOIN  [AttributeValue]
ON [AttributeId] = @ServingInterestDateAttrId
AND [EntityId] = PT.[PersonID]
AND [ValueAsDateTime] IS NOT NULL

-- Set Life Group Interest Date
UPDATE PT SET [LifeGroupInterestDate] = [ValueAsDateTime]
FROM @PersonTbl PT
INNER JOIN  [AttributeValue]
ON [AttributeId] = @LifeGroupInterestDateAttrId
AND [EntityId] = PT.[PersonID]
AND [ValueAsDateTime] IS NOT NULL

-- Set Baptism Date
UPDATE PT SET [BaptismDate] = [ValueAsDateTime]
FROM @PersonTbl PT
INNER JOIN  [AttributeValue]
ON [AttributeId] = @BaptismDateAttrId
AND [EntityId] = PT.[PersonID]
AND [ValueAsDateTime] IS NOT NULL

-- Set Baptism Date
UPDATE PT SET [BaptismDate] = [ValueAsDateTime]
FROM @PersonTbl PT
INNER JOIN  [AttributeValue]
ON [AttributeId] = @BaptismDateAttrId
AND [EntityId] = PT.[PersonID]
AND [ValueAsDateTime] IS NOT NULL

-- Set the Serving Date and Life Group Date
UPDATE T SET
	[ServingGroupDate] = SG.[ServingDate],
	[LifeGroupDate] = LG.[LifeGroupDate]
FROM @PersonTbl T
OUTER APPLY (
	SELECT MIN( COALESCE( M.[DateTimeAdded], M.[CreatedDateTime] ) ) AS [ServingDate]
	FROM [GroupMember] M
	INNER JOIN [Group] G ON G.[Id] = M.[GroupId]
	WHERE M.[PersonId] = T.[PersonId]
	AND G.[GroupTypeId] = @ServingGroupTypeId
	AND COALESCE( M.[DateTimeAdded], M.[CreatedDateTime] ) IS NOT NULL
) SG
OUTER APPLY (
	SELECT MIN( COALESCE( M.[DateTimeAdded], M.[CreatedDateTime] ) ) AS [LifeGroupDate]
	FROM [GroupMember] M
	INNER JOIN [Group] G ON G.[Id] = M.[GroupId]
	WHERE M.[PersonId] = T.[PersonId]
	AND G.[GroupTypeId] = @LifeGroupTypeId
	AND COALESCE( M.[DateTimeAdded], M.[CreatedDateTime] ) IS NOT NULL
) LG

-- Calculate Interest
UPDATE @PersonTbl SET
	[BaptismInterest] = CASE WHEN [BaptismInterestDate] IS NOT NULL AND [BaptismInterestDate] >= [DiscoverMoreClassDate] THEN 1 ELSE 0 END,
	[ServingInterest] = CASE WHEN [ServingInterestDate] IS NOT NULL AND [ServingInterestDate] >= [DiscoverMoreClassDate] THEN 1 ELSE 0 END,
	[LifeGroupInterest] = CASE WHEN [LifeGroupInterestDate] IS NOT NULL AND [LifeGroupInterestDate] >= [DiscoverMoreClassDate] THEN 1 ELSE 0 END

-- Calculate Engagement
UPDATE @PersonTbl SET
	[BaptismEngagementDays] = CASE WHEN [BaptismInterest] = 1 AND [BaptismDate] IS NOT NULL AND [BaptismDate] >= [BaptismInterestDate] THEN DATEDIFF( day, [BaptismInterestDate], [BaptismDate] ) ELSE NULL END,
	[ServingEngagementDays] = CASE WHEN [ServingInterest] = 1 AND [ServingGroupDate] IS NOT NULL AND [ServingGroupDate] >= [ServingInterestDate] THEN DATEDIFF( day, [ServingInterestDate], [ServingGroupDate] ) ELSE NULL END,
	[LifeGroupEngagementDays] = CASE WHEN [LifeGroupInterest] = 1 AND [LifeGroupDate] IS NOT NULL AND [LifeGroupDate] >= [LifeGroupInterestDate] THEN DATEDIFF( day, [LifeGroupInterestDate], [LifeGroupDate] ) ELSE NULL END

--SELECT * FROM @PersonTbl

DECLARE @Summary TABLE (
	Category varchar(50),
	MonthlyInterest int,
	MonthlyEngagement int,
	MonthlyEngagementTotalDays int,
	YearlyInterest int,
	YearlyEngagement int,
	YearlyEngagementTotalDays int
)

INSERT INTO @Summary 
SELECT 
	'Baptism' AS [Category],
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [BaptismInterest] = 1 ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [BaptismEngagementDays] IS NOT NULL ),
	( SELECT SUM([BaptismEngagementDays]) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [BaptismEngagementDays] IS NOT NULL ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [BaptismInterest] = 1 ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [BaptismEngagementDays] IS NOT NULL ),
	( SELECT SUM([BaptismEngagementDays]) FROM @PersonTbl WHERE [BaptismEngagementDays] IS NOT NULL )

UNION ALL

SELECT
	'Serving',
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [ServingInterest] = 1 ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [ServingEngagementDays] IS NOT NULL ),
	( SELECT SUM([ServingEngagementDays]) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [ServingEngagementDays] IS NOT NULL ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ServingInterest] = 1 ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ServingEngagementDays] IS NOT NULL ),
	( SELECT SUM([ServingEngagementDays]) FROM @PersonTbl WHERE [ServingEngagementDays] IS NOT NULL )

UNION ALL

SELECT 
	'Life Group',
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [LifeGroupInterest] = 1 ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [LifeGroupEngagementDays] IS NOT NULL ),
	( SELECT SUM([LifeGroupEngagementDays]) FROM @PersonTbl WHERE [ThisMonth] = 1 AND [LifeGroupEngagementDays] IS NOT NULL ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [LifeGroupInterest] = 1 ),
	( SELECT COUNT(*) FROM @PersonTbl WHERE [LifeGroupEngagementDays] IS NOT NULL ),
	( SELECT SUM([LifeGroupEngagementDays]) FROM @PersonTbl WHERE [LifeGroupEngagementDays] IS NOT NULL )

SELECT
	[Category],

	[MonthlyInterest],
	[MonthlyEngagement],
	CASE WHEN [MonthlyEngagement] IS NOT NULL AND [MonthlyInterest] IS NOT NULL AND [MonthlyInterest] > 0
		THEN CAST([MonthlyEngagement] AS decimal(9,2))/CAST([MonthlyInterest] AS decimal(9,2))
		ELSE 0 END AS [MonthlyPercentage],
	[MonthlyEngagementTotalDays],
	CASE WHEN [MonthlyEngagementTotalDays] IS NOT NULL AND [MonthlyEngagement] IS NOT NULL AND [MonthlyEngagement] > 0
		THEN [MonthlyEngagementTotalDays]/[MonthlyEngagement]
		ELSE 0 END AS [MonthlyAverageDays],

	[YearlyInterest],
	[YearlyEngagement],
	CASE WHEN [YearlyEngagement] IS NOT NULL AND [YearlyInterest] IS NOT NULL AND [YearlyInterest] > 0
		THEN CAST([YearlyEngagement] AS decimal(9,2))/CAST([YearlyInterest] AS decimal(9,2))
		ELSE 0 END AS [YearlyPercentage],
	[YearlyEngagementTotalDays],
	CASE WHEN [YearlyEngagementTotalDays] IS NOT NULL AND [YearlyEngagement] IS NOT NULL AND [YearlyEngagement] > 0
		THEN [YearlyEngagementTotalDays]/[YearlyEngagement]
		ELSE 0 END AS [YearlyAverageDays]

FROM @Summary