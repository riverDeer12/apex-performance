using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedAppointmentTypeToRecurrings00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentTypeId",
                table: "RecurringAppointments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RecurringAppointmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringAppointments_AppointmentTypeId",
                table: "RecurringAppointments",
                column: "AppointmentTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringAppointments_AppointmentTypes_AppointmentTypeId",
                table: "RecurringAppointments",
                column: "AppointmentTypeId",
                principalTable: "AppointmentTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringAppointments_AppointmentTypes_AppointmentTypeId",
                table: "RecurringAppointments");

            migrationBuilder.DropIndex(
                name: "IX_RecurringAppointments_AppointmentTypeId",
                table: "RecurringAppointments");

            migrationBuilder.DropColumn(
                name: "AppointmentTypeId",
                table: "RecurringAppointments")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RecurringAppointmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");
        }
    }
}
