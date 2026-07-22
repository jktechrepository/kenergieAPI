using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class AjoutModuleMultiDevise : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "Societes",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePaiement",
                table: "Paiements",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "Paiements",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantAPayeDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantPayeDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ResteAPayeDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxVersDevisePrincipale",
                table: "Paiements",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "Factures",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrix",
                table: "Factures",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantDevisePrincipale",
                table: "Factures",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxVersDevisePrincipale",
                table: "Factures",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrincipale",
                table: "ClientFactures",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CodeDevisePrix",
                table: "ClientFactures",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "MontantDevisePrincipale",
                table: "ClientFactures",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantDuDevisePrincipale",
                table: "ClientFactures",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontantPayeDevisePrincipale",
                table: "ClientFactures",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TauxVersDevisePrincipale",
                table: "ClientFactures",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DevisesMonetaires",
                columns: table => new
                {
                    IdDeviseMonetaire = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeDevise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Libelle = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbole = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevisesMonetaires", x => x.IdDeviseMonetaire);
                    table.ForeignKey(
                        name: "FK_DevisesMonetaires_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TauxChanges",
                columns: table => new
                {
                    IdTauxChange = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeDeviseSource = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDeviseCible = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Taux = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    DateEffet = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TauxChanges", x => x.IdTauxChange);
                    table.ForeignKey(
                        name: "FK_TauxChanges_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_DevisesMonetaires_Societe_Code",
                table: "DevisesMonetaires",
                columns: new[] { "IdSociete", "CodeDevise" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TauxChanges_Societe_Paired_DateEffet",
                table: "TauxChanges",
                columns: new[] { "IdSociete", "CodeDeviseSource", "CodeDeviseCible", "DateEffet" });

            // Seed CDF comme devise principale pour toutes les sociétés existantes
            migrationBuilder.Sql(@"
UPDATE Societes
SET CodeDevisePrincipale = 'CDF'
WHERE CodeDevisePrincipale IS NULL OR CodeDevisePrincipale = '';
");

            migrationBuilder.Sql(@"
INSERT INTO DevisesMonetaires (IdSociete, CodeDevise, Libelle, Symbole, Statut, DateCreation, DateModification)
SELECT s.IdSociete, 'CDF', 'Franc congolais', 'FC', 1, UTC_TIMESTAMP(6), NULL
FROM Societes s
WHERE NOT EXISTS (
    SELECT 1 FROM DevisesMonetaires d
    WHERE d.IdSociete = s.IdSociete AND d.CodeDevise = 'CDF'
);
");

            migrationBuilder.Sql(@"
UPDATE Factures
SET CodeDevisePrix = 'CDF',
    CodeDevisePrincipale = 'CDF',
    TauxVersDevisePrincipale = 1,
    MontantDevisePrincipale = Montant
WHERE CodeDevisePrix IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE ClientFactures
SET CodeDevisePrix = 'CDF',
    CodeDevisePrincipale = 'CDF',
    TauxVersDevisePrincipale = 1,
    MontantDevisePrincipale = Montant,
    MontantPayeDevisePrincipale = IFNULL(MontantPaye, 0),
    MontantDuDevisePrincipale = MontantDu
WHERE CodeDevisePrix IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE Paiements
SET CodeDevisePaiement = 'CDF',
    CodeDevisePrincipale = 'CDF',
    TauxVersDevisePrincipale = 1,
    MontantPayeDevisePrincipale = MontantPaye,
    MontantAPayeDevisePrincipale = MontantAPaye,
    ResteAPayeDevisePrincipale = ResteAPaye
WHERE CodeDevisePaiement IS NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevisesMonetaires");

            migrationBuilder.DropTable(
                name: "TauxChanges");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "Societes");

            migrationBuilder.DropColumn(
                name: "CodeDevisePaiement",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantAPayeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "MontantPayeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "ResteAPayeDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "TauxVersDevisePrincipale",
                table: "Paiements");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrix",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "MontantDevisePrincipale",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "TauxVersDevisePrincipale",
                table: "Factures");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrincipale",
                table: "ClientFactures");

            migrationBuilder.DropColumn(
                name: "CodeDevisePrix",
                table: "ClientFactures");

            migrationBuilder.DropColumn(
                name: "MontantDevisePrincipale",
                table: "ClientFactures");

            migrationBuilder.DropColumn(
                name: "MontantDuDevisePrincipale",
                table: "ClientFactures");

            migrationBuilder.DropColumn(
                name: "MontantPayeDevisePrincipale",
                table: "ClientFactures");

            migrationBuilder.DropColumn(
                name: "TauxVersDevisePrincipale",
                table: "ClientFactures");
        }
    }
}
