using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DWIMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "fields",
                type: "int",
                nullable: false,
                defaultValue: 0);
            
            migrationBuilder.Sql("""
                                 UPDATE fields SET `Order` = CAST(SUBSTRING_INDEX(AcroFormKey, '-', 1) AS UNSIGNED)
                                 WHERE AcroFormKey REGEXP '^[0-9]+';
                                 """);
            
            migrationBuilder.Sql("""
                                 UPDATE fields SET `Order` = 999
                                 WHERE `Order` = 0;
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "fields");
        }
    }
}
