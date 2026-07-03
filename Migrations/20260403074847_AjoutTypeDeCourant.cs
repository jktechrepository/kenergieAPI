using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class AjoutTypeDeCourant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateTable(
                name: "TypeDeCourants",
                columns: table => new
                {
                    IdTypeDeCourant = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Libelle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeDeCourants", x => x.IdTypeDeCourant);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_TypeDeCourants_TypeDeCourantIdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Factures_TypeDeCourants_TypeDeCourantIdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropTable(
                name: "TypeDeCourants");

            migrationBuilder.DropIndex(
                name: "IX_Factures_TypeDeCourantIdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropIndex(
                name: "IX_Clients_TypeDeCourantIdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "TypeDeCourantIdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "TypeDeCourantIdTypeDeCourant",
                table: "Clients");
        }
    }
}
