using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneralRole",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "GeneralRole",
                table: "Roles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneralRole",
                table: "Roles");

            migrationBuilder.AddColumn<int>(
                name: "GeneralRole",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
