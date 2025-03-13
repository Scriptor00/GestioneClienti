using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class AddStatoOrdineEnumToOrdine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotaleOrdine",
                table: "Ordini",
                type: "DECIMAL(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(10,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Stato",
                table: "Ordini",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotaleOrdine",
                table: "Ordini",
                type: "DECIMAL(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(10,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Stato",
                table: "Ordini",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
