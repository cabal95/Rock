/*
<doc>
	<summary>
 		This function returns a table of notes tied to attendance metrics
	</summary>

	<returns>
		* GroupingRow
		* FirstColumnNotes
		* SecondColumnNotes
		* ThirdColumnNotes
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
	<param name="SourceTable" datatype="AttendanceMetricValueTableType">A table containing metric data used to get notes/param>
	<code>
	SELECT *
	FROM	[dbo].[_com_centralaz_unfMetrics_GetAttendanceNotes](
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
	@MetricValues) -- Gets the notes for the weekend
	</code>
</doc>
*/
ALTER FUNCTION [dbo].[_com_centralaz_unfMetrics_GetAttendanceNotes] 
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
	,FirstColumnNotes NVARCHAR(MAX)
	,SecondColumnNotes NVARCHAR(MAX)
	,ThirdColumnNotes NVARCHAR(MAX) )
AS
BEGIN
	-- This is a dummy iCalContent string used by the 'Total' rows so that when rows are sorted
	-- by iCalContent the 'Total' rows will appear on the bottom
	DECLARE @DummyTotalICalContent NVARCHAR(MAX) = 'DTSTART:20130501T235959
	RRULE:FREQ=WEEKLY;BYDAY=ZZ
	SEQUENCE:0';

	INSERT INTO @OutputTable	
	SELECT	referenceTable.GroupingRow AS 'GroupingRow'
			,CASE [firstColumn].Notes WHEN NULL THEN '' WHEN '' THEN '' ELSE SUBSTRING([firstColumn].Notes, 1, LEN([firstColumn].Notes) - 1 ) END AS 'FirstColumnNotes'
			,CASE secondColumn.Notes WHEN NULL THEN '' WHEN '' THEN '' ELSE SUBSTRING(secondColumn.Notes, 1, LEN(secondColumn.Notes) - 1 ) END AS 'SecondColumnNotes'
			,CASE thirdColumn.Notes WHEN NULL THEN '' WHEN '' THEN '' ELSE SUBSTRING(thirdColumn.Notes, 1, LEN(thirdColumn.Notes) - 1 ) END AS 'ThirdColumnNotes'
	
	FROM 
	-- This is the table we grab the row names AND orders from
	(
		SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(GroupingData) END AS 'GroupingRow'
		FROM @ReferenceTable
		GROUP BY ROLLUP(GroupingData)
	) AS referenceTable
	-- This Week Notes
	LEFT JOIN 
	(
		SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(GroupingData) END AS 'GroupingRow'
		,
			( SELECT Note + ',' 
           FROM @SourceTable p2
          WHERE p2.GroupingData = p1.GroupingData
		  AND MetricValueDateTime >= @FirstColumnStartDate
		  AND MetricValueDateTime <= @FirstColumnEndDate
		  AND p2.Note IS NOT NULL
		  AND p2.Note != ''
          ORDER BY MetricValueDateTime
            FOR XML PATH('') ) AS Notes
		FROM @SourceTable p1
		WHERE MetricValueDateTime >= @FirstColumnStartDate
		  AND MetricValueDateTime <= @FirstColumnEndDate
		GROUP BY ROLLUP(GroupingData)
	) AS [firstColumn]
	ON referenceTable.GroupingRow = [firstColumn].GroupingRow

	-- Last Week Notes
	LEFT JOIN 
	(
		SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(MetricCategoryName) END AS 'GroupingRow'
		,
			( SELECT Note + ',' 
           FROM @SourceTable p2
          WHERE p2.GroupingData = p1.GroupingData
		  AND MetricValueDateTime >= @SecondColumnStartDate
		  AND MetricValueDateTime <= @SecondColumnEndDate
		  AND p2.Note IS NOT NULL
		  AND p2.Note != ''
          ORDER BY MetricValueDateTime
            FOR XML PATH('') ) AS Notes
		FROM @SourceTable p1
		WHERE MetricValueDateTime >= @SecondColumnStartDate
		  AND MetricValueDateTime <= @SecondColumnEndDate
		GROUP BY ROLLUP(GroupingData)
	) AS secondColumn
	ON referenceTable.GroupingRow = secondColumn.GroupingRow

	-- Last Year Notes
	LEFT JOIN 
	(
		SELECT CASE GROUPING(GroupingData) WHEN 1 THEN 'Total' ELSE MAX(MetricCategoryName) END AS 'GroupingRow'
		,
			( SELECT Note + ',' 
           FROM @SourceTable p2
          WHERE p2.GroupingData = p1.GroupingData
		  AND MetricValueDateTime >= @ThirdColumnStartDate
		  AND MetricValueDateTime <= @ThirdColumnEndDate
		  AND p2.Note IS NOT NULL
		  AND p2.Note != ''
          ORDER BY MetricValueDateTime
            FOR XML PATH('') ) AS Notes
		FROM @SourceTable p1
		WHERE MetricValueDateTime >= @ThirdColumnStartDate
		  AND MetricValueDateTime <= @ThirdColumnEndDate
		GROUP BY ROLLUP(GroupingData)
	) AS thirdColumn
	ON referenceTable.GroupingRow = thirdColumn.GroupingRow
    
    RETURN
END






GO

