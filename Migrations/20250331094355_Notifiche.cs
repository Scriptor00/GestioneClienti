using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class Notifiche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifiche",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titolo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Messaggio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInvio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Letta = table.Column<bool>(type: "bit", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LinkAzione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdRiferimento = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifiche", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifiche");
        }
    }
}
