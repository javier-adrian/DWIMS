using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionVerificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "submissions",
                type: "varchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
            
            migrationBuilder.Sql("UPDATE submissions SET VerificationCode = SUBSTRING(MD5(CONCAT(Id, 'dwims')), 1, 8)");

            migrationBuilder.CreateIndex(
                name: "IX_submissions_VerificationCode",
                table: "submissions",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_submissions_VerificationCode",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "submissions");
        }
    }
}
