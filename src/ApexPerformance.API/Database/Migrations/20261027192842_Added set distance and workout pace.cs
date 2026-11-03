using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedsetdistanceandworkoutpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaceType",
                table: "Workouts",
                type: "nvarchar(max)",
                nullable: true)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "WorkoutsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.AddColumn<int>(
                name: "DistanceMeters",
                table: "TrainingTemplateExerciseSets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistanceMeters",
                table: "TrainingExerciseSets",
                type: "int",
                nullable: true);

            // Workouts done over a distance, found by name in Croatian or English; coaches
            // change the rest in the workout form. Bikes first, so "air bike" isn't walking.
            migrationBuilder.Sql(@"
UPDATE [Workouts] SET [PaceType] = N'Kmh', [IsTimed] = 1
WHERE [PaceType] IS NULL AND ([Name] LIKE N'%bike%' OR [Name] LIKE N'%bicikl%' OR [Name] LIKE N'%cycling%');

UPDATE [Workouts] SET [PaceType] = N'Per500m', [IsTimed] = 1
WHERE [PaceType] IS NULL AND ([Name] LIKE N'%row erg%' OR [Name] LIKE N'%row-erg%' OR [Name] LIKE N'%rowerg%'
    OR [Name] LIKE N'%rowing machine%' OR [Name] LIKE N'%veslanje%' OR [Name] LIKE N'%vesla_k%'
    OR [Name] LIKE N'%ski erg%' OR [Name] LIKE N'%ski-erg%' OR [Name] LIKE N'%skierg%');

UPDATE [Workouts] SET [PaceType] = N'PerKm', [IsTimed] = 1
WHERE [PaceType] IS NULL
    AND ([Name] LIKE N'%hodanje%' OR [Name] LIKE N'%walking%' OR [Name] LIKE N'%tr_anje%'
        OR [Name] LIKE N'%running%' OR [Name] LIKE N'%treadmill%')
    AND [Name] NOT LIKE N'%lunge%' AND [Name] NOT LIKE N'%iskora%' AND [Name] NOT LIKE N'%farmer%'
    AND [Name] NOT LIKE N'%handstand%' AND [Name] NOT LIKE N'%na rukama%' AND [Name] NOT LIKE N'%plank%'
    AND [Name] NOT LIKE N'%u mjestu%' AND [Name] NOT LIKE N'%in place%' AND [Name] NOT LIKE N'%inchworm%'
    AND [Name] NOT LIKE N'%crab%' AND [Name] NOT LIKE N'%bear%' AND [Name] NOT LIKE N'%carry%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaceType",
                table: "Workouts")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "WorkoutsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropColumn(
                name: "DistanceMeters",
                table: "TrainingTemplateExerciseSets");

            migrationBuilder.DropColumn(
                name: "DistanceMeters",
                table: "TrainingExerciseSets");
        }
    }
}
