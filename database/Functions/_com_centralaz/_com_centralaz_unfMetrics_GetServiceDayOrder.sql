/*
<doc>
	<summary>
 		This function returns an integer dictating row order based on a string representation of a schedule's day of week.
	</summary>

	<returns>
		An integer representing the day of week's order in the attendance tables
	</returns>
	<param name="DayOfWeekInitials" datatype="nvarchar(max)">A string containing the initials of the day of week a schedule reoccurs on.</param>
	<code>
	SELECT [dbo].[_com_centralaz_unfMetrics_GetServiceDayOrder](@DayOfWeekInitials)
	</code>
</doc>
*/
ALTER FUNCTION [dbo].[_com_centralaz_unfMetrics_GetServiceDayOrder](
	 @DayOfWeekInitials nvarchar(max)
	)

RETURNS int AS

BEGIN

	RETURN ( Select Case
	When @DayOfWeekInitials like '%MO%' Then 1
	When @DayOfWeekInitials like '%TU%' Then 2
	When @DayOfWeekInitials like '%WE%' Then 3
	When @DayOfWeekInitials like '%TH%' Then 4
	When @DayOfWeekInitials like '%FR%' Then 5
	When @DayOfWeekInitials like '%SA%' Then 6
	When @DayOfWeekInitials like '%SU%' Then 7
	Else 8 End )
END
GO

