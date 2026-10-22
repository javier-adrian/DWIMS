using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceLink",
                table: "submissions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceTitle",
                table: "submissions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "SignatureHeight",
                table: "submissionroutes",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SignaturePage",
                table: "submissionroutes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SignatureWidth",
                table: "submissionroutes",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SignatureX",
                table: "submissionroutes",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SignatureY",
                table: "submissionroutes",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "processes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceLink",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "SourceTitle",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "SignatureHeight",
                table: "submissionroutes");

            migrationBuilder.DropColumn(
                name: "SignaturePage",
                table: "submissionroutes");

            migrationBuilder.DropColumn(
                name: "SignatureWidth",
                table: "submissionroutes");

            migrationBuilder.DropColumn(
                name: "SignatureX",
                table: "submissionroutes");

            migrationBuilder.DropColumn(
                name: "SignatureY",
                table: "submissionroutes");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "processes");
        }
    }
}
