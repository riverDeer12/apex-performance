using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApexPerformance.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangedPermissions00 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("7133ccad-9e04-432c-970a-a45b2b6a86fe"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("1b5a4f8f-4478-4e66-b636-4b7f5c3cdb28"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("2875f239-3d11-452e-acdd-b8bff6a50970"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("2f54d3f8-4f36-45f9-914d-12231b9c7a77"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("4b60b7a5-53c3-4b43-9793-b0354a29dca1"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("6bfa4f27-39cd-4a23-9a90-503b7562de09"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("74253b8e-6b5d-48f4-becb-02cc909de4bc"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("b23e50c8-9e16-44b3-b7d4-b4e470306f87"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c02b2833-ded9-4e6b-af6a-74a06cb2ffe0"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("c9b1e9a7-8bb0-4c29-86db-cbb4389e6393"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d07047e2-9c97-4f86-bb69-46699f9935b1"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("d6fbb48f-5b7b-480c-8a12-117a8b5a68fd"));

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: new Guid("e8f80d47-e956-4e05-a91b-d51315e629fd"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("d2444856-56fc-48ac-a11d-416f66576625"), new DateTimeOffset(new DateTime(2025, 4, 22, 21, 53, 38, 857, DateTimeKind.Unspecified).AddTicks(590), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 4, 22, 21, 53, 38, 857, DateTimeKind.Unspecified).AddTicks(590), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 4, 22, 21, 53, 38, 857, DateTimeKind.Unspecified).AddTicks(540), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 4, 22, 21, 53, 38, 857, DateTimeKind.Unspecified).AddTicks(540), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 4, 22, 21, 53, 38, 857, DateTimeKind.Unspecified).AddTicks(70), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 4, 22, 21, 53, 38, 857, DateTimeKind.Unspecified).AddTicks(130), new TimeSpan(0, 2, 0, 0, 0)) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Administrators",
                keyColumn: "Id",
                keyValue: new Guid("d2444856-56fc-48ac-a11d-416f66576625"));

            migrationBuilder.InsertData(
                table: "Administrators",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "FirstName", "IsDeleted", "LastName", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[] { new Guid("7133ccad-9e04-432c-970a-a45b2b6a86fe"), new DateTimeOffset(new DateTime(2025, 4, 22, 21, 13, 25, 98, DateTimeKind.Unspecified).AddTicks(9580), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), null, "Super", false, "Admin", new DateTimeOffset(new DateTime(2025, 4, 22, 21, 13, 25, 98, DateTimeKind.Unspecified).AddTicks(9590), new TimeSpan(0, 2, 0, 0, 0)), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"), new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb") });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Category", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("1b5a4f8f-4478-4e66-b636-4b7f5c3cdb28"), "Contributors", "Allows deleting contributors.", "CanDeleteContributor" },
                    { new Guid("2875f239-3d11-452e-acdd-b8bff6a50970"), "Categories", "Allows creating recipe categories.", "CanCreateCategory" },
                    { new Guid("2f54d3f8-4f36-45f9-914d-12231b9c7a77"), "Contributors", "Allows updating existing contributors.", "CanUpdateContributor" },
                    { new Guid("4b60b7a5-53c3-4b43-9793-b0354a29dca1"), "Contributors", "Allows creating new contributors.", "CanCreateContributor" },
                    { new Guid("6bfa4f27-39cd-4a23-9a90-503b7562de09"), "Recipes", "Allows viewing recipes.", "CanGetRecipes" },
                    { new Guid("74253b8e-6b5d-48f4-becb-02cc909de4bc"), "Recipes", "Allows creating recipes.", "CanCreateRecipe" },
                    { new Guid("b23e50c8-9e16-44b3-b7d4-b4e470306f87"), "Recipes", "Allows deleting recipes.", "CanDeleteRecipe" },
                    { new Guid("c02b2833-ded9-4e6b-af6a-74a06cb2ffe0"), "Categories", "Allows deleting recipe category.", "CanDeleteCategory" },
                    { new Guid("c9b1e9a7-8bb0-4c29-86db-cbb4389e6393"), "Contributors", "Allows viewing contributors.", "CanGetContributors" },
                    { new Guid("d07047e2-9c97-4f86-bb69-46699f9935b1"), "Recipes", "Allows updating recipes.", "CanUpdateRecipe" },
                    { new Guid("d6fbb48f-5b7b-480c-8a12-117a8b5a68fd"), "Categories", "Allows viewing recipe categories.", "CanGetCategories" },
                    { new Guid("e8f80d47-e956-4e05-a91b-d51315e629fd"), "Categories", "Allows updating recipe category.", "CanUpdateCategory" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("69a4116d-b1bd-4f0b-b6a7-a13bb5eb639f"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 4, 22, 21, 13, 25, 98, DateTimeKind.Unspecified).AddTicks(9520), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 4, 22, 21, 13, 25, 98, DateTimeKind.Unspecified).AddTicks(9520), new TimeSpan(0, 2, 0, 0, 0)) });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("5604e898-cd94-476b-8b86-9aa3a87cc9bb"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTimeOffset(new DateTime(2025, 4, 22, 21, 13, 25, 98, DateTimeKind.Unspecified).AddTicks(8990), new TimeSpan(0, 2, 0, 0, 0)), new DateTimeOffset(new DateTime(2025, 4, 22, 21, 13, 25, 98, DateTimeKind.Unspecified).AddTicks(9050), new TimeSpan(0, 2, 0, 0, 0)) });
        }
    }
}
