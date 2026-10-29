using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedshopaccountssettingsalertsandvisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "UserSessions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ClientDiscountPercent",
                table: "ShopOrders",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerUserId",
                table: "ShopOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShopConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    ShippingPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    FreeShippingFrom = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    ClientDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    NotificationEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShopStockAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NotifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopStockAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopStockAlerts_ShopProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ShopProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopVisitDays",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopVisitDays", x => new { x.Date, x.Path });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_CustomerUserId",
                table: "ShopOrders",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStockAlerts_VariantId_NotifiedAt",
                table: "ShopStockAlerts",
                columns: new[] { "VariantId", "NotifiedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopConfigs");

            migrationBuilder.DropTable(
                name: "ShopStockAlerts");

            migrationBuilder.DropTable(
                name: "ShopVisitDays");

            migrationBuilder.DropIndex(
                name: "IX_ShopOrders_CustomerUserId",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "ClientDiscountPercent",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "CustomerUserId",
                table: "ShopOrders");
        }
    }
}
