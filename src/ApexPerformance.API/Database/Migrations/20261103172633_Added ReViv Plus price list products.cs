using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddedReVivPluspricelistproducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Products agreed with the shop owner (e-mail of 3.11.2026.). Names and current prices
            // stay in Stripe; base prices are the prices on 2.5.2025. (food exception).
            var createdAt = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero);

            migrationBuilder.InsertData(
                table: "PriceListProducts",
                columns: new[]
                {
                    "Id", "StripeProductId", "Code", "Brand", "NetQuantity", "UnitOfMeasure", "Barcode",
                    "Category", "AnchorPrice", "SortOrder", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy",
                    "IsDeleted", "DeletedAt"
                },
                values: new object[,]
                {
                    {
                        new Guid("6b1d3a52-3f0e-4c8e-9a51-2f6f0b6c6001"), "prod_TJtDu45campD2B", "002", "Wehealth",
                        60m, "kom", null, "Dodaci prehrani", 48.80m, 0, createdAt, Guid.Empty, createdAt,
                        Guid.Empty, false, null
                    },
                    {
                        new Guid("6b1d3a52-3f0e-4c8e-9a51-2f6f0b6c6002"), "prod_TJtERAoefR0NUO", "001", "Wehealth",
                        30m, "kom", null, "Dodaci prehrani", 32.15m, 1, createdAt, Guid.Empty, createdAt,
                        Guid.Empty, false, null
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PriceListProducts",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("6b1d3a52-3f0e-4c8e-9a51-2f6f0b6c6001"),
                    new Guid("6b1d3a52-3f0e-4c8e-9a51-2f6f0b6c6002")
                });
        }
    }
}
