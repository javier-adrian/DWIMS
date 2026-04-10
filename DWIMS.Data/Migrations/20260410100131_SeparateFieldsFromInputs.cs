using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeparateFieldsFromInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Documents_DocumentId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Submissions_SubmissionId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Inputs_InputId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Processes_ProcessId",
                table: "Steps");

            migrationBuilder.DropIndex(
                name: "IX_Inputs_DocumentId",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "Required",
                table: "Inputs");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Inputs");

            migrationBuilder.RenameColumn(
                name: "InputId",
                table: "Steps",
                newName: "FieldId");

            migrationBuilder.RenameIndex(
                name: "IX_Steps_InputId",
                table: "Steps",
                newName: "IX_Steps_FieldId");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Inputs",
                newName: "FieldId");

            migrationBuilder.AlterColumn<int>(
                name: "ProcessId",
                table: "Steps",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SubmissionId",
                table: "Inputs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Field",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProcessId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Required = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedOn = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Field", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Field_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Inputs_FieldId",
                table: "Inputs",
                column: "FieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Field_ProcessId",
                table: "Field",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Field_FieldId",
                table: "Inputs",
                column: "FieldId",
                principalTable: "Field",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Submissions_SubmissionId",
                table: "Inputs",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Field_FieldId",
                table: "Steps",
                column: "FieldId",
                principalTable: "Field",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Processes_ProcessId",
                table: "Steps",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Field_FieldId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Inputs_Submissions_SubmissionId",
                table: "Inputs");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Field_FieldId",
                table: "Steps");

            migrationBuilder.DropForeignKey(
                name: "FK_Steps_Processes_ProcessId",
                table: "Steps");

            migrationBuilder.DropTable(
                name: "Field");

            migrationBuilder.DropIndex(
                name: "IX_Inputs_FieldId",
                table: "Inputs");

            migrationBuilder.RenameColumn(
                name: "FieldId",
                table: "Steps",
                newName: "InputId");

            migrationBuilder.RenameIndex(
                name: "IX_Steps_FieldId",
                table: "Steps",
                newName: "IX_Steps_InputId");

            migrationBuilder.RenameColumn(
                name: "FieldId",
                table: "Inputs",
                newName: "Type");

            migrationBuilder.AlterColumn<int>(
                name: "ProcessId",
                table: "Steps",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "SubmissionId",
                table: "Inputs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "DocumentId",
                table: "Inputs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Required",
                table: "Inputs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Inputs",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Inputs_DocumentId",
                table: "Inputs",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Documents_DocumentId",
                table: "Inputs",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Inputs_Submissions_SubmissionId",
                table: "Inputs",
                column: "SubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Inputs_InputId",
                table: "Steps",
                column: "InputId",
                principalTable: "Inputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Steps_Processes_ProcessId",
                table: "Steps",
                column: "ProcessId",
                principalTable: "Processes",
                principalColumn: "Id");
        }
    }
}
