using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SignatureHeight",
                table: "steps",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignaturePage",
                table: "steps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SignatureWidth",
                table: "steps",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SignatureX",
                table: "steps",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SignatureY",
                table: "steps",
                type: "double",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AcroFormKey",
                table: "fields",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "RectHeight",
                table: "fields",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RectPage",
                table: "fields",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RectWidth",
                table: "fields",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RectX",
                table: "fields",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RectY",
                table: "fields",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnalysisJson",
                table: "documents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MappingState",
                table: "documents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SignatureHeight",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "SignaturePage",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "SignatureWidth",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "SignatureX",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "SignatureY",
                table: "steps");

            migrationBuilder.DropColumn(
                name: "RectHeight",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "RectPage",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "RectWidth",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "RectX",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "RectY",
                table: "fields");

            migrationBuilder.DropColumn(
                name: "AnalysisJson",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "MappingState",
                table: "documents");

            migrationBuilder.UpdateData(
                table: "fields",
                keyColumn: "AcroFormKey",
                keyValue: null,
                column: "AcroFormKey",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "AcroFormKey",
                table: "fields",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
