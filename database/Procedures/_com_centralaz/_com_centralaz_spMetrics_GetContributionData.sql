/*
<doc>
	<summary>
 		This stored procedure returns a table containing contribution data for a given week.
	</summary>
	<returns>
		Returns a table containing contribution data for a given week.
	</returns>
	<param name="SundayDate" datatype="datetime">The date of a Sunday to target (optional, use NULL for last data weekend)</param>
	<remarks>	
	</remarks>
	<code>
		EXEC [dbo].[_com_centralaz_spMetrics_GetContributionData] @SundayDate
	</code>
</doc>
*/

IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[_com_centralaz_spMetrics_GetContributionData]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
	DROP PROCEDURE [dbo].[_com_centralaz_spMetrics_GetContributionData]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[_com_centralaz_spMetrics_GetContributionData]
	 @SundayDate NVARCHAR(50) = NULL

AS
BEGIN

----------------------------------------------------------------------------
-- QUERY PARAMETERS
----------------------------------------------------------------------------

DECLARE @SundayDateTime DATETIME = NULL

BEGIN TRY
	SET @SundayDateTime = CONVERT(DATE, @SundayDate);
END TRY

BEGIN CATCH

END CATCH

----------------------------------------------------------------------------
-- CONSTANTS
----------------------------------------------------------------------------
DECLARE @cTRANSACTION_TYPE_CONTRIBUTION UNIQUEIDENTIFIER = '2D607262-52D6-4724-910D-5C6E8FB89ACC'
DECLARE @transactionTypeContributionId INT = (SELECT TOP 1 Id FROM DefinedValue WHERE [Guid] = @cTRANSACTION_TYPE_CONTRIBUTION)

DECLARE @RootAttendanceMetricCategoryId INT = 540;

-- This holds any metrics that are not included in total attendance counts but are still displayed.
-- Currently includes Baptisms and First Time Guests
DECLARE @UncountedMetricCategoryId INT = 545

-- Here we build a table that contains all the categories for the metrics we'll be displaying on the page
DECLARE @MetricCategoryIds TABLE(
MetricCategoryId INT
);

INSERT INTO @MetricCategoryIds
SELECT Id
FROM dbo._com_centralaz_unfMetrics_GetDescendantCategoriesFromRoot(@RootAttendanceMetricCategoryId)
WHERE Id <> @UncountedMetricCategoryId;

DECLARE @ParentScheduleCategoryId INT = 50;

----------------------------------------------------------------------------
-- GET THE DATE RANGES
----------------------------------------------------------------------------

DECLARE @ThisWeekEnd DATETIME;
IF ( @SundayDate IS NULL OR @SundayDate = 'null')
	SET @ThisWeekEnd  = (
		SELECT Max(MetricValueDateTime)
		FROM  MetricValue mv
		JOIN Metric m ON mv.MetricId = m.Id
		JOIN MetricValuePartition mvpS ON mvpS.MetricValueId = mv.Id
		JOIN MetricPartition mpS ON mvpS.MetricPartitionId = mpS.Id AND mpS.EntityTypeId IN (SELECT TOP 1 Id FROM EntityType WHERE Name='Rock.Model.Schedule')
		JOIN Schedule s ON mvpS.EntityId = s.Id
		JOIN MetricCategory mc ON mc.MetricId = m.Id
		WHERE  mc.CategoryId IN (SELECT * FROM @MetricCategoryIds)
		AND s.CategoryId IN (SELECT Id FROM dbo.ufnChurchMetrics_GetDescendantCategoriesFromRoot(@ParentScheduleCategoryId))
		AND mv.YValue IS NOT NULL
		)
