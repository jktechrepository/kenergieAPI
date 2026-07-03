using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Notifications;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;

namespace Kenergie.Services
{
    /// <summary>
    /// Service de diffusion multi-canal des factures aux clients
    /// </summary>
    public class FactureNotificationService
    {
        private readonly KenergieDbContext _context;
        private readonly INotificationSender _notificationSender;
        private readonly ILogger<FactureNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly string _facturePath;

        public FactureNotificationService(
            KenergieDbContext context,
            INotificationSender notificationSender,
            ILogger<FactureNotificationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _notificationSender = notificationSender;
            _logger = logger;
            _configuration = configuration;
            
            // Récupérer la configuration du frontend
            _baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://k-energie.kansaconsulting.com";
            _facturePath = _configuration["FrontendSettings:FacturePath"] ?? "/factures";
        }

        /// <summary>
        /// Diffuse une facture à un client spécifique via tous les canaux activés
        /// </summary>
        public async Task<bool> DiffuserFactureAClientAsync(Facture facture, Client client, Utilisateur? utilisateur = null)
        {
            try
            {
                // Récupérer l'utilisateur si non fourni
                if (utilisateur == null && client != null)
                {
                    utilisateur = await _context.Utilisateurs
                        .Include(u => u.Societe)
                        .FirstOrDefaultAsync(u => u.IdClient == client.IdClient);
                }

                if (utilisateur == null)
                {
                    _logger.LogWarning("⚠️ Aucun utilisateur trouvé pour le client {ClientId}. Impossible de diffuser la facture.", client?.IdClient);
                    return false;
                }

                // Vérifier les préférences de notification de l'utilisateur
                var preferences = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.IdUtilisateur == utilisateur.IdUtilisateur);

                // Si opt-out global ou opt-out factures, ne pas diffuser
                if (preferences != null && (preferences.OptOutGlobal || preferences.OptOutFactures))
                {
                    _logger.LogInformation("⏭️ Client {ClientId} a opt-out des notifications de factures", client?.IdClient);
                    return false;
                }

                // Récupérer la société
                var societe = utilisateur.Societe ?? await _context.Societes.FirstOrDefaultAsync();

                // Préparer le contexte de notification avec les préférences
                var context = new NotificationContext
                {
                    Kind = NotificationKind.Facture,
                    UtilisateurDestinataire = utilisateur,
                    Societe = societe,
                    AcceptsSms = true, // Par défaut, accepter SMS
                    AllowPush = preferences?.AllowPush ?? true,
                    AllowInApp = preferences?.AllowInApp ?? true,
                    AllowSms = preferences?.AllowSms ?? true,
                    UtilisateurActif = utilisateur.Statut == true,
                    Preferences = preferences != null ? new Dictionary<string, bool>
                    {
                        { "push", preferences.AllowPush },
                        { "inapp", preferences.AllowInApp },
                        { "sms", preferences.AllowSms },
                        { "email", preferences.AllowEmail }
                    } : null
                };

                // Préparer les messages pour chaque canal (en respectant les préférences)
                var message = PrepareFactureMessages(facture, client, utilisateur, societe, preferences);

                // Créer le résultat de dispatch
                var dispatchResult = new NotificationDispatchResult(context, message);

                // Envoyer via tous les canaux
                await _notificationSender.SendAsync(dispatchResult);

                _logger.LogInformation("✅ Facture {FactureId} diffusée au client {ClientId} via tous les canaux activés", 
                    facture.IdFacture, client?.IdClient);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la diffusion de la facture {FactureId} au client {ClientId}", 
                    facture.IdFacture, client?.IdClient);
                return false;
            }
        }

        /// <summary>
        /// Récupère le nombre total de clients actifs ayant un usage spécifique
        /// </summary>
        public async Task<int> GetTotalClientsByUsageAsync(int idUsage)
        {
            return await _context.ClientUsages
                .Where(cu => cu.IdUsage == idUsage && 
                            cu.Client != null && 
                            cu.Client.Statut == true &&
                            cu.Client.IsActif == true &&
                            cu.Statut == true)
                .CountAsync();
        }

        /// <summary>
        /// Diffuse une facture à tous les clients ayant un usage spécifique
        /// </summary>
        public async Task<int> DiffuserFactureAUsageAsync(Facture facture, int idUsage)
        {
            try
            {
                // Notifier uniquement les clients ayant reçu une ClientFacture pour cette facture
                // (aligné avec skip doublons : 2e POST sans nouvelles lignes → 0 notification)
                var clientIds = await _context.ClientFactures
                    .AsNoTracking()
                    .Where(cf => cf.IdFacture == facture.IdFacture && cf.Statut == true)
                    .Select(cf => cf.IdClient)
                    .Distinct()
                    .ToListAsync();

                if (clientIds.Count == 0)
                {
                    _logger.LogInformation(
                        "Facture {FactureId}: aucune ClientFacture active — diffusion ignorée (usage {UsageId})",
                        facture.IdFacture,
                        idUsage);
                    return 0;
                }

                var clients = await _context.Clients
                    .AsNoTracking()
                    .Where(c => clientIds.Contains(c.IdClient)
                        && c.Statut == true
                        && c.IsActif == true)
                    .ToListAsync();

                var utilisateurs = await _context.Utilisateurs
                    .Include(u => u.Societe)
                    .Where(u => u.IdClient.HasValue && clients.Select(c => c.IdClient).Contains(u.IdClient.Value))
                    .ToListAsync();

                int successCount = 0;

                foreach (var client in clients)
                {
                    var utilisateur = utilisateurs.FirstOrDefault(u => u.IdClient == client.IdClient);
                    if (utilisateur != null)
                    {
                        var success = await DiffuserFactureAClientAsync(facture, client, utilisateur);
                        if (success) successCount++;
                    }
                }

                _logger.LogInformation("✅ Facture {FactureId} diffusée à {Count}/{Total} clients ayant l'usage {UsageId}", 
                    facture.IdFacture, successCount, clients.Count, idUsage);

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la diffusion de la facture {FactureId} à l'usage {UsageId}", 
                    facture.IdFacture, idUsage);
                return 0;
            }
        }

        /// <summary>
        /// Prépare les messages pour chaque canal de notification
        /// </summary>
        private NotificationMessage PrepareFactureMessages(Facture facture, Client client, Utilisateur utilisateur, Societe? societe, NotificationPreference? preferences = null)
        {
            var nomSociete = societe?.Nom ?? "K-Energie";
            var nomClient = client?.NomClient ?? utilisateur.NomComplet ?? "Client";
            var numeroFacture = facture.NumeroFacture ?? $"FACT-{facture.IdFacture}";
            var montant = facture.Montant?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00";
            var dateEmission = facture.DateEmission?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy");
            var moisAnnee = $"{facture.MoisEmission:D2}/{facture.AnneesEmission}";
            // Utiliser la configuration au lieu du lien hardcodé
            var lienFacture = $"{_baseUrl.TrimEnd('/')}{_facturePath.TrimStart('/')}/{facture.IdFacture}";

            // Email - respecter les préférences
            var allowEmail = preferences?.AllowEmail ?? true;
            var emailMessage = new EmailNotificationMessage
            {
                Subject = $"Nouvelle facture disponible - {numeroFacture}",
                HtmlBody = CreateFactureEmailHtml(facture, client, utilisateur, nomSociete, numeroFacture, montant, dateEmission, moisAnnee, lienFacture),
                PlainTextBody = CreateFactureEmailPlainText(facture, client, utilisateur, nomSociete, numeroFacture, montant, dateEmission, moisAnnee, lienFacture),
                IsEnabled = allowEmail && !string.IsNullOrWhiteSpace(utilisateur.Email)
            };

            // SMS - respecter les préférences
            var allowSms = preferences?.AllowSms ?? true;
            var smsMessage = new SmsNotificationMessage
            {
                Body = CreateFactureSmsText(nomSociete, numeroFacture, montant, dateEmission, moisAnnee, lienFacture, facture.IdFacture),
                IsEnabled = allowSms && !string.IsNullOrWhiteSpace(utilisateur.Telephone)
            };

            // Push - respecter les préférences
            var allowPush = preferences?.AllowPush ?? true;
            var pushMessage = new PushNotificationMessage
            {
                Title = $"Nouvelle facture - {nomSociete}",
                Body = $"Facture {numeroFacture} ({moisAnnee}) - {montant} FC",
                Type = "FACTURE",
                Data = new Dictionary<string, string>
                {
                    { "type", "FACTURE" },
                    { "idFacture", facture.IdFacture.ToString() },
                    { "numeroFacture", numeroFacture },
                    { "montant", montant },
                    { "moisAnnee", moisAnnee },
                    { "dateEmission", dateEmission },
                    { "nomSociete", nomSociete },
                    { "lien", lienFacture }
                },
                IsEnabled = allowPush
            };

            // In-App - respecter les préférences
            var allowInApp = preferences?.AllowInApp ?? true;
            var inAppMessage = new InAppNotificationMessage
            {
                Title = "Nouvelle facture disponible",
                Content = $"Facture {numeroFacture} d'un montant de {montant} FC pour la période {moisAnnee}",
                Type = "FACTURE",
                Icon = "receipt",
                ActionLink = $"/factures/{facture.IdFacture}",
                Metadata = new Dictionary<string, string>
                {
                    { "idFacture", facture.IdFacture.ToString() },
                    { "numeroFacture", numeroFacture },
                    { "montant", montant },
                    { "dateEmission", dateEmission }
                },
                IsEnabled = allowInApp
            };

            return new NotificationMessage
            {
                Email = emailMessage,
                Sms = smsMessage,
                Push = pushMessage,
                InApp = inAppMessage
            };
        }

        /// <summary>
        /// Crée le template HTML pour l'email de facture
        /// </summary>
        private string CreateFactureEmailHtml(
            Facture facture, 
            Client client, 
            Utilisateur utilisateur, 
            string nomSociete, 
            string numeroFacture, 
            string montant, 
            string dateEmission, 
            string moisAnnee, 
            string lienFacture)
        {
            var nomClient = client?.NomClient ?? utilisateur.NomComplet ?? "Client";
            
            return $@"
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Nouvelle facture disponible</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }}
        .email-wrapper {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }}
        .header {{
            background-color: #232f3e;
            padding: 30px 40px;
            text-align: center;
        }}
        .header-logo {{
            color: #ffffff;
            font-size: 28px;
            font-weight: 600;
            letter-spacing: 1px;
            margin: 0;
        }}
        .content {{
            padding: 40px;
            color: #232f3e;
        }}
        .title {{
            font-size: 24px;
            font-weight: 600;
            color: #232f3e;
            margin: 0 0 20px 0;
        }}
        .facture-info {{
            background-color: #f8f9fa;
            border-left: 4px solid #ff9900;
            padding: 20px;
            margin: 20px 0;
        }}
        .facture-detail {{
            margin: 10px 0;
            font-size: 16px;
        }}
        .facture-detail strong {{
            color: #232f3e;
        }}
        .montant {{
            font-size: 28px;
            font-weight: 600;
            color: #ff9900;
            margin: 20px 0;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            background-color: #ff9900;
            color: #ffffff;
            text-decoration: none;
            border-radius: 5px;
            font-weight: 600;
            margin: 20px 0;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 14px;
            color: #666;
        }}
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <h1 class='header-logo'>K-Energie</h1>
        </div>
        <div class='content'>
            <h2 class='title'>Nouvelle facture disponible</h2>
            <p>Bonjour {nomClient},</p>
            <p>Une nouvelle facture est disponible dans votre espace client.</p>
            
            <div class='facture-info'>
                <div class='facture-detail'><strong>Numéro de facture :</strong> {numeroFacture}</div>
                <div class='facture-detail'><strong>Période :</strong> {moisAnnee}</div>
                <div class='facture-detail'><strong>Date d'émission :</strong> {dateEmission}</div>
                <div class='montant'>Montant : {montant} FC</div>
            </div>
            
            <p>Vous pouvez consulter et télécharger votre facture en cliquant sur le bouton ci-dessous :</p>
            
            <a href='{lienFacture}' class='button'>Voir ma facture</a>
            
            <p>Si vous avez des questions concernant cette facture, n'hésitez pas à nous contacter.</p>
            
            <p>Cordialement,<br>L'équipe {nomSociete}</p>
        </div>
        <div class='footer'>
            <p>Cet email a été envoyé automatiquement. Merci de ne pas y répondre.</p>
            <p>&copy; {DateTime.Now.Year} {nomSociete}. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Crée le template texte pour l'email de facture
        /// </summary>
        private string CreateFactureEmailPlainText(
            Facture facture, 
            Client client, 
            Utilisateur utilisateur, 
            string nomSociete, 
            string numeroFacture, 
            string montant, 
            string dateEmission, 
            string moisAnnee, 
            string lienFacture)
        {
            var nomClient = client?.NomClient ?? utilisateur.NomComplet ?? "Client";
            
            return $@"Nouvelle facture disponible

Bonjour {nomClient},

Une nouvelle facture est disponible dans votre espace client.

Numéro de facture : {numeroFacture}
Période : {moisAnnee}
Date d'émission : {dateEmission}
Montant : {montant} FC

Vous pouvez consulter et télécharger votre facture en visitant :
{lienFacture}

Si vous avez des questions concernant cette facture, n'hésitez pas à nous contacter.

Cordialement,
L'équipe {nomSociete}

---
Cet email a été envoyé automatiquement. Merci de ne pas y répondre.
© {DateTime.Now.Year} {nomSociete}. Tous droits réservés.";
        }

        /// <summary>
        /// Crée le message SMS pour la facture
        /// Format: {nomSociete}: Votre facture de (12/2025) est de 15,000.00 FC. N FACT-2025-001.
        /// Format court sans URL pour économiser des caractères (75 caractères environ)
        /// Utilise "N" au lieu de "N°" pour éviter l'encodage Unicode (1 segment au lieu de 2)
        /// </summary>
        private string CreateFactureSmsText(
            string nomSociete, 
            string numeroFacture, 
            string montant, 
            string dateEmission, 
            string moisAnnee,
            string lienFacture,
            int idFacture)
        {
            // Format sans URL: {nomSociete}: Votre facture de (12/2025) est de 15,000.00 FC. N FACT-2025-001.
            // Longueur: ~75 caractères (très confortable sous la limite de 160)
            // Utilise "N" au lieu de "N°" pour éviter l'encodage Unicode (70 chars/segment)
            // Le client peut accéder à la facture via l'application (push, email, in-app)
            var message = $"{nomSociete}: Votre facture de ({moisAnnee}) est de {montant} FC. N {numeroFacture}.";
            
            return message;
        }
    }
}

