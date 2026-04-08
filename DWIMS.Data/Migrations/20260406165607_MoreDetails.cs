using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoreDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_UserId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Done",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Submissions");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOn",
                table: "Submissions",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedOn",
                table: "Submissions",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SubmitterId",
                table: "Submissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Steps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InputId",
                table: "Steps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Processes",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "Required",
                table: "Inputs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubmissionId",
                table: "Inputs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Inputs",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Departments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Response",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SubmissionId = table.Column<int>(type: "int", nullable: false),
                    StepId = table.Column<int>(type: "int", nullable: false),
                    ReviewerId = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Response", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Response_Steps_StepId",
                        column: x => x.StepId,
                        principalTable: "Steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Response_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Response_Users_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SubmitterId",
                table: "Submissions",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_Steps_DepartmentId",
                table: "Steps",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Steps_InputId",
                table: "Steps",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_Inputs_SubmissionId",
                table: "Inputs",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Response_ReviewerId",
                table: "Response",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_Response_StepId",
                table: "Response",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_Response_SubmissionId",
                table: "Response",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Submissions_SubmissionId",
                table: "Inputs",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Departments_DepartmentId",
                table: "Steps",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Inputs_InputId",
                table: "Steps",
                column: "InputId",
                principalTable: "Inputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_SubmitterId",
                table: "Submissions",
                column: "SubmitterId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Submissions_SubmissionId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Departments_DepartmentId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Inputs_InputId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_SubmitterId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "Response");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_SubmitterId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Steps_DepartmentId",
                table: "Steps");

            migrationBuilder.DropIndex(
                name: "IX_Steps_InputId",
                table: "Steps");

            migrationBuilder.DropIndex(
                name: "IX_Inputs_SubmissionId",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "CompletedOn",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmittedOn",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "SubmitterId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "InputId",
                table: "Steps");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "Required",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Departments");

            migrationBuilder.AddColumn<bool>(
                name: "Done",
                table: "Submissions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Submissions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UserId",
                table: "Submissions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_UserId",
                table: "Submissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
