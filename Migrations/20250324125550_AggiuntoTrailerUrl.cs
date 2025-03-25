using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class AggiuntoTrailerUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrailerUrl",
                table: "Prodotti",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrailerUrl",
                table: "Prodotti");
        }
    }
}
