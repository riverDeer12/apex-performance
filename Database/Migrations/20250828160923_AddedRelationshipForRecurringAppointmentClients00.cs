using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedRelationshipForRecurringAppointmentClients00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringAppointments_Clients_ClientId",
                table: "RecurringAppointments");

            migrationBuilder.CreateTable(
                name: "ClientRecurringAppointment",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecurringAppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientRecurringAppointment", x => new { x.ClientId, x.RecurringAppointmentId });
                    table.ForeignKey(
                        name: "FK_ClientRecurringAppointment_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientRecurringAppointment_RecurringAppointments_RecurringAppointmentId",
                        column: x => x.RecurringAppointmentId,
                        principalTable: "RecurringAppointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientRecurringAppointment_RecurringAppointmentId",
                table: "ClientRecurringAppointment",
                column: "RecurringAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringAppointments_Clients_ClientId",
                table: "RecurringAppointments",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringAppointments_Clients_ClientId",
                table: "RecurringAppointments");

            migrationBuilder.DropTable(
                name: "ClientRecurringAppointment");

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
