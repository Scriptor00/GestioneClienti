using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class UltimaMigr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ordini_Clienti_IdCliente",
                table: "Ordini");

            migrationBuilder.AddForeignKey(
                name: "FK_Ordini_Clienti_IdCliente",
                table: "Ordini",
                column: "IdCliente",
                principalTable: "Clienti",
                principalColumn: "IdCliente",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ordini_Clienti_IdCliente",
                table: "Ordini");

            migrationBuilder.AddForeignKey(
                name: "FK_Ordini_Clienti_IdCliente",
                table: "Ordini",
                column: "IdCliente",
                principalTable: "Clienti",
                principalColumn: "IdCliente",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
