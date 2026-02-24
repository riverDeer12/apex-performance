using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Bugfixing02 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientRecurringAppointments",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringAppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRecurringAppointments", x => new { x.ClientId, x.RecurringAppointmentId });
                    table.ForeignKey(
                        name: "FK_ClientRecurringAppointments_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientRecurringAppointments_RecurringAppointments_RecurringAppointmentId",
                        column: x => x.RecurringAppointmentId,
                        principalTable: "RecurringAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            
            migrationBuilder.CreateIndex(
                name: "IX_ClientRecurringAppointments_RecurringAppointmentId",
                table: "ClientRecurringAppointments",
                column: "RecurringAppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachAppointments_Appointments_AppointmentId",
                table: "CoachAppointments");

            migrationBuilder.DropForeignKey(
                name: "FK_CoachAppointments_Coaches_CoachId",
                table: "CoachAppointments");

            migrationBuilder.DropForeignKey(
                name: "FK_RecurringAppointments_AppointmentTypes_AppointmentTypeId",
                table: "RecurringAppointments");

            migrationBuilder.DropTable(
                name: "ClientRecurringAppointments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CoachAppointments",
                table: "CoachAppointments");

            migrationBuilder.DropIndex(
                name: "IX_CoachAppointments_CoachId",
                table: "CoachAppointments");

            migrationBuilder.RenameColumn(
                name: "AppointmentTypeId",
                table: "RecurringAppointments",
                newName: "ClientId")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RecurringAppointmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.RenameIndex(
                name: "IX_RecurringAppointments_AppointmentTypeId",
                table: "RecurringAppointments",
                newName: "IX_RecurringAppointments_ClientId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CoachAppointments",
                table: "CoachAppointments",
                columns: new[] { "CoachId", "AppointmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachAppointments_AppointmentId",
                table: "CoachAppointments",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachAppointments_Appointments_AppointmentId",
                table: "CoachAppointments",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CoachAppointments_Coaches_CoachId",
                table: "CoachAppointments",
                column: "CoachId",
                principalTable: "Coaches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringAppointments_Clients_ClientId",
                table: "RecurringAppointments",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
