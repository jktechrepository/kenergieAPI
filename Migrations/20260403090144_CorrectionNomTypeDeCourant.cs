using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class CorrectionNomTypeDeCourant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdTypeDeCourant",
                table: "Factures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTypeDeCourant",
                table: "Clients",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdTypeDeCourant",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "IdTypeDeCourant",
                table: "Clients");
        }
    }
}
