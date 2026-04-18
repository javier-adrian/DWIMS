using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class addFieldsToProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Field_Processes_ProcessId",
                table: "Field");

            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Field_FieldId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Response_Steps_StepId",
                table: "Response");

            migrationBuilder.DropForeignKey(
                name: "FK_Response_Submissions_SubmissionId",
                table: "Response");

            migrationBuilder.DropForeignKey(
                name: "FK_Response_Users_ReviewerId",
                table: "Response");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Departments_DepartmentId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Field_FieldId",
                table: "Steps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Response",
                table: "Response");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Field",
                table: "Field");

            migrationBuilder.RenameTable(
                name: "Response",
                newName: "Responses");

            migrationBuilder.RenameTable(
                name: "Field",
                newName: "Fields");

            migrationBuilder.RenameIndex(
                name: "IX_Response_SubmissionId",
                table: "Responses",
                newName: "IX_Responses_SubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_Response_StepId",
                table: "Responses",
                newName: "IX_Responses_StepId");

            migrationBuilder.RenameIndex(
                name: "IX_Response_ReviewerId",
                table: "Responses",
                newName: "IX_Responses_ReviewerId");

            migrationBuilder.RenameIndex(
                name: "IX_Field_ProcessId",
                table: "Fields",
                newName: "IX_Fields_ProcessId");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "Steps",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReviewerId",
                table: "Responses",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Responses",
                table: "Responses",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fields",
                table: "Fields",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Fields_Processes_ProcessId",
                table: "Fields",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Fields_FieldId",
                table: "Inputs",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_Steps_StepId",
                table: "Responses",
                column: "StepId",
                principalTable: "Steps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_Submissions_SubmissionId",
                table: "Responses",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Responses_Users_ReviewerId",
                table: "Responses",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Departments_DepartmentId",
                table: "Steps",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Fields_FieldId",
                table: "Steps",
                column: "FieldId",
                principalTable: "Fields",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fields_Processes_ProcessId",
                table: "Fields");

            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Fields_FieldId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_Steps_StepId",
                table: "Responses");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_Submissions_SubmissionId",
                table: "Responses");

            migrationBuilder.DropForeignKey(
                name: "FK_Responses_Users_ReviewerId",
                table: "Responses");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Departments_DepartmentId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Fields_FieldId",
                table: "Steps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Responses",
                table: "Responses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fields",
                table: "Fields");

            migrationBuilder.RenameTable(
                name: "Responses",
                newName: "Response");

            migrationBuilder.RenameTable(
                name: "Fields",
                newName: "Field");

            migrationBuilder.RenameIndex(
                name: "IX_Responses_SubmissionId",
                table: "Response",
                newName: "IX_Response_SubmissionId");

            migrationBuilder.RenameIndex(
                name: "IX_Responses_StepId",
                table: "Response",
                newName: "IX_Response_StepId");

            migrationBuilder.RenameIndex(
                name: "IX_Responses_ReviewerId",
                table: "Response",
                newName: "IX_Response_ReviewerId");

            migrationBuilder.RenameIndex(
                name: "IX_Fields_ProcessId",
                table: "Field",
                newName: "IX_Field_ProcessId");

            migrationBuilder.AlterColumn<Guid>(
                name: "DepartmentId",
                table: "Steps",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "ReviewerId",
                table: "Response",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Response",
                table: "Response",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Field",
                table: "Field",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Field_Processes_ProcessId",
                table: "Field",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Field_FieldId",
                table: "Inputs",
                column: "FieldId",
                principalTable: "Field",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Response_Steps_StepId",
                table: "Response",
                column: "StepId",
                principalTable: "Steps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Response_Submissions_SubmissionId",
                table: "Response",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Response_Users_ReviewerId",
                table: "Response",
                column: "ReviewerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Departments_DepartmentId",
                table: "Steps",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Field_FieldId",
                table: "Steps",
                column: "FieldId",
                principalTable: "Field",
                principalColumn: "Id");
        }
    }
}
