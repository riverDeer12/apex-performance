using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class BodyMeasurementsPermissions00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("ea41ccb7-ba3d-4ae6-83f4-5ce1eb2e63ea"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("92dfd50d-c21f-42bd-867d-d45c2992250e"), new DateTimeOffset(new DateTime(2025, 5, 29, 1, 17, 0, 467, DateTimeKind.Unspecified).AddTicks(10), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 5, 29, 1, 17, 0, 467, DateTimeKind.Unspecified).AddTicks(20), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae"), "BodyMeasurements", "Allows viewing body measurements.", "CanGetBodyMeasurements" },
                    { new Guid("8b13e0cb-4c27-497f-bf13-b2101d8f0efb"), "BodyMeasurements", "Allows deleting body measurements.", "CanDeleteBodyMeasurement" },
                    { new Guid("a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59"), "BodyMeasurements", "Allows updating body measurements.", "CanUpdateBodyMeasurement" },
                    { new Guid("c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f"), "BodyMeasurements", "Allows creating body measurements.", "CanCreateBodyMeasurement" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 29, 1, 17, 0, 466, DateTimeKind.Unspecified).AddTicks(9940), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 29, 1, 17, 0, 466, DateTimeKind.Unspecified).AddTicks(9940), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 29, 1, 17, 0, 466, DateTimeKind.Unspecified).AddTicks(9440), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 29, 1, 17, 0, 466, DateTimeKind.Unspecified).AddTicks(9510), new TimeSpan(0, 2, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("92dfd50d-c21f-42bd-867d-d45c2992250e"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("8b13e0cb-4c27-497f-bf13-b2101d8f0efb"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("ea41ccb7-ba3d-4ae6-83f4-5ce1eb2e63ea"), new DateTimeOffset(new DateTime(2025, 5, 25, 23, 33, 26, 983, DateTimeKind.Unspecified).AddTicks(3850), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 5, 25, 23, 33, 26, 983, DateTimeKind.Unspecified).AddTicks(3860), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 25, 23, 33, 26, 983, DateTimeKind.Unspecified).AddTicks(3790), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 25, 23, 33, 26, 983, DateTimeKind.Unspecified).AddTicks(3790), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 25, 23, 33, 26, 983, DateTimeKind.Unspecified).AddTicks(3290), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 25, 23, 33, 26, 983, DateTimeKind.Unspecified).AddTicks(3350), new TimeSpan(0, 2, 0, 0, 0)) });
        }
    }
}
