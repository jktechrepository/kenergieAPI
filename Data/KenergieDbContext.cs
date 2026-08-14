using Kenergie.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace Kenergie.Data
{
    public class KenergieDbContext : DbContext
    {
        public KenergieDbContext(DbContextOptions<KenergieDbContext> options)
            : base(options)
        {
        }

        // DbSets pour les modèles conservés
        public DbSet<Societe> Societes { get; set; }
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Agent> Agents { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<SmsLog> SmsLogs { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<CategorieClient> CategorieClients { get; set; }
        public DbSet<Cabine> Cabines { get; set; }
        public DbSet<Axe> Axes { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Usage> Usages { get; set; }
        public DbSet<ClientUsage> ClientUsages { get; set; }
        public DbSet<TypeDeCourant> TypeDeCourants { get; set; }
        public DbSet<Facture> Factures { get; set; }
        public DbSet<Paiement> Paiements { get; set; }
        public DbSet<ClientFacture> ClientFactures { get; set; }
        public DbSet<DeviseMonetaire> DevisesMonetaires { get; set; }
        public DbSet<TauxChange> TauxChanges { get; set; }
        public DbSet<DiffusionStatistique> DiffusionStatistiques { get; set; }
        public DbSet<PanneSignalement> PanneSignalements { get; set; }
        public DbSet<CommunicationCampaign> CommunicationCampaigns { get; set; }
        public DbSet<PlainteClient> PlainteClients { get; set; }
        public DbSet<ClientCrashed> ClientsCrashed { get; set; }
        public DbSet<ArriereeCrashed> ArriereesCrashed { get; set; }
        public DbSet<InfoPaiementSociete> InfosPaiementSociete { get; set; }
        public DbSet<PaiementElectroniqueEnAttente> PaiementsElectroniquesEnAttente { get; set; }
        public DbSet<TransactionFlexPay> TransactionsFlexPay { get; set; }
        public DbSet<CallbackFlexPay> CallbacksFlexPay { get; set; }
        public DbSet<PaiementHold> PaiementHolds { get; set; }
        public DbSet<CategorieDepense> CategorieDepenses { get; set; }
        public DbSet<Depense> Depenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration Utilisateur
            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Societe)
                .WithMany(e => e.Utilisateurs)
                .HasForeignKey(u => u.IdSociete)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Utilisateurs)
                .HasForeignKey(u => u.IdRole)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<Utilisateur>()
                .Property(u => u.IdRole)
                .IsRequired(false);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Agent)
                .WithMany(a => a.Utilisateurs)
                .HasForeignKey(u => u.IdAgent)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Utilisateur>()
                .HasOne(u => u.Client)
                .WithMany(c => c.Utilisateurs)
                .HasForeignKey(u => u.IdClient)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Index unique sur l'email
            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Utilisateurs_Email_Unique");

            // Configuration Agent
            modelBuilder.Entity<Agent>()
                .HasOne(a => a.Societe)
                .WithMany(e => e.Agents)
                .HasForeignKey(a => a.IdSociete)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Agent>(entity =>
            {
                entity.Property(e => e.Matricule).IsRequired(false);
                entity.Property(e => e.NomComplet).IsRequired(false);
                entity.Property(e => e.Genre).IsRequired(false);
                entity.Property(e => e.TelephoneAgent).IsRequired(false);
                entity.Property(e => e.EmailAgent).IsRequired(false);
                entity.Property(e => e.Statut).IsRequired(false);
                entity.Property(e => e.EtatCivil).IsRequired(false);
                entity.Property(e => e.SerialNumber).IsRequired(false);
                entity.Property(e => e.Fonction).IsRequired(false);
                entity.Property(e => e.RoleAgent).IsRequired(false);
                entity.Property(e => e.PhotoUrl).IsRequired(false);
                entity.Property(e => e.IdSociete).IsRequired(false);
                entity.Property(e => e.AdresseResidence).IsRequired(false);
                // Note: Les champs d'adresse structurés (Province, Ville, etc.) ont été supprimés
                // Agent utilise maintenant uniquement AdresseResidence
            });

            // Index unique sur le matricule Agent
            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.Matricule)
                .IsUnique()
                .HasDatabaseName("IX_Agents_Matricule_Unique");

            // Index unique sur l'email Agent
            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.EmailAgent)
                .IsUnique()
                .HasDatabaseName("IX_Agents_Email_Unique");

            // Index unique sur le SerialNumber Agent
            modelBuilder.Entity<Agent>()
                .HasIndex(a => a.SerialNumber)
                .IsUnique()
                .HasDatabaseName("IX_Agents_SerialNumber_Unique");

            // Configuration UserRole (Multi-rôles)
            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.IdUtilisateur, ur.IdRole })
                .IsUnique()
                .HasDatabaseName("IX_UserRole_Utilisateur_Role_Unique");

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Utilisateur)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.IdRole)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.IdUtilisateur)
                .HasDatabaseName("IX_UserRole_IdUtilisateur");

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => ur.IdRole)
                .HasDatabaseName("IX_UserRole_IdRole");

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.IdUtilisateur, ur.Statut })
                .HasDatabaseName("IX_UserRole_Utilisateur_Statut");

            // Configuration AuditLog
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.TableName, a.RecordId })
                .HasDatabaseName("IX_AuditLog_Table_Record");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_AuditLog_UserId");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.DateAction)
                .HasDatabaseName("IX_AuditLog_DateAction");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.IdSociete)
                .HasDatabaseName("IX_AuditLog_IdSociete");

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Action)
                .HasDatabaseName("IX_AuditLog_Action");

            // Configuration Notification
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Expediteur)
                .WithMany(u => u.NotificationsEnvoyees)
                .HasForeignKey(n => n.IdExpediteur)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Destinataire)
                .WithMany(u => u.NotificationsRecues)
                .HasForeignKey(n => n.IdDestinataire)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Societe)
                .WithMany(e => e.Notifications)
                .HasForeignKey(n => n.IdSociete)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Agent)
                .WithMany()
                .HasForeignKey(n => n.IdAgent)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction);

            // Configuration PasswordResetToken
            modelBuilder.Entity<PasswordResetToken>()
                .HasIndex(t => t.Token)
                .IsUnique();

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.Utilisateur)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(t => t.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration Role
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.Nom)
                .IsUnique();

            // Configuration RolePermission
            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions) // ✅ Spécifier la navigation property
                .HasForeignKey(rp => rp.IdRole)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions) // ✅ Spécifier la navigation property
                .HasForeignKey(rp => rp.IdPermission)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration UserPermission
            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Utilisateur)
                .WithMany()
                .HasForeignKey(up => up.IdUtilisateur)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPermission>()
                .HasOne(up => up.Permission)
                .WithMany()
                .HasForeignKey(up => up.IdPermission)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuration CategorieClient
            modelBuilder.Entity<CategorieClient>()
                .HasOne(c => c.Societe)
                .WithMany(s => s.CategorieClients)
                .HasForeignKey(c => c.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CategorieClient>()
                .HasIndex(c => new { c.NomCategorie, c.IdSociete })
                .HasDatabaseName("IX_CategorieClient_NomCategorie_IdSociete");

            // Configuration Cabine
            modelBuilder.Entity<Cabine>()
                .HasOne(c => c.Societe)
                .WithMany()
                .HasForeignKey(c => c.IdSociete)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cabine>()
                .HasIndex(c => c.IdSociete)
                .HasDatabaseName("IX_Cabine_IdSociete");

            // Configuration DateCreation pour Cabine avec valeur par défaut
            modelBuilder.Entity<Cabine>()
                .Property(c => c.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .IsRequired();

            // Configuration Statut pour Cabine avec valeur par défaut
            modelBuilder.Entity<Cabine>()
                .Property(c => c.Statut)
                .HasDefaultValue(true);

            // Configuration Axe
            modelBuilder.Entity<Axe>()
                .HasOne(a => a.Cabine)
                .WithMany()
                .HasForeignKey(a => a.IdCabine)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Axe>()
                .HasIndex(a => a.IdCabine)
                .HasDatabaseName("IX_Axe_IdCabine");

            // Configuration DateCreation pour Axe avec valeur par défaut
            modelBuilder.Entity<Axe>()
                .Property(a => a.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .IsRequired();

            // Configuration Statut pour Axe avec valeur par défaut
            modelBuilder.Entity<Axe>()
                .Property(a => a.Statut)
                .HasDefaultValue(true);

            // Configuration Client
            modelBuilder.Entity<Client>()
                .Property(c => c.Statut)
                .HasDefaultValue(true);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.Axe)
                .WithMany()
                .HasForeignKey(c => c.IdAxe)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.IdAxe)
                .HasDatabaseName("IX_Client_IdAxe");

            // ✨ Index unique sur CodeCons (seul champ unique pour Client)
            modelBuilder.Entity<Client>()
                .HasIndex(c => c.CodeCons)
                .IsUnique()
                .HasDatabaseName("IX_Client_CodeCons_Unique");

            // Configuration Usage (relation one-to-many avec CategorieClient)
            modelBuilder.Entity<Usage>()
                .HasOne(u => u.CategorieClient)
                .WithMany(cat => cat.Usages)
                .HasForeignKey(u => u.IdCategorieClient)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Usage>()
                .HasIndex(u => new { u.Libelle, u.IdCategorieClient })
                .HasDatabaseName("IX_Usage_Libelle_IdCategorieClient");

            // Configuration ClientUsage (relation many-to-many entre Client et Usage)
            modelBuilder.Entity<ClientUsage>()
                .HasKey(cu => cu.IdClientUsage);

            modelBuilder.Entity<ClientUsage>()
                .Property(cu => cu.Statut)
                .HasDefaultValue(true);

            modelBuilder.Entity<ClientUsage>()
                .HasOne(cu => cu.Client)
                .WithMany(c => c.ClientsUsages)
                .HasForeignKey(cu => cu.IdClient)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientUsage>()
                .HasOne(cu => cu.Usage)
                .WithMany(u => u.ClientsUsages)
                .HasForeignKey(cu => cu.IdUsage)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClientUsage>()
                .HasOne(cu => cu.TypeDeCourant)
                .WithMany(t => t.ClientUsages)
                .HasForeignKey(cu => cu.IdTypeDeCourant)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Index pour optimiser les requêtes
            modelBuilder.Entity<ClientUsage>()
                .HasIndex(cu => cu.IdClient)
                .HasDatabaseName("IX_ClientUsage_IdClient");

            modelBuilder.Entity<ClientUsage>()
                .HasIndex(cu => cu.IdUsage)
                .HasDatabaseName("IX_ClientUsage_IdUsage");

            // Contrainte unique pour éviter les doublons (même client, même usage)
            modelBuilder.Entity<ClientUsage>()
                .HasIndex(cu => new { cu.IdClient, cu.IdUsage })
                .IsUnique()
                .HasDatabaseName("IX_ClientUsage_Client_Usage_Unique");

            modelBuilder.Entity<ClientUsage>()
                .HasIndex(cu => cu.IdTypeDeCourant)
                .HasDatabaseName("IX_ClientUsage_IdTypeDeCourant");

            // ═══════════════════════════════════════════════════════════════
            // ✨ NOUVEAUX INDEX POUR LA SYNCHRONISATION
            // ═══════════════════════════════════════════════════════════════════

            // Index pour cursor pagination sur Clients (via relation indirecte)
            modelBuilder.Entity<Client>()
                .HasIndex(c => new { c.UpdatedAt, c.IdClient })
                .HasDatabaseName("IX_Clients_Sync");

            // Index pour cursor pagination sur ClientFactures (via relation indirecte)
            modelBuilder.Entity<ClientFacture>()
                .HasIndex(cf => new { cf.DateModification, cf.IdClientFacture })
                .HasDatabaseName("IX_ClientFactures_Sync");

            // Index unique pour idempotence des paiements (ClientRequestId seulement)
            modelBuilder.Entity<Paiement>()
                .HasIndex(p => new { p.ClientRequestId })
                .IsUnique()
                .HasDatabaseName("UX_Paiements_Idempotent");

            // Configuration Facture (relation avec Usage)
            modelBuilder.Entity<Facture>()
                .HasOne(f => f.Usage)
                .WithMany(u => u.Factures)
                .HasForeignKey(f => f.IdUsage)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Facture>()
                .HasIndex(f => f.NumeroFacture)
                .IsUnique()
                .HasDatabaseName("IX_Facture_NumeroFacture_Unique")
                .HasFilter("[numero_facture] IS NOT NULL");

            modelBuilder.Entity<Facture>()
                .Property(f => f.Statut)
                .HasDefaultValue(true);

            modelBuilder.Entity<Facture>()
                .HasIndex(f => new { f.MoisEmission, f.AnneesEmission, f.IdUsage })
                .HasDatabaseName("IX_Facture_Mois_Annee_Usage");

            // Configuration Paiement
            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Facture)
                .WithMany(f => f.Paiements)
                .HasForeignKey(p => p.IdFacture)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Client)
                .WithMany()
                .HasForeignKey(p => p.IdClient)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Paiement>()
                .HasOne(p => p.Utilisateur)
                .WithMany()
                .HasForeignKey(p => p.IdUtilisateur)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.IdFacture)
                .HasDatabaseName("IX_Paiements_IdFacture");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.IdClient)
                .HasDatabaseName("IX_Paiements_IdClient");

            modelBuilder.Entity<Paiement>()
                .HasIndex(p => p.DatePaiement)
                .HasDatabaseName("IX_Paiements_DatePaiement");

            // Configuration IsDeleted pour Paiement avec valeur par défaut
            modelBuilder.Entity<Paiement>()
                .Property(p => p.IsDeleted)
                .HasDefaultValue(false);

            // Configuration ClientFacture
            modelBuilder.Entity<ClientFacture>()
                .HasKey(cf => cf.IdClientFacture);

            modelBuilder.Entity<ClientFacture>()
                .HasOne(cf => cf.Client)
                .WithMany()
                .HasForeignKey(cf => cf.IdClient)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientFacture>()
                .HasOne(cf => cf.Facture)
                .WithMany()
                .HasForeignKey(cf => cf.IdFacture)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ClientFacture>()
                .Property(cf => cf.Statut)
                .HasDefaultValue(true);

            modelBuilder.Entity<ClientFacture>()
                .Property(cf => cf.EstArrierePreExistant)
                .HasDefaultValue(false);

            modelBuilder.Entity<ClientFacture>()
                .Property(cf => cf.MontantPaye)
                .HasDefaultValue(0m);

            // Index pour optimiser les requêtes
            modelBuilder.Entity<ClientFacture>()
                .HasIndex(cf => cf.IdClient)
                .HasDatabaseName("IX_ClientFacture_IdClient");

            modelBuilder.Entity<ClientFacture>()
                .HasIndex(cf => cf.IdFacture)
                .HasDatabaseName("IX_ClientFacture_IdFacture");

            modelBuilder.Entity<ClientFacture>()
                .HasIndex(cf => new { cf.IdClient, cf.Mois, cf.Annees })
                .HasDatabaseName("IX_ClientFacture_Client_Mois_Annees");

            modelBuilder.Entity<ClientFacture>()
                .HasIndex(cf => cf.MontantDu)
                .HasDatabaseName("IX_ClientFacture_MontantDu");

            modelBuilder.Entity<ClientFacture>()
                .HasIndex(cf => cf.DateEmission)
                .HasDatabaseName("IX_ClientFacture_DateEmission");

            // Configuration DeviseMonetaire
            modelBuilder.Entity<DeviseMonetaire>()
                .ToTable("DevisesMonetaires");

            modelBuilder.Entity<DeviseMonetaire>()
                .HasOne(d => d.Societe)
                .WithMany()
                .HasForeignKey(d => d.IdSociete)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DeviseMonetaire>()
                .HasIndex(d => new { d.IdSociete, d.CodeDevise })
                .IsUnique()
                .HasDatabaseName("UX_DevisesMonetaires_Societe_Code");

            modelBuilder.Entity<DeviseMonetaire>()
                .Property(d => d.CodeDevise)
                .IsRequired()
                .HasMaxLength(3);

            modelBuilder.Entity<DeviseMonetaire>()
                .Property(d => d.Statut)
                .HasDefaultValue(true);

            // Configuration TauxChange
            modelBuilder.Entity<TauxChange>()
                .ToTable("TauxChanges");

            modelBuilder.Entity<TauxChange>()
                .HasOne(t => t.Societe)
                .WithMany()
                .HasForeignKey(t => t.IdSociete)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TauxChange>()
                .HasIndex(t => new { t.IdSociete, t.CodeDeviseSource, t.CodeDeviseCible, t.DateEffet })
                .HasDatabaseName("IX_TauxChanges_Societe_Paired_DateEffet");

            modelBuilder.Entity<TauxChange>()
                .Property(t => t.Taux)
                .HasColumnType("decimal(18,6)");

            modelBuilder.Entity<Societe>()
                .Property(s => s.CodeDevisePrincipale)
                .HasMaxLength(3);

            // FlexPay
            modelBuilder.Entity<InfoPaiementSociete>(e =>
            {
                e.ToTable("InfosPaiementSociete");
                e.HasOne(x => x.Societe).WithMany().HasForeignKey(x => x.IdSociete).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.IdSociete).HasDatabaseName("IX_InfosPaiementSociete_IdSociete");
            });

            modelBuilder.Entity<PaiementElectroniqueEnAttente>(e =>
            {
                e.ToTable("PaiementsElectroniquesEnAttente");
                e.HasOne(x => x.Societe).WithMany().HasForeignKey(x => x.IdSociete).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.IdClient).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.ClientFacture).WithMany().HasForeignKey(x => x.IdClientFacture).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Facture).WithMany().HasForeignKey(x => x.IdFacture).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.PaiementFinalise).WithMany().HasForeignKey(x => x.IdPaiementFinalise).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(x => x.OrderNumber).HasDatabaseName("IX_PaiementElectronique_OrderNumber");
                e.HasIndex(x => x.Reference).IsUnique().HasDatabaseName("UX_PaiementElectronique_Reference");
                e.HasIndex(x => new { x.IdSociete, x.Statut }).HasDatabaseName("IX_PaiementElectronique_Societe_Statut");
            });

            modelBuilder.Entity<TransactionFlexPay>(e =>
            {
                e.ToTable("TransactionsFlexPay");
                e.HasOne(x => x.Pending).WithMany().HasForeignKey(x => x.IdPaiementElectroniqueEnAttente).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(x => x.OrderNumber).HasDatabaseName("IX_TransactionFlexPay_OrderNumber");
            });

            modelBuilder.Entity<CallbackFlexPay>(e =>
            {
                e.ToTable("CallbacksFlexPay");
                e.HasIndex(x => x.OrderNumber).HasDatabaseName("IX_CallbackFlexPay_OrderNumber");
                e.HasIndex(x => x.DateReception).HasDatabaseName("IX_CallbackFlexPay_DateReception");
            });

            modelBuilder.Entity<PaiementHold>(e =>
            {
                e.ToTable("PaiementHolds");
                e.HasOne(x => x.Pending).WithMany().HasForeignKey(x => x.IdPaiementElectroniqueEnAttente).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(x => new { x.IdSociete, x.CleRessource, x.EstLibere }).HasDatabaseName("IX_PaiementHold_Societe_Cle");
            });

            // Contrainte unique pour éviter les doublons (même client, même facture)
            // Note: Permet NULL pour IdFacture (arriérés pré-existants), donc on ne peut pas utiliser IsUnique
            // On utilisera une logique applicative pour éviter les doublons

            // Configuration PanneSignalement
            modelBuilder.Entity<PanneSignalement>()
                .ToTable("PanneSignalements");

            // Configuration CommunicationCampaign
            modelBuilder.Entity<CommunicationCampaign>()
                .HasOne(c => c.Societe)
                .WithMany()
                .HasForeignKey(c => c.IdSociete)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CommunicationCampaign>()
                .HasOne(c => c.UtilisateurCreateur)
                .WithMany()
                .HasForeignKey(c => c.IdUtilisateurCreateur)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuration PlainteClient
            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.Client)
                .WithMany()
                .HasForeignKey(p => p.IdClient)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.PanneSignalement)
                .WithMany()
                .HasForeignKey(p => p.IdPanneSignalement)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.AgentAssigné)
                .WithMany()
                .HasForeignKey(p => p.IdAgentAssigné)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlainteClient>()
                .HasOne(p => p.UtilisateurCreateur)
                .WithMany()
                .HasForeignKey(p => p.IdUtilisateurCreateur)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration Statut pour PlainteClient avec valeur par défaut
            modelBuilder.Entity<PlainteClient>()
                .Property(p => p.Statut)
                .HasDefaultValue(true);

            // Configuration ClientCrashed
            modelBuilder.Entity<ClientCrashed>()
                .HasOne(cc => cc.Societe)
                .WithMany()
                .HasForeignKey(cc => cc.IdSociete)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientCrashed>()
                .HasOne(cc => cc.ClientCree)
                .WithMany()
                .HasForeignKey(cc => cc.IdClientCree)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ClientCrashed>()
                .HasIndex(cc => cc.IdSociete)
                .HasDatabaseName("IX_ClientCrashed_IdSociete");

            modelBuilder.Entity<ClientCrashed>()
                .HasIndex(cc => cc.Statut)
                .HasDatabaseName("IX_ClientCrashed_Statut");

            modelBuilder.Entity<ClientCrashed>()
                .HasIndex(cc => cc.DateCreation)
                .HasDatabaseName("IX_ClientCrashed_DateCreation");

            modelBuilder.Entity<ClientCrashed>()
                .Property(cc => cc.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .IsRequired();

            // Configuration ArriereeCrashed
            modelBuilder.Entity<ArriereeCrashed>()
                .HasOne(ac => ac.Client)
                .WithMany()
                .HasForeignKey(ac => ac.IdClient)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ArriereeCrashed>()
                .HasOne(ac => ac.ClientFactureCree)
                .WithMany()
                .HasForeignKey(ac => ac.IdClientFactureCree)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ArriereeCrashed>()
                .HasIndex(ac => ac.Statut)
                .HasDatabaseName("IX_ArriereeCrashed_Statut");

            modelBuilder.Entity<ArriereeCrashed>()
                .HasIndex(ac => ac.CodeCons)
                .HasDatabaseName("IX_ArriereeCrashed_CodeCons");

            modelBuilder.Entity<ArriereeCrashed>()
                .HasIndex(ac => ac.DateCreation)
                .HasDatabaseName("IX_ArriereeCrashed_DateCreation");

            modelBuilder.Entity<ArriereeCrashed>()
                .Property(ac => ac.DateCreation)
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .IsRequired();

            modelBuilder.Entity<CategorieDepense>(e =>
            {
                e.ToTable("CategorieDepenses");
                e.HasOne(c => c.Societe)
                    .WithMany()
                    .HasForeignKey(c => c.IdSociete)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(c => new { c.IdSociete, c.NomCategorie })
                    .IsUnique()
                    .HasDatabaseName("IX_CategorieDepense_Societe_Nom");
            });

            modelBuilder.Entity<Depense>(e =>
            {
                e.ToTable("Depenses");
                e.HasOne(d => d.Societe)
                    .WithMany()
                    .HasForeignKey(d => d.IdSociete)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.CategorieDepense)
                    .WithMany(c => c.Depenses)
                    .HasForeignKey(d => d.IdCategorieDepense)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(d => d.UtilisateurCreateur)
                    .WithMany()
                    .HasForeignKey(d => d.IdUtilisateurCreateur)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(d => d.UtilisateurValidateur)
                    .WithMany()
                    .HasForeignKey(d => d.IdUtilisateurValidateur)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(d => d.Cabine)
                    .WithMany()
                    .HasForeignKey(d => d.IdCabine)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(d => d.Axe)
                    .WithMany()
                    .HasForeignKey(d => d.IdAxe)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasIndex(d => new { d.IdSociete, d.DateDepense })
                    .HasDatabaseName("IX_Depense_Societe_Date");
                e.HasIndex(d => new { d.IdSociete, d.Statut })
                    .HasDatabaseName("IX_Depense_Societe_Statut");
                e.HasIndex(d => d.IdUtilisateurCreateur)
                    .HasDatabaseName("IX_Depense_UtilisateurCreateur");
            });
        }

        /// <summary>
        /// Initialise les données par défaut du système (Super-Admin, Société par défaut, etc.)
        /// </summary>
        public async Task InitializeDefaultDataAsync()
        {
            // Utiliser CreateExecutionStrategy() pour gérer les transactions avec MySqlRetryingExecutionStrategy
            var strategy = Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = await Database.BeginTransactionAsync())
                {
                    try
                    {
                        var currentDate = DateTime.Now;
                        
                        // 1. Créer ou récupérer les rôles
                        var superAdminRole = await CreateOrGetSuperAdminRoleAsync(currentDate);
                        var adminRole = await CreateOrGetAdminRoleAsync(currentDate);
                        
                        // 2. Créer ou récupérer la société par défaut
                        var defaultSociete = await CreateOrGetDefaultSocieteAsync(currentDate);
                        
                        // 3. Créer l'Agent Manager Général + Utilisateur Super-Admin
                        var superAdminUser = await CreateOrGetSuperAdminWithAgentAsync(superAdminRole, defaultSociete, currentDate);
                        
                        // 4. Créer l'Agent Admin + Utilisateur Admin
                        var adminUser = await CreateOrGetAdminWithAgentAsync(adminRole, defaultSociete, currentDate);
                        
                        await transaction.CommitAsync();
                        
                        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                        Console.WriteLine("║     ✅ INITIALISATION DES DONNÉES PAR DÉFAUT TERMINÉE      ║");
                        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                        Console.WriteLine($"📋 Rôle Super-Admin: ID {superAdminRole.IdRole}");
                        Console.WriteLine($"📋 Rôle Admin: ID {adminRole.IdRole}");
                        Console.WriteLine($"🏢 Société par défaut: ID {defaultSociete.IdSociete} - {defaultSociete.Nom}");
                        Console.WriteLine($"👤 Utilisateur Super-Admin: ID {superAdminUser.IdUtilisateur}");
                        Console.WriteLine($"   📧 Email: {superAdminUser.Email}");
                        Console.WriteLine($"   📱 Téléphone: {superAdminUser.Telephone}");
                        Console.WriteLine($"   🔑 Username: {superAdminUser.DefaultUsername}");
                        Console.WriteLine($"   ⚠️  Mot de passe par défaut: Super-Admin");
                        Console.WriteLine($"   🔒 Doit changer le mot de passe: {superAdminUser.DoitChangerMotDePasse}");
                        Console.WriteLine($"👤 Utilisateur Admin: ID {adminUser.IdUtilisateur}");
                        Console.WriteLine($"   📧 Email: {adminUser.Email}");
                        Console.WriteLine($"   📱 Téléphone: {adminUser.Telephone}");
                        Console.WriteLine($"   🔑 Username: {adminUser.DefaultUsername}");
                        Console.WriteLine($"   ⚠️  Mot de passe par défaut: Admin");
                        Console.WriteLine($"   🔒 Doit changer le mot de passe: {adminUser.DoitChangerMotDePasse}");
                        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        Console.WriteLine($"Erreur lors de l'initialisation: {ex.Message}");
                        throw;
                    }
                }
            });
        }

        private async Task<Role> CreateOrGetSuperAdminRoleAsync(DateTime currentDate)
        {
            var existingRole = await Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");
            
            if (existingRole != null)
            {
                if (existingRole.Niveau != 1)
                {
                    existingRole.Niveau = 1;
                    await SaveChangesAsync();
                    Console.WriteLine($"Rôle Super-Admin (ID {existingRole.IdRole}) : Niveau corrigé à 1");
                }
                else
                {
                    Console.WriteLine($"Rôle Super-Admin existe déjà avec l'ID: {existingRole.IdRole}");
                }
                return existingRole;
            }
            
            var newRole = new Role
            {
                Nom = "Super-Admin",
                Niveau = 1,
                DateCreation = currentDate
            };
            
            Roles.Add(newRole);
            await SaveChangesAsync();
            
            Console.WriteLine($"Rôle Super-Admin créé avec l'ID: {newRole.IdRole}");
            return newRole;
        }

        private async Task<Role> CreateOrGetAdminRoleAsync(DateTime currentDate)
        {
            var existingRole = await Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");
            
            if (existingRole != null)
            {
                Console.WriteLine($"Rôle Admin existe déjà avec l'ID: {existingRole.IdRole}");
                return existingRole;
            }
            
            var newRole = new Role
            {
                Nom = "Admin",
                Niveau = 2,
                DateCreation = currentDate
            };
            
            Roles.Add(newRole);
            await SaveChangesAsync();
            
            Console.WriteLine($"Rôle Admin créé avec l'ID: {newRole.IdRole}");
            return newRole;
        }

        private async Task<Societe> CreateOrGetDefaultSocieteAsync(DateTime currentDate)
        {
            var existingSociete = await Societes.FirstOrDefaultAsync(e => e.Nom == "Kenergie");
            
            if (existingSociete != null)
            {
                Console.WriteLine($"✅ Société par défaut existe déjà avec l'ID: {existingSociete.IdSociete}");
                return existingSociete;
            }
            
            var newSociete = new Societe
            {
                Nom = "Kenergie",
                Devise = "Excellence et Innovation",
                Type = "Privée",
                Description = "Société d'excellence offrant des services de qualité énergétique",
                Telephone = "+243999999999",
                EmailContact = "contact@kenergie.cd",
                SiteWeb = "https://www.kenergie.cd",
                NomCompletResponsable = "Administrateur Super Admin", // Nom complet du responsable
                GenreResponsable = "Masculin",
                Statut = true,
                DateCreation = currentDate
            };
            
            Societes.Add(newSociete);
            await SaveChangesAsync();
            
            Console.WriteLine($"✅ Société par défaut créée avec l'ID: {newSociete.IdSociete}");
            Console.WriteLine($"   Nom: {newSociete.Nom}");
            Console.WriteLine($"   Email: {newSociete.EmailContact}");
            Console.WriteLine($"   Téléphone: {newSociete.Telephone}");
            return newSociete;
        }

        private async Task<Utilisateur> CreateOrGetSuperAdminWithAgentAsync(Role superAdminRole, Societe defaultSociete, DateTime currentDate)
        {
            var existingUser = await Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == superAdminRole.IdRole && u.IdSociete == defaultSociete.IdSociete);
            
            if (existingUser != null)
            {
                Console.WriteLine($"✅ Utilisateur Super-Admin existe déjà avec l'ID: {existingUser.IdUtilisateur}");
                
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == superAdminRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = superAdminRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    UserRoles.Add(userRole);
                    await SaveChangesAsync();
                    Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur existant {existingUser.IdUtilisateur} avec le rôle Super-Admin");
                }
                
                return existingUser;
            }
            
            // 1. Créer l'Agent Manager Général
            var managerAgent = await CreateOrGetManagerGeneralAgentAsync(defaultSociete, currentDate);
            
            // 2. Générer le hash du mot de passe par défaut
            // Mot de passe par défaut: "Super-Admin" (à changer lors de la première connexion)
            string motDePasseParDefaut = "Super-Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));
            
            // 3. Créer l'Utilisateur Super-Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = managerAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = managerAgent.NomComplet,
                Email = "superadmin@kenergie.cd",
                DefaultUsername = "SuperAdmin",
                Telephone = "+243999999999",
                MotDePasseHash = motDePasseHash,
                Genre = managerAgent.Genre,
                DateNaissance = managerAgent.DateNaissance,
                Statut = true,
                IdRole = superAdminRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true // Forcer le changement de mot de passe à la première connexion
            };
            
            Utilisateurs.Add(newUser);
            await SaveChangesAsync();
            
            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == superAdminRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = superAdminRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                UserRoles.Add(userRole);
                await SaveChangesAsync();
                Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur {newUser.IdUtilisateur} avec le rôle Super-Admin");
            }
            
            Console.WriteLine($"✅ Utilisateur Super-Admin créé avec l'ID: {newUser.IdUtilisateur} (lié à l'Agent {managerAgent.IdAgent})");
            Console.WriteLine($"   Email: {newUser.Email}");
            Console.WriteLine($"   Username: {newUser.DefaultUsername}");
            Console.WriteLine($"   Téléphone: {newUser.Telephone}");
            Console.WriteLine($"   ⚠️  Mot de passe par défaut: {motDePasseParDefaut} (à changer à la première connexion)");
            return newUser;
        }

        private async Task<Agent> CreateOrGetManagerGeneralAgentAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingManager = await Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Manager Général");
            
            if (existingManager != null)
            {
                Console.WriteLine($"Agent Manager Général existe déjà avec l'ID: {existingManager.IdAgent}");
                return existingManager;
            }
            
            var managerAgent = new Agent
            {
                NomComplet = "Administrateur Super Admin",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-40),
                TelephoneAgent = "+243999999999",
                EmailAgent = "superadmin@kenergie.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Manager Général",
                RoleAgent = "Super-Administrateur",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };
            
            Agents.Add(managerAgent);
            await SaveChangesAsync();
            
            Console.WriteLine($"Agent Manager Général créé avec l'ID: {managerAgent.IdAgent} - Matricule: {managerAgent.Matricule}");
            return managerAgent;
        }

        private async Task<string> GenerateUniqueMatriculeAgentAsync()
        {
            string matricule;
            
            do
            {
                string annee = DateTime.Now.Year.ToString().Substring(2);
                string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = $"NAT{annee}-{guid}";
                
            } while (await Agents.AnyAsync(a => a.Matricule == matricule));
            
            return matricule;
        }

        private async Task<Utilisateur> CreateOrGetAdminWithAgentAsync(Role adminRole, Societe defaultSociete, DateTime currentDate)
        {
            var existingUser = await Utilisateurs
                .FirstOrDefaultAsync(u => u.IdRole == adminRole.IdRole && u.IdSociete == defaultSociete.IdSociete && u.Email == "admin@kenergie.cd");
            
            if (existingUser != null)
            {
                Console.WriteLine($"✅ Utilisateur Admin existe déjà avec l'ID: {existingUser.IdUtilisateur}");
                
                // Vérifier si l'association UserRole existe, sinon la créer
                var existingUserRole = await UserRoles
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == existingUser.IdUtilisateur && ur.IdRole == adminRole.IdRole);
                
                if (existingUserRole == null)
                {
                    var userRole = new UserRole
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdRole = adminRole.IdRole,
                        IsPrimary = true, // Rôle principal
                        DateAttribution = currentDate,
                        Statut = true // Statut actif
                    };
                    
                    UserRoles.Add(userRole);
                    await SaveChangesAsync();
                    Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur existant {existingUser.IdUtilisateur} avec le rôle Admin");
                }
                
                return existingUser;
            }
            
            // 1. Créer l'Agent Admin
            var adminAgent = await CreateOrGetAdminAgentAsync(defaultSociete, currentDate);
            
            // 2. Générer le hash du mot de passe par défaut
            // Mot de passe par défaut: "Admin" (à changer lors de la première connexion)
            string motDePasseParDefaut = "Admin";
            string motDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut, BCrypt.Net.BCrypt.GenerateSalt(11));
            
            // 3. Créer l'Utilisateur Admin lié à cet Agent
            var newUser = new Utilisateur
            {
                IdAgent = adminAgent.IdAgent,
                ReferenceUtilisateur = Guid.NewGuid(),
                NomComplet = adminAgent.NomComplet,
                Email = "admin@kenergie.cd",
                DefaultUsername = "Admin",
                Telephone = "+243888888888",
                MotDePasseHash = motDePasseHash,
                Genre = adminAgent.Genre,
                DateNaissance = adminAgent.DateNaissance,
                Statut = true,
                IdRole = adminRole.IdRole,
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate,
                IsConnecte = false,
                DoitChangerMotDePasse = true // Forcer le changement de mot de passe à la première connexion
            };
            
            Utilisateurs.Add(newUser);
            await SaveChangesAsync();
            
            // 4. Créer l'association UserRole (Multi-rôles)
            var newUserRoleCheck = await UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == newUser.IdUtilisateur && ur.IdRole == adminRole.IdRole);
            
            if (newUserRoleCheck == null)
            {
                var userRole = new UserRole
                {
                    IdUtilisateur = newUser.IdUtilisateur,
                    IdRole = adminRole.IdRole,
                    IsPrimary = true, // Rôle principal
                    DateAttribution = currentDate,
                    Statut = true // Statut actif
                };
                
                UserRoles.Add(userRole);
                await SaveChangesAsync();
                Console.WriteLine($"✅ Association UserRole créée pour l'utilisateur {newUser.IdUtilisateur} avec le rôle Admin");
            }
            
            Console.WriteLine($"✅ Utilisateur Admin créé avec l'ID: {newUser.IdUtilisateur} (lié à l'Agent {adminAgent.IdAgent})");
            Console.WriteLine($"   Email: {newUser.Email}");
            Console.WriteLine($"   Username: {newUser.DefaultUsername}");
            Console.WriteLine($"   Téléphone: {newUser.Telephone}");
            Console.WriteLine($"   ⚠️  Mot de passe par défaut: {motDePasseParDefaut} (à changer à la première connexion)");
            return newUser;
        }

        private async Task<Agent> CreateOrGetAdminAgentAsync(Societe defaultSociete, DateTime currentDate)
        {
            var existingAdmin = await Agents
                .FirstOrDefaultAsync(a => a.IdSociete == defaultSociete.IdSociete && a.Fonction == "Administrateur");
            
            if (existingAdmin != null)
            {
                Console.WriteLine($"Agent Administrateur existe déjà avec l'ID: {existingAdmin.IdAgent}");
                return existingAdmin;
            }
            
            var adminAgent = new Agent
            {
                NomComplet = "Administrateur Kenergie",
                Genre = "Masculin",
                DateNaissance = DateTime.Now.AddYears(-35),
                TelephoneAgent = "+243888888888",
                EmailAgent = "admin@kenergie.cd",
                Statut = true,
                EtatCivil = "Marié",
                Fonction = "Administrateur",
                RoleAgent = "Admin",
                Matricule = await GenerateUniqueMatriculeAgentAsync(),
                IdSociete = defaultSociete.IdSociete,
                DateCreation = currentDate
            };
            
            Agents.Add(adminAgent);
            await SaveChangesAsync();
            
            Console.WriteLine($"Agent Administrateur créé avec l'ID: {adminAgent.IdAgent} - Matricule: {adminAgent.Matricule}");
            return adminAgent;
        }
    }
}
