using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangedAppointmentTypesBase00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AppointmentTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "AppointmentTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "AppointmentTypes",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "AppointmentTypes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "AppointmentTypes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppointmentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "AppointmentTypes",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "AppointmentTypes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
