/*
<doc>
	<summary>
 		This stored procedure returns the following tables of metrics for the given parameters:

		Table 1: Column data ( Names and orders )
		Table 2: Returns attendance data
	</summary>
	<returns>
		Returns 2 tables of data as described above.
	</returns>
	<param name="IsHoliday" datatype="bit">Boolean to indicate if the weekend is a holiday (special rules apply in that case)</param>
	<param name="Holiday" datatype="NVARCHAR(50)">The name of the holiday: Christmas, Easter, Thanksgiving (ignored if IsHoliday = 0)</param>
	<param name="SundayDate" datatype="datetime">The date of a Sunday to target (optional, use NULL for last data weekend)</param>
	<param name="MetricCategoryId" datatype="int">Any metrics in this or child categories will be included in the report</param>
	<param name="GroupingMethod" datatype="int">This parameter dictates how the metrics are grouped. They are grouped as follows:
		@GroupingMethod == 0: Group By Metric Name
		@GroupingMethod == 1: Groub by first word of Metric Name
		@GroupingMethod == 2: Group by if Metric contains 'SM' or 'Servant Minister'
		@GroupingMethod == 3: Group by Metric Category Name
	</param>
	<param name="ScheduleId" datatype="int">Normally null, if this parameter has a value then the stored procedure will only deliver metric values tied to the corresponding schedule.</param>
	<remarks>	
	</remarks>
	<code>
		EXEC [dbo].[_com_centralaz_spMetrics_GetSingleWeekMetricData] @IsHoliday, @Holiday, @SundayDate, @MetricCategoryId, @GroupingMethod
	</code>
</doc>
*/

