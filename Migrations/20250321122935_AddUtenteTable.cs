using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class AddUtenteTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rimuovi la creazione della tabella 'Utenti' poiché esiste già nel database
            // migrationBuilder.CreateTable(
            //     name: "Utenti",
            //     columns: table => new
            //     {
            //         Id = table.Column<int>(type: "int", nullable: false)
            //             .Annotation("SqlServer:Identity", "1, 1"),
            //         Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
            //         PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
            //         Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
            //     },
            //     constraints: table =>
            //     {
            //         table.PrimaryKey("PK_Utenti", x => x.Id);
            //     });

            // Crea l'indice 'Username' se non esiste già
            migrationBuilder.CreateIndex(
                name: "IX_Utenti_Username",
                table: "Utenti",
                column: "Username",
                unique: true);

            // Rimuovi la creazione della colonna 'ImmagineUrl' nella tabella 'Prodotti'
            // migrationBuilder.AddColumn<string>(
            //     name: "ImmagineUrl",
            //     table: "Prodotti",
            //     type: "nvarchar(200)",
            //     maxLength: 200,
            //     nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rimuovi la tabella 'Utenti' se dovessi fare un rollback
            // migrationBuilder.DropTable(name: "Utenti");

            // Rimuovi la colonna 'ImmagineUrl' dalla tabella 'Prodotti' se dovessi fare un rollback
            // migrationBuilder.DropColumn(
            //     name: "ImmagineUrl",
            //     table: "Prodotti");
        }
    }
}
