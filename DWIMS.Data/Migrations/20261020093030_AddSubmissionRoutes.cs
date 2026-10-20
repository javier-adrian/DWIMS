using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "submissionroutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SubmissionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StepId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AssigneeId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submissionroutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_submissionroutes_steps_StepId",
                        column: x => x.StepId,
                        principalTable: "steps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_submissionroutes_submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_submissionroutes_users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_submissionroutes_AssigneeId",
                table: "submissionroutes",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_submissionroutes_SubmissionId_StepId",
                table: "submissionroutes",
                columns: new[] { "SubmissionId", "StepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_submissionroutes_SubmissionId",
                table: "submissionroutes",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "submissionroutes");
        }
    }
}
