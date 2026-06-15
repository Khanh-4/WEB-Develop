using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechSpecs.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnonymousShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_saved_builds_AspNetUsers_UserId",
                table: "saved_builds");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "saved_builds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "saved_builds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "saved_builds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_builds_AspNetUsers_UserId",
                table: "saved_builds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_saved_builds_AspNetUsers_UserId",
                table: "saved_builds");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "saved_builds");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "saved_builds");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "saved_builds",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_saved_builds_AspNetUsers_UserId",
                table: "saved_builds",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
