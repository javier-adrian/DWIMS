using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldPatterns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pattern",
                table: "fields",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PatternMessage",
                table: "fields",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pattern",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "PatternMessage",
                table: "fields");
        }
    }
}
