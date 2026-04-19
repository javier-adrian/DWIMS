using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class StoreSignatureOnDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Link",
                table: "Signatures",
                newName: "MimeType");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Signatures",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "EncryptedBlob",
                table: "Signatures",
                type: "longblob",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "isCurrent",
                table: "Signatures",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "EncryptedBlob",
                table: "Signatures");

            migrationBuilder.DropColumn(
                name: "isCurrent",
                table: "Signatures");

            migrationBuilder.RenameColumn(
                name: "MimeType",
                table: "Signatures",
                newName: "Link");
        }
    }
}
