using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kenergie.Migrations
{
    public partial class AjoutModuleDepense : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "CategorieDepenses",
                columns: table => new
                {
                    IdCategorieDepense = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    NomCategorie = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorieDepenses", x => x.IdCategorieDepense);
                    table.ForeignKey(
                        name: "FK_CategorieDepenses_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
                    ActifMobileMoney = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ActifCarteBancaire = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Statut = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                        name: "FK_PaiementsElectroniquesEnAttente_ClientFactures_IdClientFactu~",
                        column: x => x.IdClientFacture,
                        principalTable: "ClientFactures",
                        principalColumn: "IdClientFacture",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaiementsElectroniquesEnAttente_Clients_IdClient",
                        column: x => x.IdClient,
                        principalTable: "Clients",
                        principalColumn: "IdClient",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaiementsElectroniquesEnAttente_Factures_IdFacture",
                        column: x => x.IdFacture,
                        principalTable: "Factures",
                        principalColumn: "IdFacture",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaiementsElectroniquesEnAttente_Paiements_IdPaiementFinalise",
                        column: x => x.IdPaiementFinalise,
                        principalTable: "Paiements",
                        principalColumn: "IdPaiement",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PaiementsElectroniquesEnAttente_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Depenses",
                columns: table => new
                {
                    IdDepense = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    IdSociete = table.Column<int>(type: "int", nullable: false),
                    IdCategorieDepense = table.Column<int>(type: "int", nullable: true),
                    Libelle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Beneficiaire = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReferencePiece = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Montant = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CodeDeviseMontant = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CodeDevisePrincipale = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TauxVersDevisePrincipale = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    MontantDevisePrincipale = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ModePaiement = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateDepense = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Statut = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdUtilisateurCreateur = table.Column<int>(type: "int", nullable: false),
                    IdUtilisateurValidateur = table.Column<int>(type: "int", nullable: true),
                    DateValidation = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IdCabine = table.Column<int>(type: "int", nullable: true),
                    IdAxe = table.Column<int>(type: "int", nullable: true),
                    MotifAnnulation = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Depenses", x => x.IdDepense);
                    table.ForeignKey(
                        name: "FK_Depenses_Axes_IdAxe",
                        column: x => x.IdAxe,
                        principalTable: "Axes",
                        principalColumn: "IdAxe",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Depenses_Cabines_IdCabine",
                        column: x => x.IdCabine,
                        principalTable: "Cabines",
                        principalColumn: "IdCabine",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Depenses_CategorieDepenses_IdCategorieDepense",
                        column: x => x.IdCategorieDepense,
                        principalTable: "CategorieDepenses",
                        principalColumn: "IdCategorieDepense",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Depenses_Societes_IdSociete",
                        column: x => x.IdSociete,
                        principalTable: "Societes",
                        principalColumn: "IdSociete",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Depenses_Utilisateurs_IdUtilisateurCreateur",
                        column: x => x.IdUtilisateurCreateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Depenses_Utilisateurs_IdUtilisateurValidateur",
                        column: x => x.IdUtilisateurValidateur,
                        principalTable: "Utilisateurs",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.SetNull);
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
                        name: "FK_PaiementHolds_PaiementsElectroniquesEnAttente_IdPaiementElec~",
                        column: x => x.IdPaiementElectroniqueEnAttente,
                        principalTable: "PaiementsElectroniquesEnAttente",
                        principalColumn: "IdPaiementElectroniqueEnAttente",
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
                        name: "FK_TransactionsFlexPay_PaiementsElectroniquesEnAttente_IdPaieme~",
                        column: x => x.IdPaiementElectroniqueEnAttente,
                        principalTable: "PaiementsElectroniquesEnAttente",
                        principalColumn: "IdPaiementElectroniqueEnAttente",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CallbackFlexPay_DateReception",
                table: "CallbacksFlexPay",
                column: "DateReception");

            migrationBuilder.CreateIndex(
                name: "IX_CallbackFlexPay_OrderNumber",
                table: "CallbacksFlexPay",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CategorieDepense_Societe_Nom",
                table: "CategorieDepenses",
                columns: new[] { "IdSociete", "NomCategorie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Depense_Societe_Date",
                table: "Depenses",
                columns: new[] { "IdSociete", "DateDepense" });

            migrationBuilder.CreateIndex(
                name: "IX_Depense_Societe_Statut",
                table: "Depenses",
                columns: new[] { "IdSociete", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_Depense_UtilisateurCreateur",
                table: "Depenses",
                column: "IdUtilisateurCreateur");

            migrationBuilder.CreateIndex(
                name: "IX_Depenses_IdAxe",
                table: "Depenses",
                column: "IdAxe");

            migrationBuilder.CreateIndex(
                name: "IX_Depenses_IdCabine",
                table: "Depenses",
                column: "IdCabine");

            migrationBuilder.CreateIndex(
                name: "IX_Depenses_IdCategorieDepense",
                table: "Depenses",
                column: "IdCategorieDepense");

            migrationBuilder.CreateIndex(
                name: "IX_Depenses_IdUtilisateurValidateur",
                table: "Depenses",
                column: "IdUtilisateurValidateur");

            migrationBuilder.CreateIndex(
                name: "IX_InfosPaiementSociete_IdSociete",
                table: "InfosPaiementSociete",
                column: "IdSociete");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementHold_Societe_Cle",
                table: "PaiementHolds",
                columns: new[] { "IdSociete", "CleRessource", "EstLibere" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementHolds_IdPaiementElectroniqueEnAttente",
                table: "PaiementHolds",
                column: "IdPaiementElectroniqueEnAttente");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementElectronique_OrderNumber",
                table: "PaiementsElectroniquesEnAttente",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementElectronique_Societe_Statut",
                table: "PaiementsElectroniquesEnAttente",
                columns: new[] { "IdSociete", "Statut" });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsElectroniquesEnAttente_IdClient",
                table: "PaiementsElectroniquesEnAttente",
                column: "IdClient");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsElectroniquesEnAttente_IdClientFacture",
                table: "PaiementsElectroniquesEnAttente",
                column: "IdClientFacture");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsElectroniquesEnAttente_IdFacture",
                table: "PaiementsElectroniquesEnAttente",
                column: "IdFacture");

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsElectroniquesEnAttente_IdPaiementFinalise",
                table: "PaiementsElectroniquesEnAttente",
                column: "IdPaiementFinalise");

            migrationBuilder.CreateIndex(
                name: "UX_PaiementElectronique_Reference",
                table: "PaiementsElectroniquesEnAttente",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionFlexPay_OrderNumber",
                table: "TransactionsFlexPay",
                column: "OrderNumber");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionsFlexPay_IdPaiementElectroniqueEnAttente",
                table: "TransactionsFlexPay",
                column: "IdPaiementElectroniqueEnAttente");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallbacksFlexPay");

            migrationBuilder.DropTable(
                name: "Depenses");

            migrationBuilder.DropTable(
                name: "InfosPaiementSociete");

            migrationBuilder.DropTable(
                name: "PaiementHolds");

            migrationBuilder.DropTable(
                name: "TransactionsFlexPay");

            migrationBuilder.DropTable(
                name: "CategorieDepenses");

            migrationBuilder.DropTable(
                name: "PaiementsElectroniquesEnAttente");
        }
    }
}
