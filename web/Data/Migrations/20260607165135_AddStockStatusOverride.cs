using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockStatusOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "video_card",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "storage",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "power_supply",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "motherboard",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "memory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "cpu_cooler",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "cpu",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockStatusOverride",
                table: "case_enclosure",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "video_card");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "storage");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "power_supply");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "motherboard");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "memory");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "cpu_cooler");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "cpu");

            migrationBuilder.DropColumn(
                name: "StockStatusOverride",
                table: "case_enclosure");
        }
    }
}
