USE [RockDB_Sync]
GO

/****** Object:  UserDefinedFunction [dbo].[_com_centralaz_unfMetrics_GetAttendanceData]    Script Date: 4/18/2018 3:36:48 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/*
<doc>
	<summary>
 		This function returns a table of notes tied to attendance metrics
	</summary>

	<returns>
		* GroupingRow
		* iCalendarContent
		* RowOrder
		* CategoryOrder
		* FirstColumnAttendance
		* SecondColumnAttendance
		* ThirdColumnAttendance
		* Growth
		* YtdGrowth
	</returns>
	<param name="FirstColumnStartDate" datatype="datetime">The start datetime for filtering attendance in the first column</param>
	<param name="FirstColumnEndDate" datatype="datetime">The end datetime for filtering attendance in the first column</param>
	<param name="SecondColumnStartDate" datatype="datetime">The start datetime for filtering attendance in the second column</param>
	<param name="SecondColumnEndDate" datatype="datetime">The end datetime for filtering attendance in the second column</param>
	<param name="ThirdColumnStartDate" datatype="datetime">The start datetime for filtering attendance in the third column</param>
	<param name="ThirdColumnEndDate" datatype="datetime">The end datetime for filtering attendance in the third column</param>
	<param name="IsHoliday" datatype="bit">A boolean specifying whether this table is for holiday metrics</param>
	<param name="CurrentMetrics" datatype="MetricKeyStringTableType">A table containing a list of keystrings for metrics that have already been entered for the 'current period'</param>
	<param name="ReferenceTable" datatype="AttendanceMetricValueTableType">A table containing metric data used to get grouping names and orders</param>
	<param name="SourceTable" datatype="AttendanceMetricValueTableType">A table containing metric data used to get attendance data/param>
	<code>
		SELECT GroupingRow, FirstColumnAttendance, SecondColumnAttendance, ThirdColumnAttendance, Growth
			FROM [dbo].[_com_centralaz_unfMetrics_GetAttendanceData](
			@FirstColumnStart,
			@FirstColumnEnd,
			@SecondColumnStart,
			@SecondColumnEnd,
			@ThirdColumnStart,
			@ThirdColumnEnd,
			@IsHoliday,
			@IsServicesOngoing,
			@CurrentMetrics,
			@MetricValues,
			@StudentsMetricValues) -- Gets the metrics for the students category
	</code>
</doc>
*/
ALTER FUNCTION [dbo].[_com_centralaz_unfMetrics_GetAttendanceData] 
( 
    @FirstColumnStartDate datetime,
	@FirstColumnEndDate datetime,
	@SecondColumnStartDate datetime,
	@SecondColumnEndDate datetime,
	@ThirdColumnStartDate datetime,
	@ThirdColumnEndDate datetime,
	@IsHoliday bit,
	@IsServicesOngoing bit,
	@CurrentMetrics MetricKeyStringTableType READONLY,
	@ReferenceTable AttendanceMetricValueTableType READONLY,
	@SourceTable AttendanceMetricValueTableType READONLY
)
RETURNS @OutputTable table ( 
	GroupingRow nvarchar(50)
	,iCalendarContent NVARCHAR(MAX)
	,RowOrder INT
	,CategoryOrder INT
	,FirstColumnAttendance NVARCHAR(MAX)
	,SecondColumnAttendance NVARCHAR(MAX)
	,ThirdColumnAttendance NVARCHAR(MAX)
	,Growth NVARCHAR(MAX)
	,YtdGrowth NVARCHAR(MAX) )
