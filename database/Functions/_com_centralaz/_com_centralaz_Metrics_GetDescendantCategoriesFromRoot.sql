USE [RockDB_Test]
GO

/****** Object:  UserDefinedFunction [dbo].[_com_centralaz_Metrics_GetDescendantCategoriesFromRoot]    Script Date: 4/18/2018 3:01:18 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




/*
<doc>
	<summary>
 		This function recieves a category id and returns taht id 
		and the ids of all descendant categories
	</summary>
	<returns>
		* id
	</returns>
	<remarks>
		Used by the [dbo].[_com_centralaz_Metrics_GetAttendanceMetricData] stored procedure 
	</remarks>
	<code>
		SELECT * FROM [dbo].[_com_centralaz_Metrics_GetDescendantCategoriesFromRoot](435) 
	</code>
</doc>
*/
ALTER FUNCTION [dbo].[_com_centralaz_Metrics_GetDescendantCategoriesFromRoot] 
( 
    @Input int
)
RETURNS @OutputTable table ( [Id] int )
AS
BEGIN
	insert into @OutputTable select @Input;

	with tblWeekendServiceCategoryChild AS
	(
		SELECT *
			FROM Category WHERE ParentCategoryId = @Input
		UNION ALL
		SELECT Category.* FROM Category JOIN tblWeekendServiceCategoryChild  ON Category.ParentCategoryId = tblWeekendServiceCategoryChild.Id
	)
	insert into @OutputTable
	Select Id from tblWeekendServiceCategoryChild;
    
    RETURN
END



GO