ELSE SET @ThisWeekEnd  = @SundayDate;
DECLARE @ThisWeekStart DATETIME= DATEADD(wk, DATEDIFF(wk, 0, @ThisWeekEnd -1), 0);
SET @ThisWeekEnd = DATEADD(ms, -1, DATEADD(DAY, 7, @ThisWeekStart));
DECLARE @LastWeekEnd DATETIME= DATEADD(wk, -1, @ThisWeekEnd)
DECLARE @LastWeekStart DATETIME= DATEADD(wk, -1, @ThisWeekStart);
DECLARE @LastYearWeekStart DATETIME= DATEADD(wk, -52, @ThisWeekStart);
DECLARE @LastYearWeekEnd DATETIME= DATEADD(wk, -52, @ThisWeekEnd);

----------------------------------------------------------------------------
-- GET THE GENERIC FINANCIALTRANSACTIONS JOINED TABLE
----------------------------------------------------------------------------
DECLARE @Transactions TABLE(
	CampusId INT,
	Amount FLOAT,
	AccountId FLOAT,
	AccountName nvarchar(MAX),
	TransactionDateTime DATETIME,
	CampusName nvarchar(MAX),
	BatchName nvarchar(MAX),
	SourceTypeName nvarchar(MAX)
);

INSERT INTO @Transactions
SELECT
	c.Id
	,ftd.Amount
	,fa.Id
	,fa.Name
	,ft.TransactionDateTime
	,c.Name
	,fb.Name
	,dv.Value
  FROM [FinancialTransactionDetail] ftd
  JOIN FinancialAccount fa ON ftd.AccountId = fa.Id
  JOIN FinancialTransaction ft ON ftd.TransactionId = ft.Id
  JOIN FinancialBatch fb ON ft.BatchId = fb.Id
 LEFT JOIN DefinedValue dv ON ft.SourceTypeValueId = dv.Id
JOIN Campus c ON fb.CampusId = c.Id
WHERE ft.TransactionTypeValueId = @transactionTypeContributionId
AND (
	( ft.TransactionDateTime >= @LastYearWeekStart AND ft.TransactionDateTime <= @LastYearWeekEnd) OR
	( ft.TransactionDateTime >= @LastWeekStart AND ft.TransactionDateTime <= @ThisWeekEnd)
	)

