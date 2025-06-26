using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedCanceledStatus00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("a4a7de2e-c126-4a31-995b-c6b363775ad2"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("70b5c6cb-d2b4-49ae-9959-cd6450439f55"), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9828), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9832), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.InsertData(
                table: "AppointmentStatuses",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "Description", "IsDeleted", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("e2031af4-e2d7-440d-a88b-b7e09fff9805"), new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000"), null, "Canceled status.", false, "Canceled", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000000") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "Name" },
                values: new object[] { new Guid("b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d"), "Appointments", "Allows progressing appointment.", "CanProgressAppointment" });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9734), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9744), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(8966), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9038), new TimeSpan(0, 2, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("70b5c6cb-d2b4-49ae-9959-cd6450439f55"));

            migrationBuilder.DeleteData(
                table: "AppointmentStatuses",
                keyColumn: "Id",
                keyValue: new Guid("e2031af4-e2d7-440d-a88b-b7e09fff9805"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("a4a7de2e-c126-4a31-995b-c6b363775ad2"), new DateTimeOffset(new DateTime(2025, 6, 23, 14, 14, 55, 912, DateTimeKind.Unspecified).AddTicks(2840), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 6, 23, 14, 14, 55, 912, DateTimeKind.Unspecified).AddTicks(2840), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 23, 14, 14, 55, 912, DateTimeKind.Unspecified).AddTicks(2770), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 23, 14, 14, 55, 912, DateTimeKind.Unspecified).AddTicks(2770), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 23, 14, 14, 55, 912, DateTimeKind.Unspecified).AddTicks(2220), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 23, 14, 14, 55, 912, DateTimeKind.Unspecified).AddTicks(2270), new TimeSpan(0, 2, 0, 0, 0)) });
        }
    }
}
