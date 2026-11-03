using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedshoprefundsdisputesandStripeevents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisputeStatus",
                table: "ShopOrders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisputedAt",
                table: "ShopOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasFraudWarning",
                table: "ShopOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "ShopOrders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundedAt",
                table: "ShopOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShopStripeEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopStripeEvents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopStripeEvents");

            migrationBuilder.DropColumn(
                name: "DisputeStatus",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "DisputedAt",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "HasFraudWarning",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "ShopOrders");
        }
    }
}