----------------------------------------------------------------------------
-- GRAB THE REQUESTED VALUES
----------------------------------------------------------------------------
SELECT *
FROM (
	SELECT titleTable.Campus AS 'Campus'
	, titleTable.[Order] AS 'Order'
	, Format(currentWeekOnline.Amount,'N0')  AS 'OnlineThisWeek'
	, Format(lastWeekOnline.Amount,'N0')  AS 'OnlineLastWeek'
	, Format(lastYearWeekOnline.Amount,'N0')  AS 'OnlineLastYear'
	, (currentWeekOnline.Amount - lastYearWeekOnline.Amount) / lastYearWeekOnline.Amount AS 'OnlineGrowth'
	, Format(currentWeekUndesignated.Amount,'N0')  AS 'UndesignatedThisWeek'
	, Format(lastWeekUndesignated.Amount,'N0')  AS 'UndesignatedLastWeek'
	, Format(lastYearWeekUndesignated.Amount,'N0')  AS 'UndesignatedLastYear'
	, (currentWeekUndesignated.Amount - lastYearWeekUndesignated.Amount) / lastYearWeekUndesignated.Amount AS 'UndesignatedGrowth'
	, Format(currentWeekSubtotal.Amount,'N0')  AS 'SubtotalThisWeek'
	, Format(lastWeekSubtotal.Amount,'N0')  AS 'SubtotalLastWeek'
	, Format(lastYearWeekSubtotal.Amount,'N0')  AS 'SubtotalLastYear'
	, (currentWeekSubtotal.Amount - lastYearWeekSubtotal.Amount) / lastYearWeekSubtotal.Amount AS 'SubtotalGrowth'
	, Format(currentWeekBuilding.Amount,'N0')  AS 'BuildingThisWeek'
	, Format(lastWeekBuilding.Amount,'N0')  AS 'BuildingLastWeek'
	, Format(lastYearWeekBuilding.Amount,'N0')  AS 'BuildingLastYear'
	, (currentWeekBuilding.Amount - lastYearWeekBuilding.Amount) / lastYearWeekBuilding.Amount AS 'BuildingGrowth'
	, Format(currentWeekGlobalOutreach.Amount,'N0')  AS 'GlobalOutreachThisWeek'
	, Format(lastWeekGlobalOutreach.Amount,'N0')  AS 'GlobalOutreachLastWeek'
	, Format(lastYearWeekGlobalOutreach.Amount,'N0')  AS 'GlobalOutreachLastYear'
	, (currentWeekGlobalOutreach.Amount - lastYearWeekGlobalOutreach.Amount) / lastYearWeekGlobalOutreach.Amount AS 'GlobalOutreachGrowth'
	, Format(currentWeekTotal.Amount,'N0')  AS 'TotalThisWeek'
	, Format(lastWeekTotal.Amount,'N0')  AS 'TotalLastWeek'
	, Format(lastYearWeekTotal.Amount,'N0')  AS 'TotalLastYear'
	, (currentWeekTotal.Amount - lastYearWeekTotal.Amount) / lastYearWeekTotal.Amount AS 'TotalGrowth'
	FROM -------------------------------------------------------------------------------------------------------- TitleTable
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,CASE GROUPING(CampusId) WHEN 1 THEN 1 ELSE 0 END AS 'Order'
		FROM @Transactions
		GROUP BY Rollup(CampusId)
	) AS titleTable
	LEFT JOIN -------------------------------------------------------------------------------------------------------- Online General Tables
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @ThisWeekStart AND TransactionDateTime <= @ThisWeekEnd
		AND (SourceTypeName = 'Website' OR SourceTypeName = 'Kiosk' OR SourceTypeName = 'Mobile Application')
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS currentWeekOnline
	ON titleTable.Campus = currentWeekOnline.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastWeekStart AND TransactionDateTime <= @LastWeekEnd
		AND (SourceTypeName = 'Website' OR SourceTypeName = 'Kiosk' OR SourceTypeName = 'Mobile Application')
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS lastWeekOnline
	ON titleTable.Campus = lastWeekOnline.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastYearWeekStart AND TransactionDateTime <= @LastYearWeekEnd
		AND (SourceTypeName = 'Website' OR SourceTypeName = 'Kiosk' OR SourceTypeName = 'Mobile Application')
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS lastYearWeekOnline
	ON titleTable.Campus = lastYearWeekOnline.Campus
	LEFT JOIN -------------------------------------------------------------------------------------------------------- In-Person General Tables
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @ThisWeekStart AND TransactionDateTime <= @ThisWeekEnd
		AND SourceTypeName = 'On-Site Collection'
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS currentWeekUndesignated
	ON titleTable.Campus = currentWeekUndesignated.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastWeekStart AND TransactionDateTime <= @LastWeekEnd
		AND SourceTypeName = 'On-Site Collection'
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS lastWeekUndesignated
	ON titleTable.Campus = lastWeekUndesignated.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastYearWeekStart AND TransactionDateTime <= @LastYearWeekEnd
		AND SourceTypeName = 'On-Site Collection'
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS lastYearWeekUndesignated
	ON titleTable.Campus = lastYearWeekUndesignated.Campus
	LEFT JOIN -------------------------------------------------------------------------------------------------------- Subtotal Tables
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @ThisWeekStart AND TransactionDateTime <= @ThisWeekEnd
		AND (SourceTypeName = 'Website' OR SourceTypeName = 'Kiosk' OR SourceTypeName = 'Mobile Application' OR SourceTypeName = 'On-Site Collection')
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS currentWeekSubtotal
	ON titleTable.Campus = currentWeekSubtotal.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastWeekStart AND TransactionDateTime <= @LastWeekEnd
		AND (SourceTypeName = 'Website' OR SourceTypeName = 'Kiosk' OR SourceTypeName = 'Mobile Application' OR SourceTypeName = 'On-Site Collection')
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS lastWeekSubtotal
	ON titleTable.Campus = lastWeekSubtotal.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastYearWeekStart AND TransactionDateTime <= @LastYearWeekEnd
		AND (SourceTypeName = 'Website' OR SourceTypeName = 'Kiosk' OR SourceTypeName = 'Mobile Application' OR SourceTypeName = 'On-Site Collection')
		AND AccountName = 'General'
		GROUP BY Rollup(CampusId)
	) AS lastYearWeekSubtotal
	ON titleTable.Campus = lastYearWeekSubtotal.Campus
	LEFT JOIN -------------------------------------------------------------------------------------------------------- Building Fund Tables
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @ThisWeekStart AND TransactionDateTime <= @ThisWeekEnd
		AND AccountName = 'Building'
		GROUP BY Rollup(CampusId)
	) AS currentWeekBuilding
	ON titleTable.Campus = currentWeekBuilding.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastWeekStart AND TransactionDateTime <= @LastWeekEnd
		AND AccountName = 'Building'
		GROUP BY Rollup(CampusId)
	) AS lastWeekBuilding
	ON titleTable.Campus = lastWeekBuilding.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastYearWeekStart AND TransactionDateTime <= @LastYearWeekEnd
		AND AccountName = 'Building'
		GROUP BY Rollup(CampusId)
	) AS lastYearWeekBuilding
	ON titleTable.Campus = lastYearWeekBuilding.Campus
	LEFT JOIN -------------------------------------------------------------------------------------------------------- Global Outreach Tables
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @ThisWeekStart AND TransactionDateTime <= @ThisWeekEnd
		AND AccountName = 'Global Outreach'
		GROUP BY Rollup(CampusId)
	) AS currentWeekGlobalOutreach
	ON titleTable.Campus = currentWeekGlobalOutreach.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastWeekStart AND TransactionDateTime <= @LastWeekEnd
		AND AccountName = 'Global Outreach'
		GROUP BY Rollup(CampusId)
	) AS lastWeekGlobalOutreach
	ON titleTable.Campus = lastWeekGlobalOutreach.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastYearWeekStart AND TransactionDateTime <= @LastYearWeekEnd
		AND AccountName = 'Global Outreach'
		GROUP BY Rollup(CampusId)
	) AS lastYearWeekGlobalOutreach
	ON titleTable.Campus = lastYearWeekGlobalOutreach.Campus
	LEFT JOIN -------------------------------------------------------------------------------------------------------- Total Tables
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @ThisWeekStart AND TransactionDateTime <= @ThisWeekEnd
		AND ( AccountName = 'General' OR AccountName = 'Building' OR AccountName = 'Global Outreach' )
		GROUP BY Rollup(CampusId)
	) AS currentWeekTotal
	ON titleTable.Campus = currentWeekTotal.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastWeekStart AND TransactionDateTime <= @LastWeekEnd
		AND ( AccountName = 'General' OR AccountName = 'Building' OR AccountName = 'Global Outreach' )
		GROUP BY Rollup(CampusId)
	) AS lastWeekTotal
	ON titleTable.Campus = lastWeekTotal.Campus
	LEFT JOIN
	(
		SELECT CASE GROUPING(CampusId) WHEN 1 THEN 'Total' ELSE Max(CampusName) END AS 'Campus'
		,Sum(Amount) AS 'Amount'
		FROM @Transactions
		WHERE TransactionDateTime >= @LastYearWeekStart AND TransactionDateTime <= @LastYearWeekEnd
		AND ( AccountName = 'General' OR AccountName = 'Building' OR AccountName = 'Global Outreach' )
		GROUP BY Rollup(CampusId)
	) AS lastYearWeekTotal
	ON titleTable.Campus = lastYearWeekTotal.Campus
	) innerTable
ORDER BY [Order] DESC, Campus ASC
END
GO