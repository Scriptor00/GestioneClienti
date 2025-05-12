using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clienti",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Cognome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataIscrizione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Attivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clienti", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "Prodotti",
                columns: table => new
                {
                    IdProdotto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeProdotto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Prezzo = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    Giacenza = table.Column<int>(type: "int", nullable: false),
                    DataInserimento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prodotti", x => x.IdProdotto);
                });

            migrationBuilder.CreateTable(
                name: "Ordini",
                columns: table => new
                {
                    IdOrdine = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    DataOrdine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotaleOrdine = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ordini", x => x.IdOrdine);
                    table.ForeignKey(
                        name: "FK_Ordini_Clienti_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Clienti",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DettagliOrdini",
                columns: table => new
                {
                    IdDettaglio = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdOrdine = table.Column<int>(type: "int", nullable: false),
                    IdProdotto = table.Column<int>(type: "int", nullable: false),
                    Quantita = table.Column<int>(type: "int", nullable: false),
                    PrezzoUnitario = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DettagliOrdini", x => x.IdDettaglio);
                    table.ForeignKey(
                        name: "FK_DettagliOrdini_Ordini_IdOrdine",
                        column: x => x.IdOrdine,
                        principalTable: "Ordini",
                        principalColumn: "IdOrdine",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DettagliOrdini_Prodotti_IdProdotto",
                        column: x => x.IdProdotto,
                        principalTable: "Prodotti",
                        principalColumn: "IdProdotto",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DettagliOrdini_IdOrdine",
                table: "DettagliOrdini",
                column: "IdOrdine");

            migrationBuilder.CreateIndex(
                name: "IX_DettagliOrdini_IdProdotto",
                table: "DettagliOrdini",
                column: "IdProdotto");

            migrationBuilder.CreateIndex(
                name: "IX_Ordini_IdCliente",
                table: "Ordini",
                column: "IdCliente");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DettagliOrdini");

            migrationBuilder.DropTable(
                name: "Ordini");

            migrationBuilder.DropTable(
                name: "Prodotti");

            migrationBuilder.DropTable(
                name: "Clienti");
        }
    }
}
