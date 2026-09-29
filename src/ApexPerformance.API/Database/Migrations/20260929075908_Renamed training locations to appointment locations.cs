using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Renamedtraininglocationstoappointmentlocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_TrainingLocations_TrainingLocationId",
                table: "Appointments");

            // Table is renamed (not dropped and created again)
            // so existing locations and links are kept.
            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainingLocations",
                table: "TrainingLocations");

            migrationBuilder.RenameTable(
                name: "TrainingLocations",
                newName: "AppointmentLocations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppointmentLocations",
                table: "AppointmentLocations",
                column: "Id");

            migrationBuilder.RenameColumn(
                name: "TrainingLocationId",
                table: "Appointments",
                newName: "AppointmentLocationId")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AppointmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_TrainingLocationId",
                table: "Appointments",
                newName: "IX_Appointments_AppointmentLocationId");

            migrationBuilder.UpdateData(
                table: "AppointmentLocations",
                keyColumn: "Id",
                keyValue: new Guid("b3f1c2a4-5d6e-4f70-8a91-2c3d4e5f6a7b"),
                column: "Name",
                value: "Legacy Gym Škurinje");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppointmentLocations_AppointmentLocationId",
                table: "Appointments",
                column: "AppointmentLocationId",
                principalTable: "AppointmentLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppointmentLocations_AppointmentLocationId",
                table: "Appointments");

            // Table is renamed (not dropped and created again)
            // so existing locations and links are kept.
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppointmentLocations",
                table: "AppointmentLocations");

            migrationBuilder.RenameTable(
                name: "AppointmentLocations",
                newName: "TrainingLocations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainingLocations",
                table: "TrainingLocations",
                column: "Id");

            migrationBuilder.RenameColumn(
                name: "AppointmentLocationId",
                table: "Appointments",
                newName: "TrainingLocationId")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "AppointmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_AppointmentLocationId",
                table: "Appointments",
                newName: "IX_Appointments_TrainingLocationId");

            migrationBuilder.UpdateData(
                table: "TrainingLocations",
                keyColumn: "Id",
                keyValue: new Guid("b3f1c2a4-5d6e-4f70-8a91-2c3d4e5f6a7b"),
                column: "Name",
                value: "Apex Performance Rijeka");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_TrainingLocations_TrainingLocationId",
                table: "Appointments",
                column: "TrainingLocationId",
                principalTable: "TrainingLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
