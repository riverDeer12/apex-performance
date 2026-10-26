using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedworkoutcategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Workouts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "StrengthConditioning")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "WorkoutsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            // Workouts with a mobility or stretching type are mobility and flexibility,
            // the same keywords WorkoutPlanAccess uses; all others stay strength and conditioning.
            migrationBuilder.Sql("""
                UPDATE [Workouts] SET [Category] = N'MobilityFlexibility'
                WHERE EXISTS (
                    SELECT 1 FROM [WorkoutWorkoutTypes] AS r
                    INNER JOIN [WorkoutTypes] AS t ON t.[Id] = r.[WorkoutTypeId]
                    WHERE r.[WorkoutId] = [Workouts].[Id]
                      AND (t.[Name] LIKE N'%mobil%' OR t.[Name] LIKE N'%stretch%' OR t.[Name] LIKE N'%istez%'
                           OR t.[Name] LIKE N'%fleksib%' OR t.[Name] LIKE N'%flexib%' OR t.[Name] LIKE N'%allung%'))
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Workouts")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "WorkoutsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
