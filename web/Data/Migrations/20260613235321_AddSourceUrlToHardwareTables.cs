using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceUrlToHardwareTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "video_card",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "storage",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "power_supply",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "motherboard",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "memory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "cpu_cooler",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "cpu",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "case_enclosure",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "text", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchScore = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_articles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_articles_Url",
                table: "product_articles",
                column: "Url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_articles");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "video_card");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "storage");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "power_supply");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "motherboard");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "memory");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "cpu_cooler");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "cpu");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "case_enclosure");
        }
    }
}
