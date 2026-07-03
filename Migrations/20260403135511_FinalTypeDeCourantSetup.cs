using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class FinalTypeDeCourantSetup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_TypeDeCourants_TypeDeCourantIdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Factures_TypeDeCourants_TypeDeCourantIdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Factures_TypeDeCourantIdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Clients_TypeDeCourantIdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TypeDeCourants");

            migrationBuilder.DropColumn(
                name: "TypeDeCourantIdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "TypeDeCourantIdTypeDeCourant",
                table: "Clients");

            migrationBuilder.CreateIndex(
                name: "IX_Factures_IdTypeDeCourant",
                table: "Factures",
                column: "IdTypeDeCourant");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_IdTypeDeCourant",
                table: "Clients",
                column: "IdTypeDeCourant");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_TypeDeCourants_IdTypeDeCourant",
                table: "Clients",
                column: "IdTypeDeCourant",
                principalTable: "TypeDeCourants",
                principalColumn: "IdTypeDeCourant");

            migrationBuilder.AddForeignKey(
                name: "FK_Factures_TypeDeCourants_IdTypeDeCourant",
                table: "Factures",
                column: "IdTypeDeCourant",
                principalTable: "TypeDeCourants",
                principalColumn: "IdTypeDeCourant");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_TypeDeCourants_IdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Factures_TypeDeCourants_IdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Factures_IdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Clients_IdTypeDeCourant",
                table: "Clients");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TypeDeCourants",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TypeDeCourantIdTypeDeCourant",
                table: "Factures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TypeDeCourantIdTypeDeCourant",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Factures_TypeDeCourantIdTypeDeCourant",
                table: "Factures",
                column: "TypeDeCourantIdTypeDeCourant");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TypeDeCourantIdTypeDeCourant",
                table: "Clients",
                column: "TypeDeCourantIdTypeDeCourant");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_TypeDeCourants_TypeDeCourantIdTypeDeCourant",
                table: "Clients",
                column: "TypeDeCourantIdTypeDeCourant",
                principalTable: "TypeDeCourants",
                principalColumn: "IdTypeDeCourant");

            migrationBuilder.AddForeignKey(
                name: "FK_Factures_TypeDeCourants_TypeDeCourantIdTypeDeCourant",
                table: "Factures",
                column: "TypeDeCourantIdTypeDeCourant",
                principalTable: "TypeDeCourants",
                principalColumn: "IdTypeDeCourant");
        }
    }
}
