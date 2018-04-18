USE [RockDB_Test]
GO

/****** Object:  UserDefinedTableType [dbo].[MetricKeyStringTableType]    Script Date: 4/18/2018 3:04:28 PM ******/
DROP TYPE [dbo].[MetricKeyStringTableType]
GO

/****** Object:  UserDefinedTableType [dbo].[MetricKeyStringTableType]    Script Date: 4/18/2018 3:04:28 PM ******/
CREATE TYPE [dbo].[MetricKeyStringTableType] AS TABLE(
	[KeyString] [nvarchar](max) NULL
)
GO

