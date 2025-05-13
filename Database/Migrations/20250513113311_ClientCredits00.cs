using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ClientCredits00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("98b3bf6e-eb92-47fc-993a-efc859904d79"));

            migrationBuilder.AddColumn<int>(
                name: "Credits",
                table: "Clients",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClientsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("e8becdb7-8009-4169-9455-eea019ffa281"), new DateTimeOffset(new DateTime(2025, 5, 13, 13, 33, 11, 698, DateTimeKind.Unspecified).AddTicks(5770), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 5, 13, 13, 33, 11, 698, DateTimeKind.Unspecified).AddTicks(5770), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 13, 13, 33, 11, 698, DateTimeKind.Unspecified).AddTicks(5710), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 13, 13, 33, 11, 698, DateTimeKind.Unspecified).AddTicks(5710), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 5, 13, 13, 33, 11, 698, DateTimeKind.Unspecified).AddTicks(5240), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 5, 13, 13, 33, 11, 698, DateTimeKind.Unspecified).AddTicks(5300), new TimeSpan(0, 2, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("e8becdb7-8009-4169-9455-eea019ffa281"));

            migrationBuilder.DropColumn(
                name: "Credits",
                table: "Clients")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "ClientsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("98b3bf6e-eb92-47fc-993a-efc859904d79"), new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(8340), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 5, 12, 13, 34, 44, 978, DateTimeKind.Unspecified).AddTicks(8350), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

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
    }
}
