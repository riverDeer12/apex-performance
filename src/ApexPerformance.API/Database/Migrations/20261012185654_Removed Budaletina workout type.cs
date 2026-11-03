using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <summary>
    /// Data cleanup: removes the "Budaletina" workout type, which was added by mistake.
    /// Workouts that had it keep all their other data, only the link to this type is removed.
    /// Names are JSON with translations, so any translation containing the word matches.
    /// </summary>
    public partial class RemovedBudaletinaworkouttype : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM [WorkoutWorkoutTypes]
                WHERE [WorkoutTypeId] IN (SELECT [Id] FROM [WorkoutTypes] WHERE [Name] LIKE N'%Budaletina%');

                DELETE FROM [WorkoutTypes]
                WHERE [Name] LIKE N'%Budaletina%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted data is not restored, the workout type was added by mistake.
        }
    }
}
