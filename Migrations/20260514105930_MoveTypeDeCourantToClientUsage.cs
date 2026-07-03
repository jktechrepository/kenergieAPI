using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class MoveTypeDeCourantToClientUsage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTypeDeCourant",
                table: "ClientUsages",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ClientUsages cu
INNER JOIN Clients c ON c.IdClient = cu.IdClient
SET cu.IdTypeDeCourant = c.IdTypeDeCourant
WHERE c.IdTypeDeCourant IS NOT NULL;
");

            migrationBuilder.CreateIndex(
                name: "IX_ClientUsage_IdTypeDeCourant",
                table: "ClientUsages",
                column: "IdTypeDeCourant");

            migrationBuilder.AddForeignKey(
                name: "FK_ClientUsages_TypeDeCourants_IdTypeDeCourant",
                table: "ClientUsages",
                column: "IdTypeDeCourant",
                principalTable: "TypeDeCourants",
                principalColumn: "IdTypeDeCourant",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_TypeDeCourants_IdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_IdTypeDeCourant",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IdTypeDeCourant",
                table: "Clients");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTypeDeCourant",
                table: "Clients",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE Clients c
INNER JOIN (
    SELECT IdClient, MIN(IdTypeDeCourant) AS IdTypeDeCourant
    FROM ClientUsages
    WHERE IdTypeDeCourant IS NOT NULL
    GROUP BY IdClient
) x ON x.IdClient = c.IdClient
SET c.IdTypeDeCourant = x.IdTypeDeCourant;
");

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

            migrationBuilder.DropForeignKey(
                name: "FK_ClientUsages_TypeDeCourants_IdTypeDeCourant",
                table: "ClientUsages");

            migrationBuilder.DropIndex(
                name: "IX_ClientUsage_IdTypeDeCourant",
                table: "ClientUsages");

            migrationBuilder.DropColumn(
                name: "IdTypeDeCourant",
                table: "ClientUsages");
        }
    }
}
