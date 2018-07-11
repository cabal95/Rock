DECLARE @StartDate datetime = '2017-07-01'
DECLARE @Months int = 24
DECLARE @GrowthRate decimal(5,2) = .02

DECLARE @CampusTbl TABLE ( CampusGuid uniqueidentifier NOT NULL, Multiplier decimal(5,2) )
INSERT INTO @CampusTbl VALUES
    ( '0BF5BD9B-2D8E-4215-999C-EAA46165BD9F', 1 ),	--	Gilbert
    ( '76882AE3-1CE8-42A6-A2B6-8C0B29CF8CF8', .8 ),	--	Mesa
    ( '9329C09C-B41A-408F-9DEF-CE5A743D6CFC', .2 ),	--	Queen Creek
    ( '6A23A30B-A4DA-4240-9449-B5B227233B30', .2 ),	--	Glendale
    ( 'B450CE45-37E8-4295-9BF9-11F907714FFA', .1 )	--	Ahwatukee


DECLARE @MetricTbl TABLE ( MetricGuid uniqueidentifier NOT NULL, Goal decimal(18,2), [Value] decimal(18,2), StartGoalIsPercentage bit NOT NULL )
INSERT INTO @MetricTbl VALUES
	( 'BBF8148D-84A2-4FCD-8768-A154B951A986', 25, 0, 0 ),	--	Discover More Attendance
	( '2340CC55-FDF6-4F87-9013-E4918C3D83C7', 50, 0, 0 ),	--	Connection Cards (FTG)
	( '35CCF658-25AE-4DD5-88A4-83F3C3DDAAB2', 50, 0, 1 ),	--	Connection Card Conversion
	( '8E502D63-9485-4332-A412-94EAC686E91B', 50, 100, 1 ),	--	DM Room Capacity Utilization
	( '92BAE802-FA3C-41C2-A551-960A492B800E', 20, 0, 0 ),	--	Baptisms
	( '156C80A4-33CF-4E6D-920E-30FC56BE7801', 10, 0, 1 ),	--	New Servant Ministers
	( 'A22B3072-1A68-4034-A3E6-7B331894BC6E', 20, 0, 1 )	--	New Life Group Members

DECLARE @Month datetime = @StartDate
DECLARE @LastMonth datetime
DECLARE @EndDate datetime = DATEADD( m, @Months, @StartDate )
DECLARE @CampusEntityTypeId INT = ( SELECT TOP 1 Id FROM EntityType WHERE Name='Rock.Model.Campus' )

---- Delete existing values
DELETE P
FROM @MetricTbl T
INNER JOIN [Metric] M ON M.[Guid] = T.[MetricGuid]
INNER JOIN [MetricValue] V ON V.[MetricId] = M.[Id]
INNER JOIN [MetricValuePartition] P ON P.[MetricValueId] = V.[Id]

DELETE V
FROM @MetricTbl T
INNER JOIN [Metric] M ON M.[Guid] = T.[MetricGuid]
INNER JOIN [MetricValue] V ON V.[MetricId] = M.[Id]

DECLARE @CampusId int
DECLARE @CampusMultiplier decimal(5,2)
DECLARE @MetricId int
DECLARE @MetricValueId int
DECLARE @LastValue decimal(18,2)

