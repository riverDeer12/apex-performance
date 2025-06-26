using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedCoachAppoitmentRelation00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("70b5c6cb-d2b4-49ae-9959-cd6450439f55"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("11c09349-6f32-4a43-a4b2-dbd58c244b1a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("17ef9141-208a-491a-9cb7-84d4f8375fb9"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("2070c041-997a-43d6-8a01-ba04c8f1b1ed"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("28ab969b-c866-470f-b3b4-7c1f1b066a48"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("2fe1ad9e-4229-411f-8095-e8f289777455"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3808be7c-782b-4fcf-8d2b-b9cd3a2bb8ee"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3a3f30a7-c0fc-43e7-aac0-861a53836479"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5a0c2c49-6e9a-4c45-a6db-d50f802816ef"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("5bdaa142-aae1-4225-8e8e-2539e74bd616"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("6a6c1fd6-8c28-49cf-8a71-91bead303a6f"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("718efa10-761a-4e44-8eda-eae6db4cb0a3"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("7aeb7c60-844b-4a38-b1ae-55829b8e5f3a"));

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
                keyValue: new Guid("a4e62d67-676d-4f53-9ace-b4c600ea9718"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b22c672f-6d69-4674-b9cd-5fbb8d497e7a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c1a4f730-9c92-47c0-97b0-9ce7e94fc20a"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d7775310-c4ff-4fd4-bf3b-719d85b60b4c"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("f250b493-7826-4a43-968f-d1392d925b96"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("f9fa3e15-8819-48f0-8751-02cf42e22a1d"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("fa6a2e89-cf1e-4e4c-bd3a-c95365c52f81"));

            migrationBuilder.CreateTable(
                name: "CoachAppointments",
                columns: table => new
                {
                    CoachId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachAppointments", x => new { x.CoachId, x.AppointmentId });
                    table.ForeignKey(
                        name: "FK_CoachAppointments_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CoachAppointments_Coaches_CoachId",
                        column: x => x.CoachId,
                        principalTable: "Coaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("d84ed40d-1c0d-4c5a-b590-132b8093ff95"), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 58, 57, 729, DateTimeKind.Unspecified).AddTicks(179), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 6, 26, 12, 58, 57, 729, DateTimeKind.Unspecified).AddTicks(186), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 26, 12, 58, 57, 729, DateTimeKind.Unspecified).AddTicks(28), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 58, 57, 729, DateTimeKind.Unspecified).AddTicks(43), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 6, 26, 12, 58, 57, 728, DateTimeKind.Unspecified).AddTicks(8924), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 58, 57, 728, DateTimeKind.Unspecified).AddTicks(9008), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_CoachAppointments_AppointmentId",
                table: "CoachAppointments",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachAppointments");

            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("d84ed40d-1c0d-4c5a-b590-132b8093ff95"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("70b5c6cb-d2b4-49ae-9959-cd6450439f55"), new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9828), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 6, 26, 12, 16, 34, 642, DateTimeKind.Unspecified).AddTicks(9832), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("11c09349-6f32-4a43-a4b2-dbd58c244b1a"), "Appointments", "Allows creating appointment.", "CanCreateAppointment" },
                    { new Guid("17ef9141-208a-491a-9cb7-84d4f8375fb9"), "Clients", "Allows viewing clients.", "CanGetClients" },
                    { new Guid("2070c041-997a-43d6-8a01-ba04c8f1b1ed"), "Administrators", "Allows deleting administrator.", "CanDeleteAdministrator" },
                    { new Guid("28ab969b-c866-470f-b3b4-7c1f1b066a48"), "Clients", "Allows creating client.", "CanCreateClient" },
                    { new Guid("2fe1ad9e-4229-411f-8095-e8f289777455"), "Users", "Allows updating user.", "CanUpdateUser" },
                    { new Guid("3808be7c-782b-4fcf-8d2b-b9cd3a2bb8ee"), "Administrators", "Allows viewing administrators.", "CanGetAdministrators" },
                    { new Guid("3a3f30a7-c0fc-43e7-aac0-861a53836479"), "Appointments", "Allows declining appointment.", "CanDeclineAppointment" },
                    { new Guid("3c9e9e34-7a7f-4e49-8f3d-2c7a1f3c6b22"), "Coaches", "Allows viewing coaches.", "CanGetCoaches" },
                    { new Guid("3f8c91de-49e7-4af7-b3f4-8d5c7ff1a9ae"), "BodyMeasurements", "Allows viewing body measurements.", "CanGetBodyMeasurements" },
                    { new Guid("5a0c2c49-6e9a-4c45-a6db-d50f802816ef"), "Appointments", "Allows approving appointment.", "CanApproveAppointment" },
                    { new Guid("5bdaa142-aae1-4225-8e8e-2539e74bd616"), "Administrators", "Allows updating administrator.", "CanUpdateAdministrator" },
                    { new Guid("6a6c1fd6-8c28-49cf-8a71-91bead303a6f"), "Appointments", "Allows deleting appointment.", "CanDeleteAppointment" },
                    { new Guid("718efa10-761a-4e44-8eda-eae6db4cb0a3"), "Clients", "Allows updating client.", "CanUpdateClient" },
                    { new Guid("7aeb7c60-844b-4a38-b1ae-55829b8e5f3a"), "Coaches", "Allows updating coach.", "CanUpdateCoach" },
                    { new Guid("8b13e0cb-4c27-497f-bf13-b2101d8f0efb"), "BodyMeasurements", "Allows deleting body measurements.", "CanDeleteBodyMeasurement" },
                    { new Guid("a412e56f-5d9c-4e1d-97e4-1c31f7aa2e59"), "BodyMeasurements", "Allows updating body measurements.", "CanUpdateBodyMeasurement" },
                    { new Guid("a4e62d67-676d-4f53-9ace-b4c600ea9718"), "Users", "Allows creating user.", "CanCreateUser" },
                    { new Guid("b22c672f-6d69-4674-b9cd-5fbb8d497e7a"), "Clients", "Allows deleting client.", "CanDeleteClient" },
                    { new Guid("b8f6cb70-ccf7-4a1c-8e6a-3f4a13c2735d"), "Appointments", "Allows progressing appointment.", "CanProgressAppointment" },
                    { new Guid("c1a4f730-9c92-47c0-97b0-9ce7e94fc20a"), "Appointments", "Allows viewing appointments.", "CanGetAppointments" },
                    { new Guid("c7a1738d-e20f-4931-a2f1-d3c0dcf64c5f"), "BodyMeasurements", "Allows creating body measurements.", "CanCreateBodyMeasurement" },
                    { new Guid("d7775310-c4ff-4fd4-bf3b-719d85b60b4c"), "Administrators", "Allows creating administrator.", "CanCreateAdministrator" },
                    { new Guid("d8e0e377-5f70-4f5b-a9bc-68a7b91c5a8d"), "Coaches", "Allows creating coach.", "CanCreateCoach" },
                    { new Guid("e3b4a1d2-f0fa-4d56-b349-8bb7b78f99ff"), "Coaches", "Allows deleting coach.", "CanDeleteCoach" },
                    { new Guid("f250b493-7826-4a43-968f-d1392d925b96"), "Users", "Allows deleting user.", "CanDeleteUser" },
                    { new Guid("f9fa3e15-8819-48f0-8751-02cf42e22a1d"), "Appointments", "Allows updating appointment.", "CanUpdateAppointment" },
                    { new Guid("fa6a2e89-cf1e-4e4c-bd3a-c95365c52f81"), "Users", "Allows viewing users.", "CanGetUsers" }
                });

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
    }
}
