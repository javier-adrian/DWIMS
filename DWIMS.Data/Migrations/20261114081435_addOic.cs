using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class addOic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OicActive",
                table: "users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OicActiveUntilUtc",
                table: "users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OicId",
                table: "users",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_users_OicId",
                table: "users",
                column: "OicId");

            migrationBuilder.AddForeignKey(
                name: "FK_users_users_OicId",
                table: "users",
                column: "OicId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_users_OicId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_OicId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OicActive",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OicActiveUntilUtc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OicId",
                table: "users");
        }
    }
}
