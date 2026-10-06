using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedtrainingexercisesets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingExerciseSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Reps = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(6,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingExerciseSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingExerciseSets_TrainingExercises_TrainingExerciseId",
                        column: x => x.TrainingExerciseId,
                        principalTable: "TrainingExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingExerciseSets_TrainingExerciseId",
                table: "TrainingExerciseSets",
                column: "TrainingExerciseId");

            // Existing exercises keep their values: "Sets" becomes that many
            // sets with the same repetitions and weight (one set when not given).
            migrationBuilder.Sql(@"
                ;WITH Numbers AS (
                    SELECT TOP (50) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS N
                    FROM sys.all_objects
                )
                INSERT INTO TrainingExerciseSets (Id, TrainingExerciseId, [Order], Reps, Weight)
                SELECT NEWID(), e.Id, n.N, e.Reps, e.Weight
                FROM TrainingExercises e
                JOIN Numbers n ON n.N < CASE WHEN e.Sets IS NULL OR e.Sets < 1 THEN 1
                                            WHEN e.Sets > 50 THEN 50 ELSE e.Sets END
                WHERE e.Sets IS NOT NULL OR e.Reps IS NOT NULL OR e.Weight IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "Reps",
                table: "TrainingExercises");

            migrationBuilder.DropColumn(
                name: "Sets",
                table: "TrainingExercises");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "TrainingExercises");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingExerciseSets");

            migrationBuilder.AddColumn<string>(
                name: "Reps",
                table: "TrainingExercises",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sets",
                table: "TrainingExercises",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "TrainingExercises",
                type: "decimal(6,2)",
                nullable: true);
        }
    }
}
