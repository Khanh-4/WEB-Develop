using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrebuiltPcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prebuilt_pcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    OldPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CpuId = table.Column<int>(type: "integer", nullable: true),
                    MotherboardId = table.Column<int>(type: "integer", nullable: true),
                    MemoryId = table.Column<int>(type: "integer", nullable: true),
                    VideoCardId = table.Column<int>(type: "integer", nullable: true),
                    StorageId = table.Column<int>(type: "integer", nullable: true),
                    PowerSupplyId = table.Column<int>(type: "integer", nullable: true),
                    CaseId = table.Column<int>(type: "integer", nullable: true),
                    CoolerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prebuilt_pcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_case_enclosure_CaseId",
                        column: x => x.CaseId,
                        principalTable: "case_enclosure",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_cpu_CpuId",
                        column: x => x.CpuId,
                        principalTable: "cpu",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_cpu_cooler_CoolerId",
                        column: x => x.CoolerId,
                        principalTable: "cpu_cooler",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_memory_MemoryId",
                        column: x => x.MemoryId,
                        principalTable: "memory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_motherboard_MotherboardId",
                        column: x => x.MotherboardId,
                        principalTable: "motherboard",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_power_supply_PowerSupplyId",
                        column: x => x.PowerSupplyId,
                        principalTable: "power_supply",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_storage_StorageId",
                        column: x => x.StorageId,
                        principalTable: "storage",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_prebuilt_pcs_video_card_VideoCardId",
                        column: x => x.VideoCardId,
                        principalTable: "video_card",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_CaseId",
                table: "prebuilt_pcs",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_CoolerId",
                table: "prebuilt_pcs",
                column: "CoolerId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_CpuId",
                table: "prebuilt_pcs",
                column: "CpuId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_MemoryId",
                table: "prebuilt_pcs",
                column: "MemoryId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_MotherboardId",
                table: "prebuilt_pcs",
                column: "MotherboardId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_PowerSupplyId",
                table: "prebuilt_pcs",
                column: "PowerSupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_StorageId",
                table: "prebuilt_pcs",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_prebuilt_pcs_VideoCardId",
                table: "prebuilt_pcs",
                column: "VideoCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prebuilt_pcs");
        }
    }
}
