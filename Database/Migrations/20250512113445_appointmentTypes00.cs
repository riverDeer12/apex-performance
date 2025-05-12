using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class appointmentTypes00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("31d62a6b-49fc-4bcd-91f4-e1c02f0fe737"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("98b3bf6e-eb92-47fc-993a-efc859904d79"), new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(8340), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(8350), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("4b6bc0dd-8bc8-42b1-ade2-c37ad49d34d0"), "AppointmentTypes", "Allows updating appointment type.", "CanUpdateAppointmentType" },
                    { new Guid("5591667e-38c2-4056-bfc6-f8644c31d30a"), "AppointmentTypes", "Allows viewing appointment types.", "CanGetAppointmentTypes" },
                    { new Guid("6df626b0-2937-4a3e-8f81-0f52375e85a5"), "AppointmentTypes", "Allows deleting appointment type.", "CanDeleteAppointmentType" },
                    { new Guid("9ecc7bdf-c144-4a6b-b10b-0885f09fb06b"), "AppointmentTypes", "Allows creating appointment type.", "CanCreateAppointmentType" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(8290), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(8300), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(7770), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(7840), new TimeSpan(0, 2, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("98b3bf6e-eb92-47fc-993a-efc859904d79"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4b6bc0dd-8bc8-42b1-ade2-c37ad49d34d0"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5591667e-38c2-4056-bfc6-f8644c31d30a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("6df626b0-2937-4a3e-8f81-0f52375e85a5"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("9ecc7bdf-c144-4a6b-b10b-0885f09fb06b"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("31d62a6b-49fc-4bcd-91f4-e1c02f0fe737"), new DateTimeOffset(new DateTime(2025, 5, 6, 12, 29, 38, 834, DateTimeKind.Unspecified).AddTicks(3800), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 5, 6, 12, 29, 38, 834, DateTimeKind.Unspecified).AddTicks(3810), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 6, 12, 29, 38, 834, DateTimeKind.Unspecified).AddTicks(3750), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 6, 12, 29, 38, 834, DateTimeKind.Unspecified).AddTicks(3750), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 6, 12, 29, 38, 834, DateTimeKind.Unspecified).AddTicks(3320), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 6, 12, 29, 38, 834, DateTimeKind.Unspecified).AddTicks(3380), new TimeSpan(0, 2, 0, 0, 0)) });
        }
    }
}
