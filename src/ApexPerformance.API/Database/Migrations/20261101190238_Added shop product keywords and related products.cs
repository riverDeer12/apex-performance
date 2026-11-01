using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class Addedshopproductkeywordsandrelatedproducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // JSON lists (EF primitive collections): existing products start with empty ones.
            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "ShopProducts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "RelatedProductIds",
                table: "ShopProducts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "ShopProducts");

            migrationBuilder.DropColumn(
                name: "RelatedProductIds",
                table: "ShopProducts");
        }
    }
}
