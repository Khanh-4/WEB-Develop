using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComponentBenchmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "component_benchmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ComponentName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CinebenchR23Multi = table.Column<int>(type: "integer", nullable: true),
                    CinebenchR23Single = table.Column<int>(type: "integer", nullable: true),
                    FpsCs2_1080p = table.Column<int>(type: "integer", nullable: true),
                    FpsCs2_1440p = table.Column<int>(type: "integer", nullable: true),
                    FpsCyberpunk_1080p = table.Column<int>(type: "integer", nullable: true),
                    FpsCyberpunk_1440p = table.Column<int>(type: "integer", nullable: true),
                    FpsValorant_1080p = table.Column<int>(type: "integer", nullable: true),
                    FpsValorant_1440p = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_component_benchmarks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "component_benchmarks");
        }
    }
}
