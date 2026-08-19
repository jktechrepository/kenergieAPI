using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class PaiementElectroniqueCrossDevise : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDeviseFacture",
                table: "PaiementsElectroniquesEnAttente",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "PaiementsElectroniquesEnAttente",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantFacture",
                table: "PaiementsElectroniquesEnAttente",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantFactureDevisePrincipale",
                table: "PaiementsElectroniquesEnAttente",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxFactureVersDevisePrincipale",
                table: "PaiementsElectroniquesEnAttente",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxFactureVersPaiement",
                table: "PaiementsElectroniquesEnAttente",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeDeviseFacture",
                table: "Paiements",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantPayeDevisePaiement",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxFactureVersDevisePaiement",
                table: "Paiements",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE PaiementsElectroniquesEnAttente
                  SET MontantFacture = Montant,
                      CodeDeviseFacture = CodeDevisePaiement,
                      TauxFactureVersPaiement = 1
                  WHERE MontantFacture = 0
                    AND COALESCE(CodeDevisePaiement, '') <> '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeDeviseFacture",
                table: "PaiementsElectroniquesEnAttente");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "PaiementsElectroniquesEnAttente");

            migrationBuilder.DropColumn(
                name: "MontantFacture",
                table: "PaiementsElectroniquesEnAttente");

            migrationBuilder.DropColumn(
                name: "MontantFactureDevisePrincipale",
                table: "PaiementsElectroniquesEnAttente");

            migrationBuilder.DropColumn(
                name: "TauxFactureVersDevisePrincipale",
                table: "PaiementsElectroniquesEnAttente");

            migrationBuilder.DropColumn(
                name: "TauxFactureVersPaiement",
                table: "PaiementsElectroniquesEnAttente");

            migrationBuilder.DropColumn(
                name: "CodeDeviseFacture",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantPayeDevisePaiement",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "TauxFactureVersDevisePaiement",
                table: "Paiements");
        }
    }
}
