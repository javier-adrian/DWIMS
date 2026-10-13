using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssigneeToResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeId",
                table: "responses",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_responses_AssigneeId",
                table: "responses",
                column: "AssigneeId");

            migrationBuilder.AddForeignKey(
                name: "FK_responses_users_AssigneeId",
                table: "responses",
                column: "AssigneeId",
                principalTable: "users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_responses_users_AssigneeId",
                table: "responses");

            migrationBuilder.DropIndex(
                name: "IX_responses_AssigneeId",
                table: "responses");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "responses");
        }
    }
}
