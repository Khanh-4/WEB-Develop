using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardwareSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "case_enclosure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    FormFactorSupport = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxVGALength = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case_enclosure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cpu",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Socket = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CoreCount = table.Column<int>(type: "integer", nullable: false),
                    ThreadCount = table.Column<int>(type: "integer", nullable: false),
                    BaseClock = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    BoostClock = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    TDP = table.Column<int>(type: "integer", nullable: false),
                    ApproximatePerformance = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cpu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cpu_cooler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    SocketCompatibility = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxTDP = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cpu_cooler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "memory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Modules = table.Column<int>(type: "integer", nullable: false),
                    Speed = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "motherboard",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    SocketCompatibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FormFactor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MemoryCompatibility = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MemorySlots = table.Column<int>(type: "integer", nullable: false),
                    MaxMemoryCapacity = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motherboard", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "power_supply",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Wattage = table.Column<int>(type: "integer", nullable: false),
                    Efficiency = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Modular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_power_supply", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "storage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Interface = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReadSpeed = table.Column<int>(type: "integer", nullable: false),
                    WriteSpeed = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "video_card",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    VRAM = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false),
                    TDP = table.Column<int>(type: "integer", nullable: false),
                    ApproximatePerformance = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_video_card", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_enclosure");

            migrationBuilder.DropTable(
                name: "cpu");

            migrationBuilder.DropTable(
                name: "cpu_cooler");

            migrationBuilder.DropTable(
                name: "memory");

            migrationBuilder.DropTable(
                name: "motherboard");

            migrationBuilder.DropTable(
                name: "power_supply");

            migrationBuilder.DropTable(
                name: "storage");

            migrationBuilder.DropTable(
                name: "video_card");
        }
    }
}
