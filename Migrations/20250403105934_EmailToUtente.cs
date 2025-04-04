using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppEF.Migrations
{
    /// <inheritdoc />
    public partial class EmailToUtente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Utenti",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Utenti",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpires",
                table: "Utenti",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeDestinatario",
                table: "Notifiche",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UtenteId",
                table: "Notifiche",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notifiche_UtenteId",
                table: "Notifiche",
                column: "UtenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifiche_Utenti_UtenteId",
                table: "Notifiche",
                column: "UtenteId",
                principalTable: "Utenti",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifiche_Utenti_UtenteId",
                table: "Notifiche");

            migrationBuilder.DropIndex(
                name: "IX_Notifiche_UtenteId",
                table: "Notifiche");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpires",
                table: "Utenti");

            migrationBuilder.DropColumn(
                name: "NomeDestinatario",
                table: "Notifiche");

            migrationBuilder.DropColumn(
                name: "UtenteId",
                table: "Notifiche");
        }
    }
}
