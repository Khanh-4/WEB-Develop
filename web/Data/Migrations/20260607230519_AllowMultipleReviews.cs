using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_reviews_UserId_Category_ComponentId",
                table: "product_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_UserId",
                table: "product_reviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_reviews_UserId",
                table: "product_reviews");

            migrationBuilder.CreateIndex(
                name: "IX_product_reviews_UserId_Category_ComponentId",
                table: "product_reviews",
                columns: new[] { "UserId", "Category", "ComponentId" },
                unique: true);
        }
    }
}
