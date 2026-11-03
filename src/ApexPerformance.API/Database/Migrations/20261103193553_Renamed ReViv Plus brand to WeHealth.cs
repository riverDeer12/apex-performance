using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenamedReVivPlusbrandtoWeHealth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Brand as the shop owner writes it in Stripe.
            migrationBuilder.Sql("UPDATE [PriceListProducts] SET [Brand] = N'WeHealth' WHERE [Brand] = N'Wehealth'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [PriceListProducts] SET [Brand] = N'Wehealth' WHERE [Brand] = N'WeHealth'");
        }
    }
}
