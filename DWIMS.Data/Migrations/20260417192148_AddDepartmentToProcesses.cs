using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Processes",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Processes_DepartmentId",
                table: "Processes",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Processes_Departments_DepartmentId",
                table: "Processes",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processes_Departments_DepartmentId",
                table: "Processes");

            migrationBuilder.DropIndex(
                name: "IX_Processes_DepartmentId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Processes");
        }
    }
}
