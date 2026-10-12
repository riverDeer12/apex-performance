using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <summary>
    /// Data correction: workout types named "Gire" are renamed to the correct "Girje".
    /// Names and descriptions are JSON with translations, the word is replaced wherever it
    /// appears (case sensitive, so "Gire" and "gire" keep their capitalization).
    /// </summary>
    public partial class RenamedGireworkouttypestoGirje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A type that becomes the same as an existing "Girje" type is merged into it:
            // its workouts get the existing type and the duplicate is removed.
            migrationBuilder.Sql("""
                SELECT [Id] AS [OldId],
                       REPLACE(REPLACE([Name] COLLATE Latin1_General_CS_AS, N'Gire', N'Girje'), N'gire', N'girje') AS [NewName]
                INTO #RenamedTypes
                FROM [WorkoutTypes]
                WHERE [Name] COLLATE Latin1_General_CS_AS LIKE N'%Gire%'
                   OR [Name] COLLATE Latin1_General_CS_AS LIKE N'%gire%';

                SELECT r.[OldId], MIN(CAST(t.[Id] AS NVARCHAR(36))) AS [ExistingId]
                INTO #MergedTypes
                FROM #RenamedTypes r
                JOIN [WorkoutTypes] t ON t.[Name] COLLATE Latin1_General_CS_AS = r.[NewName] COLLATE Latin1_General_CS_AS
                    AND t.[Id] <> r.[OldId]
                GROUP BY r.[OldId];

                INSERT INTO [WorkoutWorkoutTypes] ([WorkoutId], [WorkoutTypeId])
                SELECT DISTINCT w.[WorkoutId], CAST(m.[ExistingId] AS UNIQUEIDENTIFIER)
                FROM [WorkoutWorkoutTypes] w
                JOIN #MergedTypes m ON w.[WorkoutTypeId] = m.[OldId]
                WHERE NOT EXISTS (
                    SELECT 1 FROM [WorkoutWorkoutTypes] x
                    WHERE x.[WorkoutId] = w.[WorkoutId] AND x.[WorkoutTypeId] = CAST(m.[ExistingId] AS UNIQUEIDENTIFIER));

                DELETE w FROM [WorkoutWorkoutTypes] w JOIN #MergedTypes m ON w.[WorkoutTypeId] = m.[OldId];

                DELETE FROM [WorkoutTypes] WHERE [Id] IN (SELECT [OldId] FROM #MergedTypes);

                UPDATE [WorkoutTypes]
                SET [Name] = REPLACE(REPLACE([Name] COLLATE Latin1_General_CS_AS, N'Gire', N'Girje'), N'gire', N'girje'),
                    [Description] = REPLACE(REPLACE([Description] COLLATE Latin1_General_CS_AS, N'Gire', N'Girje'), N'gire', N'girje')
                WHERE [Name] COLLATE Latin1_General_CS_AS LIKE N'%Gire%'
                   OR [Name] COLLATE Latin1_General_CS_AS LIKE N'%gire%'
                   OR [Description] COLLATE Latin1_General_CS_AS LIKE N'%Gire%'
                   OR [Description] COLLATE Latin1_General_CS_AS LIKE N'%gire%';

                DROP TABLE #MergedTypes;
                DROP TABLE #RenamedTypes;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [WorkoutTypes]
                SET [Name] = REPLACE(REPLACE([Name] COLLATE Latin1_General_CS_AS, N'Girje', N'Gire'), N'girje', N'gire'),
                    [Description] = REPLACE(REPLACE([Description] COLLATE Latin1_General_CS_AS, N'Girje', N'Gire'), N'girje', N'gire')
                WHERE [Name] COLLATE Latin1_General_CS_AS LIKE N'%irje%'
                   OR [Description] COLLATE Latin1_General_CS_AS LIKE N'%irje%';
                """);
        }
    }
}
