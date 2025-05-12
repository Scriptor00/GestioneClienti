using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class CarrelloItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carrello",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProdottoId = table.Column<int>(type: "int", nullable: false),
                    Quantita = table.Column<int>(type: "int", nullable: false),
                    DataAggiunta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carrello", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Carrello_Prodotti_ProdottoId",
                        column: x => x.ProdottoId,
                        principalTable: "Prodotti",
                        principalColumn: "IdProdotto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Carrello_ProdottoId",
                table: "Carrello",
                column: "ProdottoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Carrello");
        }
    }
}
