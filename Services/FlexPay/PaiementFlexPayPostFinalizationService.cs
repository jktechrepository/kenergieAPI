using Kenergie.Models;
using Kenergie.Services.Repositories;
using KenergieAPI.Services.Repositories;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services.FlexPay
{
    public interface IPaiementFlexPayPostFinalizationService
    {
        Task NotifyAfterFinalizationAsync(
            PaiementElectroniqueEnAttente pending,
            Paiement paiement);
    }

    /// <summary>
    /// Notifications post-finalisation FlexPay (SignalR, push client, audit).
    /// </summary>
    public class PaiementFlexPayPostFinalizationService : IPaiementFlexPayPostFinalizationService
    {
        private readonly PaiementNotificationService _paiementNotificationService;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ISignalRStatistiquesService _signalRStatistiquesService;
        private readonly IAuditService _auditService;
        private readonly ILogger<PaiementFlexPayPostFinalizationService> _logger;

        public PaiementFlexPayPostFinalizationService(
            PaiementNotificationService paiementNotificationService,
            ISignalRNotificationService signalRNotificationService,
            ISignalRStatistiquesService signalRStatistiquesService,
            IAuditService auditService,
            ILogger<PaiementFlexPayPostFinalizationService> logger)
        {
            _paiementNotificationService = paiementNotificationService;
            _signalRNotificationService = signalRNotificationService;
            _signalRStatistiquesService = signalRStatistiquesService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task NotifyAfterFinalizationAsync(
            PaiementElectroniqueEnAttente pending,
            Paiement paiement)
        {
            var societeId = pending.IdSociete;

            try
            {
                await _paiementNotificationService.NotifierPaiementAsync(paiement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur notification client FlexPay paiement {IdPaiement}", paiement.IdPaiement);
            }

            try
            {
                await _signalRNotificationService.NotifyPaiementElectroniqueStatusChangedAsync(
                    societeId,
                    pending.IdPaiementElectroniqueEnAttente,
                    StatutPaiementElectronique.Finalise,
                    paiement.IdPaiement);

                await _signalRNotificationService.NotifyNewPaiementAsync(societeId, new
                {
                    id = paiement.IdPaiement,
                    montant = paiement.MontantPaye,
                    date = paiement.DatePaiement,
                    mode = paiement.MethodePaiement,
                    statut = paiement.Statut,
                    estPaiementArriere = paiement.EstPaiementArriere,
                    idClient = paiement.IdClient,
                    idFacture = paiement.IdFacture,
                    idClientFacture = paiement.IdClientFacture,
                    source = "flexpay"
                });

                await _signalRStatistiquesService.NotifyStatistiquesStatusChangeAsync(
                    societeId, "paiement", paiement.IdPaiement, "créé");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur SignalR FlexPay paiement {IdPaiement}", paiement.IdPaiement);
            }

            try
            {
                var userId = pending.IdUtilisateur ?? 0;
                await _auditService.LogCreateAsync(
                    paiement,
                    userId,
                    userId > 0 ? "Utilisateur" : "FlexPay",
                    "FlexPay",
                    societeId,
                    commentaire: $"Paiement FlexPay finalisé (pending #{pending.IdPaiementElectroniqueEnAttente})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur audit FlexPay paiement {IdPaiement}", paiement.IdPaiement);
            }
        }
    }
}
