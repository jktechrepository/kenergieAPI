using System.Globalization;
using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    /// <summary>
    /// Service d'envoi de notifications lors de l'enregistrement d'un paiement.
    /// </summary>
    public class PaiementNotificationService
    {
        private readonly KenergieDbContext _context;
        private readonly INotificationSender _notificationSender;
        private readonly ILogger<PaiementNotificationService> _logger;
        private readonly string _baseUrl;
        private readonly string _facturePath;

        public PaiementNotificationService(
            KenergieDbContext context,
            INotificationSender notificationSender,
            ILogger<PaiementNotificationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _notificationSender = notificationSender;
            _logger = logger;

            // Récupérer la configuration du frontend
            _baseUrl = configuration["FrontendSettings:BaseUrl"] ?? "https://k-energie.kansaconsulting.com";
            _facturePath = configuration["FrontendSettings:FacturePath"] ?? "/factures";
        }

        /// <summary>
        /// Envoie une notification au client suite à un paiement.
        /// </summary>
        public async Task<bool> NotifierPaiementAsync(Paiement paiement)
        {
            try
            {
                // Charger la facture et le client associés
                var facture = await _context.Factures.FirstOrDefaultAsync(f => f.IdFacture == paiement.IdFacture);
            Client? client = null;

            if (paiement.IdClient.HasValue)
            {
                client = await _context.Clients.FirstOrDefaultAsync(c => c.IdClient == paiement.IdClient.Value);
            }
            else
            {
                _logger.LogWarning("⚠️ Paiement {PaiementId} sans IdClient associé, notification ignorée", paiement.IdPaiement);
                return false;
            }

                if (client == null || facture == null)
                {
                    _logger.LogWarning("⚠️ Impossible d'envoyer la notification paiement : client ou facture introuvable (Paiement {PaiementId})", paiement.IdPaiement);
                    return false;
                }

                // Récupérer l'utilisateur lié au client
                var utilisateur = await _context.Utilisateurs
                    .Include(u => u.Societe)
                    .FirstOrDefaultAsync(u => u.IdClient == client.IdClient);

                if (utilisateur == null)
                {
                    _logger.LogWarning("⚠️ Aucun utilisateur associé au client {ClientId}, notification paiement non envoyée", client.IdClient);
                    return false;
                }

                // Préférences de notification
                var preferences = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.IdUtilisateur == utilisateur.IdUtilisateur);

                if (preferences?.OptOutGlobal == true)
                {
                    _logger.LogInformation("⏭️ Utilisateur {UtilisateurId} a opt-out globalement, notification paiement ignorée", utilisateur.IdUtilisateur);
                    return false;
                }

                var societe = utilisateur.Societe ?? await _context.Societes.FirstOrDefaultAsync();

                var nomSociete = societe?.Nom ?? "K-Energie";
                var nomClient = client.NomClient ?? utilisateur.NomComplet ?? "Client";
                var numeroFacture = facture.NumeroFacture ?? $"FACT-{facture.IdFacture}";
                var montantPaye = paiement.MontantPaye.ToString("N2", CultureInfo.InvariantCulture);
                var datePaiement = paiement.DatePaiement.ToString("dd/MM/yyyy");
                var lienFacture = $"{_baseUrl.TrimEnd('/')}{_facturePath.TrimStart('/')}/{facture.IdFacture}";

                var allowEmail = preferences?.AllowEmail ?? true;
                var allowSms = preferences?.AllowSms ?? true;
                var allowPush = preferences?.AllowPush ?? true;
                var allowInApp = preferences?.AllowInApp ?? true;

                var message = new NotificationMessage
                {
                    Email = new EmailNotificationMessage
                    {
                        Subject = $"Paiement reçu - Facture {numeroFacture}",
                        HtmlBody = $"Bonjour {nomClient},<br/><br/>Nous avons bien reçu votre paiement de <b>{montantPaye} FC</b> pour la facture <b>{numeroFacture}</b> en date du {datePaiement}.<br/><br/>Vous pouvez consulter la facture ici : <a href=\"{lienFacture}\">{lienFacture}</a><br/><br/>Merci pour votre confiance.<br/>{nomSociete}",
                        PlainTextBody = $"Bonjour {nomClient},\n\nNous avons bien reçu votre paiement de {montantPaye} FC pour la facture {numeroFacture} en date du {datePaiement}.\n\nVous pouvez consulter la facture ici : {lienFacture}\n\nMerci pour votre confiance.\n{nomSociete}",
                        IsEnabled = allowEmail && !string.IsNullOrWhiteSpace(utilisateur.Email)
                    },
                    Sms = new SmsNotificationMessage
                    {
                        Body = $"Paiement {montantPaye} FC reçu pour facture {numeroFacture} ({datePaiement}). Merci.",
                        IsEnabled = allowSms && !string.IsNullOrWhiteSpace(utilisateur.Telephone)
                    },
                    Push = new PushNotificationMessage
                    {
                        Title = "Paiement reçu",
                        Body = $"Facture {numeroFacture} - {montantPaye} FC",
                        Type = "PAIEMENT",
                        Data = new Dictionary<string, string>
                        {
                            { "type", "PAIEMENT" },
                            { "idPaiement", paiement.IdPaiement.ToString() },
                            { "idFacture", facture.IdFacture.ToString() },
                            { "numeroFacture", numeroFacture },
                            { "montant", montantPaye },
                            { "datePaiement", datePaiement },
                            { "lien", lienFacture }
                        },
                        IsEnabled = allowPush
                    },
                    InApp = new InAppNotificationMessage
                    {
                        Title = "Paiement enregistré",
                        Content = $"Paiement de {montantPaye} FC pour la facture {numeroFacture} ({datePaiement})",
                        Type = "PAIEMENT",
                        Icon = "payments",
                        ActionLink = $"/factures/{facture.IdFacture}",
                        Metadata = new Dictionary<string, string>
                        {
                            { "idPaiement", paiement.IdPaiement.ToString() },
                            { "idFacture", facture.IdFacture.ToString() },
                            { "numeroFacture", numeroFacture },
                            { "montant", montantPaye },
                            { "datePaiement", datePaiement }
                        },
                        IsEnabled = allowInApp
                    }
                };

                var context = new NotificationContext
                {
                    Kind = NotificationKind.Paiement,
                    UtilisateurDestinataire = utilisateur,
                    Societe = societe,
                    AcceptsSms = true,
                    AllowPush = allowPush,
                    AllowInApp = allowInApp,
                    AllowSms = allowSms,
                    UtilisateurActif = utilisateur.Statut == true,
                    Preferences = preferences != null ? new Dictionary<string, bool>
                    {
                        { "push", preferences.AllowPush },
                        { "inapp", preferences.AllowInApp },
                        { "sms", preferences.AllowSms },
                        { "email", preferences.AllowEmail }
                    } : null
                };

                await _notificationSender.SendAsync(new NotificationDispatchResult(context, message));

                _logger.LogInformation("✅ Notification paiement envoyée (Paiement {PaiementId}, Facture {FactureId}, Client {ClientId})",
                    paiement.IdPaiement, facture.IdFacture, client.IdClient);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'envoi de la notification paiement (Paiement {PaiementId})", paiement.IdPaiement);
                return false;
            }
        }
    }
}

