using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addeddeviceinfotodevicetokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppVersion",
                table: "DeviceTokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildNumber",
                table: "DeviceTokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "DeviceTokens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OsVersion",
                table: "DeviceTokens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppVersion",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "BuildNumber",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "DeviceModel",
                table: "DeviceTokens");

            migrationBuilder.DropColumn(
                name: "OsVersion",
                table: "DeviceTokens");
        }
    }
}
