using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Departments_DepartmentId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Roles_RoleId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Processes_Documents_DocumentId",
                table: "Processes");

            migrationBuilder.DropIndex(
                name: "IX_Processes_DocumentId",
                table: "Processes");

            migrationBuilder.DropIndex(
                name: "IX_Documents_DepartmentId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_RoleId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "SubmittedOn",
                table: "Responses",
                newName: "ActivatedOn");

            migrationBuilder.AlterColumn<int>(
                name: "Result",
                table: "Responses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedOn",
                table: "Responses",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessId",
                table: "Documents",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ProcessId",
                table: "Documents",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Processes_ProcessId",
                table: "Documents",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Processes_ProcessId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ProcessId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "ActivatedOn",
                table: "Responses",
                newName: "SubmittedOn");

            migrationBuilder.AlterColumn<int>(
                name: "Result",
                table: "Responses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedOn",
                table: "Responses",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "Processes",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Documents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Documents",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Processes_DocumentId",
                table: "Processes",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DepartmentId",
                table: "Documents",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RoleId",
                table: "Documents",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Departments_DepartmentId",
                table: "Documents",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Roles_RoleId",
                table: "Documents",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Processes_Documents_DocumentId",
                table: "Processes",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
