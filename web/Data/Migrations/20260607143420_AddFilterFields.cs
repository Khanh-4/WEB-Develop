using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFilterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PsuFormFactor",
                table: "power_supply",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Chipset",
                table: "motherboard",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Profile",
                table: "memory",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CaseType",
                table: "case_enclosure",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RadiatorSupport",
                table: "case_enclosure",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PsuFormFactor",
                table: "power_supply");

            migrationBuilder.DropColumn(
                name: "Chipset",
                table: "motherboard");

            migrationBuilder.DropColumn(
                name: "Profile",
                table: "memory");

            migrationBuilder.DropColumn(
                name: "CaseType",
                table: "case_enclosure");

            migrationBuilder.DropColumn(
                name: "RadiatorSupport",
                table: "case_enclosure");
        }
    }
}