AS
BEGIN
	-- This is a dummy iCalContent string used by the 'Total' rows so that when rows are sorted
	-- by iCalContent the 'Total' rows will appear on the bottom
	DECLARE @DummyTotalICalContent NVARCHAR(MAX) = 'DTSTART:20130501T235959
	RRULE:FREQ=WEEKLY;BYDAY=ZZ
	SEQUENCE:0';

	INSERT INTO @OutputTable
	SELECT	GroupingRow,
			iCalendarContent,
			RowOrder,
			CategoryOrder,
			CASE WHEN FirstColumnAttendance IS NULL THEN ' ' ELSE FirstColumnAttendance END,
			CASE WHEN SecondColumnAttendance IS NULL THEN ' ' ELSE SecondColumnAttendance END,
			CASE WHEN ThirdColumnAttendance IS NULL THEN ' ' ELSE ThirdColumnAttendance END,
			Growth,
			YtdGrowth
	FROM (	
		SELECT	referenceTable.GroupingRow AS 'GroupingRow',
				referenceTable.iCalendarContent AS 'iCalendarContent',
				referenceTable.[Order] AS 'RowOrder',
				referenceTable.CategoryOrder AS 'CategoryOrder',
				FORMAT([firstColumn].Attendance,'N0') AS 'FirstColumnAttendance',
				FORMAT(secondColumn.Attendance,'N0') AS 'SecondColumnAttendance',
				FORMAT(thirdColumn.Attendance,'N0') AS 'ThirdColumnAttendance'
				--The growth between this time period AND last year
				, CASE 
					WHEN (thirdColumn.Attendance != 0 AND thirdColumn.Attendance IS NOT NULL AND @IsHoliday = 0) THEN ([firstColumn].Attendance - thirdColumn.Attendance) / thirdColumn.Attendance
					WHEN (secondColumn.Attendance != 0 AND secondColumn.Attendance IS NOT NULL AND @IsHoliday = 1) THEN ([firstColumn].Attendance - secondColumn.Attendance) / secondColumn.Attendance
					ELSE NULL END AS 'Growth'
				--The growth between this year to date AND last year to date
				, CASE 
					WHEN (thirdColumn.Attendance != 0 AND thirdColumn.Attendance IS NOT NULL) THEN (secondColumn.Attendance - thirdColumn.Attendance) / thirdColumn.Attendance
					ELSE NULL END AS 'YtdGrowth'
		FROM 
		-- This is the table we grab the row names AND orders from
		(
			SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(GroupingData) END AS 'GroupingRow'
			,CASE GROUPING(GroupingData) WHEN 1 THEN @DummyTotalICalContent ELSE MAX(ScheduleICalendarContent) END AS 'iCalendarContent'
			,CASE GROUPING(GroupingData) WHEN 1 THEN 1 ELSE 0 END AS 'Order'
			,CASE GROUPING(GroupingData) WHEN 1 THEN MAX(MetricCategoryOrder)+1 ELSE MAX(MetricCategoryOrder) END AS 'CategoryOrder'
			FROM @ReferenceTable
			GROUP BY ROLLUP(GroupingData)
		) AS referenceTable
		-- First Column Attendance
		LEFT JOIN 
		(
			SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(GroupingData) END AS 'GroupingRow'
			,SUM(Attendance) AS 'Attendance'
			FROM @SourceTable
			WHERE MetricValueDateTime >= @FirstColumnStartDate 
			AND MetricValueDateTime <= @FirstColumnEndDate
			GROUP BY ROLLUP(GroupingData)
		) AS [firstColumn]
		ON referenceTable.GroupingRow = [firstColumn].GroupingRow
		-- Last Period Attendance
		LEFT JOIN  
		(
			SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(GroupingData) END AS 'GroupingRow'
			,SUM(Attendance) AS 'Attendance'
			FROM @SourceTable			
			WHERE MetricValueDateTime >= @SecondColumnStartDate 
			AND MetricValueDateTime <= @SecondColumnEndDate
			AND (@IsServicesOngoing = 0 OR MetricKeyString IN (SELECT *FROM @CurrentMetrics))
			GROUP BY ROLLUP(GroupingData)
		) AS secondColumn
		ON referenceTable.GroupingRow = secondColumn.GroupingRow
		-- Second Last Period Attendance
		LEFT JOIN 
		(
			SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(GroupingData) END AS 'GroupingRow'
			,SUM(Attendance) AS 'Attendance'
			FROM @SourceTable
			WHERE MetricValueDateTime >= @ThirdColumnStartDate 
			AND MetricValueDateTime <= @ThirdColumnEndDate
			AND (@IsServicesOngoing = 0 OR MetricKeyString IN (SELECT *FROM @CurrentMetrics))
			GROUP BY ROLLUP(GroupingData)
		) AS thirdColumn
		ON referenceTable.GroupingRow = thirdColumn.GroupingRow
	) dataTable
	WHERE FirstColumnAttendance IS NOT NULL
	OR SecondColumnAttendance IS NOT NULL
	OR ThirdColumnAttendance IS NOT NULL
    
    RETURN
END





GO

