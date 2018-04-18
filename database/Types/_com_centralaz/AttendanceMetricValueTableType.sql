USE [RockDB_Test]
GO

/****** Object:  UserDefinedTableType [dbo].[AttendanceMetricValueTableType]    Script Date: 4/18/2018 3:04:42 PM ******/
DROP TYPE [dbo].[AttendanceMetricValueTableType]
GO

/****** Object:  UserDefinedTableType [dbo].[AttendanceMetricValueTableType]    Script Date: 4/18/2018 3:04:42 PM ******/
CREATE TYPE [dbo].[AttendanceMetricValueTableType] AS TABLE(
	[Id] [int] NULL,
	[MetricId] [int] NULL,
	[MetricCategoryId] [int] NULL,
	[MetricCategoryName] [nvarchar](50) NULL,
	[MetricCategoryOrder] [int] NULL,
	[Attendance] [float] NULL,
	[MetricValueDateTime] [datetime] NULL,
	[ScheduleId] [int] NULL,
	[ScheduleName] [nvarchar](50) NULL,
	[ScheduleICalendarContent] [nvarchar](max) NULL,
	[CampusId] [int] NULL,
	[CampusName] [nvarchar](50) NULL,
	[Note] [nvarchar](max) NULL,
	[MetricKeyString] [nvarchar](max) NULL,
	[GroupingData] [nvarchar](50) NULL
)
GO