IF EXISTS (select * from dbo.sysobjects where id = object_id(N'[dbo].[_com_centralaz_spMetrics_GetSingleWeekMetricData]') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
	DROP PROCEDURE [dbo].[_com_centralaz_spMetrics_GetSingleWeekMetricData]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[_com_centralaz_spMetrics_GetSingleWeekMetricData]
	 @IsHoliday BIT = 0,
	 @Holiday NVARCHAR(50) = 'Christmas',
	 @SundayDate NVARCHAR(50) = NULL,
	 @MetricCategoryId INT = 541,
	 @GroupingMethod INT = 0,
	 @ScheduleId INT = NULL

AS
BEGIN
----------------------------------------------------------------------------
-- QUERY PARAMETERS
----------------------------------------------------------------------------

-- The following declarations are here to make it easy to extract the stored procedure into a new query
-- window for editing and testing. By default they should be commented out.
-- BEGIN FOR TESTING

--DECLARE @IsHoliday BIT = 1;
--DECLARE @Holiday NVARCHAR(50) = 'Christmas';
--DECLARE @SundayDate NVARCHAR(50) = NULL;
--DECLARE @MetricCategoryId INT = 541;
--DECLARE @GroupingMethod INT = 0; -- 0: 1-1, 1: By first word, 2: By child vs volunteer, 3: By category
--DECLARE @ScheduleId INT = NULL;

-- END FOR TESTING

DECLARE @SundayDateTime DATETIME = NULL

BEGIN TRY
	SET @SundayDateTime = CONVERT(DATE, @SundayDate);
END TRY

BEGIN CATCH

END CATCH 

----------------------------------------------------------------------------
-- GET THE METRIC CATEGORY IDS
----------------------------------------------------------------------------
-- These are the root category Ids of the metrics included in the total attendance counts
DECLARE @RootAttendanceMetricCategoryId INT = 540;

DECLARE @WorshipMetricCategoryId INT = 543;
DECLARE @ChildrenMetricCategoryId INT = 541;
DECLARE @StudentsMetricCategoryId INT = 542;
DECLARE @WorshipNightMetricCategoryId INT = 544;

-- This holds any metrics that are not included in total attendance counts but are still displayed.
-- Currently includes Baptisms and First Time Guests
DECLARE @UncountedMetricCategoryId INT = 545 

-- Here we build a table that contains all the categories for the metrics we'll be displaying on the page
DECLARE @MetricCategoryIds TABLE(
MetricCategoryId INT
);

INSERT INTO @MetricCategoryIds SELECT Id FROM dbo._com_centralaz_unfMetrics_GetDescendantCategoriesFromRoot(@MetricCategoryId);

----------------------------------------------------------------------------
-- GET THE SCHEDULE CATEGORY IDS
----------------------------------------------------------------------------

-- These are the root category Ids of the schedules used in the total attendance counts
DECLARE @WeekendScheduleCategoryId INT = 50;
DECLARE @StudentsScheduleCategoryId INT = 496;
DECLARE @SpecialEventsCategoryId INT = 138;

DECLARE @HolidayScheduleCategoryId INT;
IF @Holiday = 'Christmas'
	SET @HolidayScheduleCategoryId = 448	
ELSE IF @Holiday = 'Easter'
	SET @HolidayScheduleCategoryId = 284
ELSE IF @Holiday = 'Thanksgiving'
	SET @HolidayScheduleCategoryId = 457
ELSE SET @HolidayScheduleCategoryId = 448

-- Here we build a table that contains all the categories for the schedules we'll be displaying on the page
DECLARE @ScheduleCategoryIds TABLE(
ScheduleCategoryId INT
);

IF @IsHoliday = 1
	-- This holds the correct holiday schedules
	INSERT INTO @ScheduleCategoryIds SELECT Id FROM dbo._com_centralaz_unfMetrics_GetDescendantCategoriesFromRoot(@HolidayScheduleCategoryId) 
ELSE
BEGIN
	-- These are normal weekend schedules
	INSERT INTO @ScheduleCategoryIds SELECT Id FROM dbo._com_centralaz_unfMetrics_GetDescendantCategoriesFromRoot(@WeekendScheduleCategoryId)

	-- These are the student's schedules ( Including Wed Night Bible Study )
	INSERT INTO @ScheduleCategoryIds SELECT Id FROM dbo._com_centralaz_unfMetrics_GetDescendantCategoriesFromRoot(@StudentsScheduleCategoryId) 

	-- These are schedules for events we want to track attendance for, like Worship Night
	INSERT INTO @ScheduleCategoryIds SELECT Id FROM dbo._com_centralaz_unfMetrics_GetDescendantCategoriesFromRoot(@SpecialEventsCategoryId) 
END

----------------------------------------------------------------------------
-- GET THE GENERIC METRICVALUE JOINED TABLE
----------------------------------------------------------------------------
DECLARE @DummyTotalICalContent NVARCHAR(MAX) = 'DTSTART:20130501T235959
RRULE:FREQ=WEEKLY;BYDAY=ZZ
SEQUENCE:0';

DECLARE @MetricValues TABLE(
	[CampusName] NVARCHAR(50) NULL,
	[CampusOrder] INT NULL,
	[ScheduleName] NVARCHAR(50) NULL,
	[ScheduleOrder] INT NULL,
	[ColumnName] NVARCHAR(MAX) NULL,
	[ColumnOrder] INT NULL,
	[MetricTitle] NVARCHAR(50) NULL,
	[MetricCategoryName] NVARCHAR(50) NULL,
	[MetricCategoryOrder] INT NULL,
	[Attendance] FLOAT NULL,
	[MetricValueDateTime] DATETIME NULL,
	[ScheduleId] INT NULL,
	[ScheduleICalendarContent] NVARCHAR(MAX) NULL,
	[FirstWord] NVARCHAR(MAX) NULL,
	[ServantMinister] NVARCHAR(MAX) NULL
)

-- Here we dump all the relevant metrics and their accessory information into a custom table that 
-- we'll work off of from here on out
INSERT INTO @MetricValues
SELECT c.Name
	,NULL
	,s.Name
	,NULL
	,NULL
	,NULL
	,m.Title
	,cat.Name
	,CASE
		WHEN  mc.CategoryId = @WorshipMetricCategoryId THEN 1
		WHEN  mc.CategoryId = @WorshipNightMetricCategoryId THEN 2
		WHEN  mc.CategoryId = @ChildrenMetricCategoryId THEN 3
		WHEN  mc.CategoryId = @StudentsMetricCategoryId THEN 4
	END
	,mv.YValue
	,mv.MetricValueDateTime
	,s.Id
	,s.iCalendarContent
	,(SELECT CASE CHARINDEX(' ', m.Title, 1)
			WHEN 0 
			THEN m.Title -- empty or single word
			ELSE SUBSTRING(m.Title, 1, CHARINDEX(' ', m.Title, 1) - 1) -- multi-word
			END)
	,(SELECT CASE 
			WHEN (m.Title LIKE '%SM%' or m.Title LIKE '%Servant Minister%') 
			THEN 'Servant Ministers'
			ELSE 'Children' 
			END)
FROM MetricValue mv
JOIN Metric m ON mv.MetricId = m.Id
JOIN MetricValuePartition mvpC ON mvpC.MetricValueId = mv.Id
JOIN MetricPartition mpC ON mvpC.MetricPartitionId = mpC.Id AND mpC.EntityTypeId IN (SELECT TOP 1 Id FROM EntityType WHERE Name='Rock.Model.Campus')
JOIN Campus c ON mvpC.EntityId = c.Id
JOIN MetricValuePartition mvpS ON mvpS.MetricValueId = mv.Id
JOIN MetricPartition mpS ON mvpS.MetricPartitionId = mpS.Id AND mpS.EntityTypeId IN (SELECT TOP 1 Id FROM EntityType WHERE Name='Rock.Model.Schedule')
JOIN Schedule s ON mvpS.EntityId = s.Id
JOIN MetricCategory mc ON mc.MetricId = m.Id
JOIN Category cat ON cat.Id = mc.CategoryId
WHERE  mc.CategoryId IN (SELECT * FROM @MetricCategoryIds )
AND ( 
	(@ScheduleId IS NULL AND s.CategoryId IN (SELECT * FROM @ScheduleCategoryIds)) OR
	(@ScheduleId IS NOT NULL AND s.Id = @ScheduleId)
	)
AND mv.YValue IS NOT NULL
AND mc.CategoryId <> @UncountedMetricCategoryId

----------------------------------------------------------------------------
-- GET THE DATE RANGES
----------------------------------------------------------------------------

DECLARE @ThisWeekEnd DATETIME;
IF ( @SundayDate IS NULL OR @SundayDate = 'null') 
	SET @ThisWeekEnd  = (
		SELECT Max(MetricValueDateTime) 
		FROM @MetricValues
		)
ELSE SET @ThisWeekEnd = @SundayDate;
DECLARE @ThisWeekStart DATETIME = DATEADD(wk, DATEDIFF(wk, 0, @ThisWeekEnd -1), 0)
SET @ThisWeekEnd = DATEADD(DAY, 6, @ThisWeekStart);

----------------------------------------------------------------------------
-- TRIM DOWN METRIC TABLE
----------------------------------------------------------------------------

-- Next we'll slim down the @MetricValues table to only include values counted towards totals, as well as only 
-- including values that fall within the time periods that will be shown
DELETE
FROM @MetricValues
WHERE NOT ( MetricValueDateTime >= @ThisWeekStart AND MetricValueDateTime <= @ThisWeekEnd)

----------------------------------------------------------------------------
-- SET GROUPING DATA
----------------------------------------------------------------------------
/**
@GroupingMethod == 0: Group By Metric Name
@GroupingMethod == 1: Groub by first word of Metric Name
@GroupingMethod == 2: Group by if Metric contains 'SM' or 'Servant Minister'
@GroupingMethod == 3: Group by Metric Category Name
**/

IF @GroupingMethod = 0
	BEGIN
		UPDATE @MetricValues
		SET [ColumnName] = MetricTitle
	END
ELSE IF @GroupingMethod = 1
	BEGIN
		UPDATE @MetricValues
		SET [ColumnName] = FirstWord
	END
ELSE IF @GroupingMethod = 2
	BEGIN
		UPDATE @MetricValues
		SET [ColumnName] = ServantMinister
	END
ELSE IF @GroupingMethod = 3
	BEGIN
		UPDATE @MetricValues
		SET [ColumnName] = MetricCategoryName
	END

-----------------------------------------------------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------------------------------------------------
------
------ BUILD DATA TABLES
------
-----------------------------------------------------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------------------------------------------------

----------------------------------------------------------------------------
--  TABLE 1: GRAB THE REFERENCE DATA
----------------------------------------------------------------------------

-- This returns a reference table with the parameters and years so that the lava template has access to them.
SELECT DISTINCT ColumnName, ServantMinister, ColumnOrder FROM (
	SELECT CASE GROUPING(ColumnName) WHEN 1 THEN 'Total' ELSE MAX(ColumnName) END AS 'ColumnName'
	,CASE GROUPING(ColumnName)  WHEN 1 THEN 1 ELSE 0 END AS 'ColumnOrder'	
	,MIN(ServantMinister) AS 'ServantMinister'	
	 FROM @MetricValues
	 GROUP BY ROLLUP(ColumnName)
	 ) x
	ORDER BY ColumnOrder DESC, ServantMinister, ColumnName;

----------------------------------------------------------------------------
--  TABLE 2: GRAB THE ATTENDANCE DATA
----------------------------------------------------------------------------

SELECT *
, [dbo].[_com_centralaz_unfMetrics_GetServiceDayOrder]([Date]) AS 'ServiceDayOrder'
FROM 
( SELECT DISTINCT CampusName, IsCampusTotal, IsScheduleTotal,ScheduleName, iCalendarContent 
	,CASE WHEN ( CHARINDEX('DTSTART:', iCalendarContent) <> 0 AND CHARINDEX('RRULE:', iCalendarContent)<>0) THEN SUBSTRING(iCalendarContent, CHARINDEX('DTSTART:', iCalendarContent) + 16, (CHARINDEX('RRULE:', iCalendarContent)-(CHARINDEX('DTSTART:', iCalendarContent)+16))) ELSE 'T235959' END AS 'Time'
	,CASE WHEN ( CHARINDEX('BYDAY=', iCalendarContent) <> 0 AND CHARINDEX('SEQUENCE:', iCalendarContent)<>0) THEN SUBSTRING(iCalendarContent, CHARINDEX('BYDAY=', iCalendarContent) + 6, (CHARINDEX('SEQUENCE:', iCalendarContent)-(CHARINDEX('BYDAY=', iCalendarContent)+6))) Else 'ZZ' END AS 'Date'	
	FROM (
	SELECT CASE GROUPING(CampusName) WHEN 1 THEN 'Total' ELSE MAX(CampusName) END AS 'CampusName'
		,CASE GROUPING(CampusName) WHEN 1 THEN 1 ELSE 0 END AS 'IsCampusTotal'
		,CASE GROUPING(ScheduleName) WHEN 1 THEN 'Total' ELSE MAX(ScheduleName) END AS 'ScheduleName'
		,CASE GROUPING(ScheduleName) WHEN 1 THEN 1 ELSE 0 END AS 'IsScheduleTotal'		
		,CASE GROUPING(ScheduleName) WHEN 1 THEN @DummyTotalICalContent ELSE MAX(ScheduleICalendarContent) END AS 'iCalendarContent'
		FROM @MetricValues
		GROUP BY ROLLUP(CampusName, ScheduleName)
		) y
) AS [campusSchedule]
CROSS JOIN
(
SELECT DISTINCT ColumnName, ColumnOrder, ServantMinister FROM (
	SELECT CASE GROUPING(ColumnName) WHEN 1 THEN 'Total' ELSE Max(ColumnName) END AS 'ColumnName'
	,CASE GROUPING(ColumnName)  WHEN 1 THEN 1 ELSE 0 END AS 'ColumnOrder'
	,MIN(ServantMinister) AS 'ServantMinister'	
	 FROM @MetricValues
	 GROUP BY ROLLUP(ColumnName)
	 ) x
) AS [column]
LEFT JOIN
(
	SELECT CASE GROUPING(CampusName) WHEN 1 THEN 'Total' ELSE MAX(CampusName) END AS 'Campus'
		,CASE GROUPING(ScheduleName) WHEN 1 THEN 'Total' ELSE MAX(ScheduleName) END AS 'ServiceTime'
		,CASE GROUPING(ColumnName) WHEN 1 THEN 'Total' ELSE MAX(ColumnName) END AS 'ColumnName'
		,SUM(Attendance) AS 'Attendance'
		FROM @MetricValues
		GROUP BY CUBE(CampusName, ScheduleName, ColumnName)
) AS [attendance] 
ON attendance.Campus=campusSchedule.CampusName 
AND attendance.ServiceTime = campusSchedule.ScheduleName
AND attendance.ColumnName = [column].ColumnName
ORDER BY IsCampusTotal DESC, CampusName, IsScheduleTotal, ServiceDayOrder, [Time],  iCalendarContent, ColumnOrder DESC, ServantMinister, [column].ColumnName;
END
GO