WHILE @Month < @EndDate
BEGIN
	
	DECLARE CampusCursor CURSOR FOR
	SELECT C.[Id], CT.[Multiplier]
	FROM @CampusTbl CT
	INNER JOIN [Campus] C ON C.[Guid] = CT.[CampusGuid]
	
	OPEN CampusCursor
	FETCH NEXT FROM CampusCursor
	INTO @CampusId, @CampusMultiplier

	WHILE (@@FETCH_STATUS <> -1)
	BEGIN

		IF (@@FETCH_STATUS = 0)
		BEGIN

			DECLARE MetricCursor CURSOR FOR
			SELECT M.[Id] 
			FROM @MetricTbl MT
			INNER JOIN [Metric] M ON M.[Guid] = MT.[MetricGuid]
	
			OPEN MetricCursor
			FETCH NEXT FROM MetricCursor
			INTO @MetricId

			WHILE (@@FETCH_STATUS <> -1)
			BEGIN

				IF (@@FETCH_STATUS = 0)
				BEGIN

					SET @LastValue = ( 
						SELECT TOP 1 MV.[YValue]
						FROM [MetricPartition] MP
						INNER JOIN [MetricValuePartition] VP ON VP.[MetricPartitionId] = MP.[Id] AND VP.[EntityId] = @CampusId
						INNER JOIN [MetricValue] MV ON MV.[Id] = VP.[MetricValueId]
						WHERE MP.[MetricId] = @MetricId 
						AND MP.[EntityTypeId] = @CampusEntityTypeId 
						AND MV.[MetricValueDateTime] = @LastMonth
					)

					-- Create Goal
					INSERT INTO [MetricValue] ( [MetricValueType], [XValue], [YValue], [MetricId], [Note], [MetricValueDateTime], [Guid] )
					SELECT 1, '', 
						CASE WHEN MT.[StartGoalIsPercentage] = 1 
							THEN MT.[Goal]
							ELSE
								CASE WHEN @LastValue IS NULL 
									THEN CAST ( MT.[Goal] * @CampusMultiplier AS INT )
									ELSE CAST ( ( @LastValue + ( @LastValue * @GrowthRate ) ) AS INT )
								END
						END, M.[Id], '', @Month, NEWID()
					FROM [Metric] M
					INNER JOIN @MetricTbl MT ON MT.[MetricGuid] = M.[Guid]
					WHERE M.[Id] = @MetricId
					SET @MetricValueId = SCOPE_IDENTITY()

					-- Add Partition
					INSERT INTO [MetricValuePartition] ( [MetricPartitionId], [MetricValueId], [EntityId], [Guid] )
					SELECT MP.[Id], @MetricValueId, @CampusId, NEWID()
					FROM [Metric] M
					INNER JOIN [MetricPartition] MP ON MP.[MetricId] = M.[Id] AND MP.[EntityTypeId] = @CampusEntityTypeId
					INNER JOIN @MetricTbl MT ON MT.[MetricGuid] = M.[Guid]
					WHERE M.[Id] = @MetricId
		
					-- Insert Value
					INSERT INTO [MetricValue] ( [MetricValueType], [XValue], [YValue], [MetricId], [Note], [MetricValueDateTime], [Guid] )
					SELECT 0, '', MT.[Value] * @CampusMultiplier, M.[Id], '', @Month, NEWID()
					FROM [Metric] M
					INNER JOIN @MetricTbl MT ON MT.[MetricGuid] = M.[Guid] AND MT.[Value] > 0
					WHERE M.[Id] = @MetricId
					SET @MetricValueId = SCOPE_IDENTITY()

					-- Add Partition
					INSERT INTO [MetricValuePartition] ( [MetricPartitionId], [MetricValueId], [EntityId], [Guid] )
					SELECT MP.[Id], @MetricValueId, @CampusId, NEWID()
					FROM [Metric] M
					INNER JOIN [MetricPartition] MP ON MP.[MetricId] = M.[Id] AND MP.[EntityTypeId] = @CampusEntityTypeId
					INNER JOIN @MetricTbl MT ON MT.[MetricGuid] = M.[Guid] AND MT.[Value] > 0
					WHERE M.[Id] = @MetricId

					FETCH NEXT FROM MetricCursor
					INTO @MetricId

				END
	
			END

			CLOSE MetricCursor
			DEALLOCATE MetricCursor
		
		END
	
		FETCH NEXT FROM CampusCursor
		INTO @CampusId, @CampusMultiplier

	END

	CLOSE CampusCursor
	DEALLOCATE CampusCursor

	SET @LastMonth = @Month
	SET @Month = DATEADD( m, 1, @Month) 

END

