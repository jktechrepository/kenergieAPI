using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    /// <summary>
    /// Migration manuelle alignée sur Scripts/production_add_module_flexpay.sql
    /// MigrationId: 20260714063200_AjoutModuleFlexPay
    /// </summary>
    public partial class AjoutModuleFlexPay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InfosPaiementSociete",
                columns: table => new
                {
                    IdInfoPaiementSociete = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CodeMarchand = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ApiToken = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActifMobileMoney = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ActifCarteBancaire = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateModification = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfosPaiementSociete", x => x.IdInfoPaiementSociete);
                    table.ForeignKey(
                        name: "FK_InfosPaiementSociete_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallbacksFlexPay",
                columns: table => new
                {
                    IdCallbackFlexPay = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    OrderNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeadersJson = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IpAddress = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TraiteAvecSucces = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MessageTraitement = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateReception = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallbacksFlexPay", x => x.IdCallbackFlexPay);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaiementsElectroniquesEnAttente",
                columns: table => new
                {
                    IdPaiementElectroniqueEnAttente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdClient = table.Column<int>(type: "int", nullable: false),
                    IdClientFacture = table.Column<int>(type: "int", nullable: true),
                    IdFacture = table.Column<int>(type: "int", nullable: true),
                    IdUtilisateur = table.Column<int>(type: "int", nullable: true),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevisePaiement = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Methode = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdPaiementFinalise = table.Column<int>(type: "int", nullable: true),
                    HoldExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateFinalisation = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    MessageErreur = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementsElectroniquesEnAttente", x => x.IdPaiementElectroniqueEnAttente);
                    table.ForeignKey(
                        name: "FK_PaiementElectronique_Societes",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaiementElectronique_Clients",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaiementElectronique_ClientFactures",
                        column: x => x.IdClientFacture,
                        principalTable: "ClientFactures",
                        principalColumn: "IdClientFacture",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaiementElectronique_Factures",
                        column: x => x.IdFacture,
                        principalTable: "Factures",
                        principalColumn: "IdFacture",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaiementElectronique_Paiements",
                        column: x => x.IdPaiementFinalise,
                        principalTable: "Paiements",
                        principalColumn: "IdPaiement",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TransactionsFlexPay",
                columns: table => new
                {
                    IdTransactionFlexPay = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdPaiementElectroniqueEnAttente = table.Column<int>(type: "int", nullable: false),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeFlexPay = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDevise = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreCallbacks = table.Column<int>(type: "int", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionsFlexPay", x => x.IdTransactionFlexPay);
                    table.ForeignKey(
                        name: "FK_TransactionFlexPay_Pending",
                        column: x => x.IdPaiementElectroniqueEnAttente,
                        principalTable: "PaiementsElectroniquesEnAttente",
                        principalColumn: "IdPaiementElectroniqueEnAttente",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PaiementHolds",
                columns: table => new
                {
                    IdPaiementHold = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    CleRessource = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telephone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdPaiementElectroniqueEnAttente = table.Column<int>(type: "int", nullable: true),
                    ExpireAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EstLibere = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementHolds", x => x.IdPaiementHold);
                    table.ForeignKey(
                        name: "FK_PaiementHold_Pending",
                        column: x => x.IdPaiementElectroniqueEnAttente,
                        principalTable: "PaiementsElectroniquesEnAttente",
                        principalColumn: "IdPaiementElectroniqueEnAttente",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InfosPaiementSociete_IdSociete",
                table: "InfosPaiementSociete",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "UX_PaiementElectronique_Reference",
                table: "PaiementsElectroniquesEnAttente",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaiementElectronique_OrderNumber",
                table: "PaiementsElectroniquesEnAttente",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementElectronique_Societe_Statut",
                table: "PaiementsElectroniquesEnAttente",
                columns: new[] { "IdSociete", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionFlexPay_OrderNumber",
                table: "TransactionsFlexPay",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CallbackFlexPay_OrderNumber",
                table: "CallbacksFlexPay",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CallbackFlexPay_DateReception",
                table: "CallbacksFlexPay",
                column: "DateReception");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementHold_Societe_Cle",
                table: "PaiementHolds",
                columns: new[] { "IdSociete", "CleRessource", "EstLibere" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TransactionsFlexPay");
            migrationBuilder.DropTable(name: "CallbacksFlexPay");
            migrationBuilder.DropTable(name: "PaiementHolds");
            migrationBuilder.DropTable(name: "PaiementsElectroniquesEnAttente");
            migrationBuilder.DropTable(name: "InfosPaiementSociete");
        }
    }
}
