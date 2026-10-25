using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_processes_departments_DepartmentId",
                table: "processes");

            migrationBuilder.DropForeignKey(
                name: "FK_roles_departments_DepartmentId",
                table: "roles");

            migrationBuilder.DropForeignKey(
                name: "FK_steps_departments_DepartmentId",
                table: "steps");

            migrationBuilder.DropTable(
                name: "departmentuser");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropIndex(
                name: "IX_steps_DepartmentId",
                table: "steps");

            migrationBuilder.DropIndex(
                name: "IX_roles_DepartmentId",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_processes_DepartmentId",
                table: "processes");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "processes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "steps",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "roles",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "processes",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DeletedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "departmentuser",
                columns: table => new
                {
                    DepartmentsId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UsersId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departmentuser", x => new { x.DepartmentsId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_departmentuser_departments_DepartmentsId",
                        column: x => x.DepartmentsId,
                        principalTable: "departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_departmentuser_users_UsersId",
                        column: x => x.UsersId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_steps_DepartmentId",
                table: "steps",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_roles_DepartmentId",
                table: "roles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_processes_DepartmentId",
                table: "processes",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_departmentuser_UsersId",
                table: "departmentuser",
                column: "UsersId");

            migrationBuilder.AddForeignKey(
                name: "FK_processes_departments_DepartmentId",
                table: "processes",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_roles_departments_DepartmentId",
                table: "roles",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_steps_departments_DepartmentId",
                table: "steps",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
