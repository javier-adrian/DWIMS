using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveToGeneralRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Roles_RoleId",
                table: "Steps");

            migrationBuilder.DropIndex(
                name: "IX_Steps_RoleId",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Steps");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Steps",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Steps");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Steps",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Steps_RoleId",
                table: "Steps",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Roles_RoleId",
                table: "Steps",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
