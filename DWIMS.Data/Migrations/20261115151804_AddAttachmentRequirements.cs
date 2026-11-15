using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequirementId",
                table: "attachments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "attachmentrequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProcessId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachmentrequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attachmentrequirements_processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_RequirementId",
                table: "attachments",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_attachmentrequirements_ProcessId",
                table: "attachmentrequirements",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_attachmentrequirements_RequirementId",
                table: "attachments",
                column: "RequirementId",
                principalTable: "attachmentrequirements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_attachmentrequirements_RequirementId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "attachmentrequirements");

            migrationBuilder.DropIndex(
                name: "IX_attachments_RequirementId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "RequirementId",
                table: "attachments");
        }
    }
}
