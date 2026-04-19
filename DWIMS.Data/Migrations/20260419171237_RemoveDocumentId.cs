using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "Fields",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Fields_DocumentId",
                table: "Fields",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Documents_DocumentId",
                table: "Fields",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Documents_DocumentId",
                table: "Fields");

            migrationBuilder.DropIndex(
                name: "IX_Fields_DocumentId",
                table: "Fields");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "Fields");
        }
    }
}
