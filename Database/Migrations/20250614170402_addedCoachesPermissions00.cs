using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class addedCoachesPermissions00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("fc5d1d4c-2159-404c-92c1-b31965faf864"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("bffde944-25cf-4cfa-a8ba-59bf14b49843"), new DateTimeOffset(new DateTime(2025, 6, 14, 19, 4, 2, 221, DateTimeKind.Unspecified).AddTicks(9220), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 6, 14, 19, 4, 2, 221, DateTimeKind.Unspecified).AddTicks(9220), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22"), "Coaches", "Allows viewing coaches.", "CanGetCoaches" },
                    { new Guid("7aeb7c60-844b-4a38-b1ae-55829b8e5f3a"), "Coaches", "Allows updating coach.", "CanUpdateCoach" },
                    { new Guid("d8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d"), "Coaches", "Allows creating coach.", "CanCreateCoach" },
                    { new Guid("e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff"), "Coaches", "Allows deleting coach.", "CanDeleteCoach" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 14, 19, 4, 2, 221, DateTimeKind.Unspecified).AddTicks(9140), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 14, 19, 4, 2, 221, DateTimeKind.Unspecified).AddTicks(9140), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 14, 19, 4, 2, 221, DateTimeKind.Unspecified).AddTicks(8600), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 14, 19, 4, 2, 221, DateTimeKind.Unspecified).AddTicks(8670), new TimeSpan(0, 2, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("bffde944-25cf-4cfa-a8ba-59bf14b49843"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("7aeb7c60-844b-4a38-b1ae-55829b8e5f3a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("fc5d1d4c-2159-404c-92c1-b31965faf864"), new DateTimeOffset(new DateTime(2025, 6, 12, 23, 17, 50, 519, DateTimeKind.Unspecified).AddTicks(5510), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 6, 12, 23, 17, 50, 519, DateTimeKind.Unspecified).AddTicks(5510), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 12, 23, 17, 50, 519, DateTimeKind.Unspecified).AddTicks(5440), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 12, 23, 17, 50, 519, DateTimeKind.Unspecified).AddTicks(5450), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 12, 23, 17, 50, 519, DateTimeKind.Unspecified).AddTicks(4940), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 12, 23, 17, 50, 519, DateTimeKind.Unspecified).AddTicks(5000), new TimeSpan(0, 2, 0, 0, 0)) });
        }
    }
}
