using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConditionFieldId",
                table: "steps",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "ConditionOperator",
                table: "steps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConditionValue",
                table: "steps",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionFieldId",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "ConditionOperator",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "ConditionValue",
                table: "steps");
        }
    }
}
