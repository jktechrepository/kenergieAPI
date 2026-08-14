using System.Globalization;
using System.Text.Json;
using Kenergie.Data;
using Kenergie.Helpers;
using Kenergie.Models;
using Kenergie.Models.Configuration;
using Kenergie.Models.DTOs.FlexPay;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kenergie.Services.FlexPay
{
    public interface IPaiementElectroniqueService
    {
        Task<PaiementElectroniquePendingDto> InitierAsync(InitierPaiementElectroniqueDto dto, int idSociete, int? idUtilisateur);
        Task<PaiementElectroniquePendingDto?> GetPendingAsync(int idPending, int? idSocieteFilter);
        Task<FlexPayCallbackResponseDto> ProcessCallbackAsync(
            FlexPayCallbackDto payload,
            string? payloadJson,
            string? headersJson,
            string? ip,
            bool fromVerifier = false,
            string? transactionStatusFromCheck = null);
        Task<FlexPayCallbackResponseDto> VerifierAsync(string orderNumber, int? idUtilisateur = null);
    }

    public class PaiementElectroniqueService : IPaiementElectroniqueService
    {
        private readonly KenergieDbContext _context;
        private readonly IFlexPayHttpService _flexPayHttp;
        private readonly IInfoPaiementSocieteService _infoPaiement;
        private readonly IPaiementRepository _paiementRepository;
        private readonly IPaiementFlexPayPostFinalizationService _postFinalizationService;
        private readonly FlexPayOptions _options;
        private readonly ILogger<PaiementElectroniqueService> _logger;

        public PaiementElectroniqueService(
            KenergieDbContext context,
            IFlexPayHttpService flexPayHttp,
            IInfoPaiementSocieteService infoPaiement,
            IPaiementRepository paiementRepository,
            IPaiementFlexPayPostFinalizationService postFinalizationService,
            IOptions<FlexPayOptions> options,
            ILogger<PaiementElectroniqueService> logger)
        {
            _context = context;
            _flexPayHttp = flexPayHttp;
            _infoPaiement = infoPaiement;
            _paiementRepository = paiementRepository;
            _postFinalizationService = postFinalizationService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<PaiementElectroniquePendingDto> InitierAsync(
            InitierPaiementElectroniqueDto dto,
            int idSociete,
            int? idUtilisateur)
        {
            if (!_options.Enabled)
                throw new InvalidOperationException("FlexPay est désactivé (FlexPay:Enabled=false).");

            var methode = MethodePaiementHelper.Normalize(dto.Methode);
            if (!MethodePaiementHelper.IsFlexPay(methode))
                throw new ArgumentException("Méthode invalide. Utilisez MOBILE_MONEY ou CARTE_BANCAIRE.");

            var marchand = await _infoPaiement.GetActiveEntityForSocieteAsync(idSociete)
                ?? throw new InvalidOperationException("Paiement électronique non configuré. Veuillez contacter l'administrateur.");

            if (methode == MethodeFlexPay.MobileMoney && !marchand.ActifMobileMoney)
                throw new InvalidOperationException("Mobile Money non activé pour ce marchand.");
            if (methode == MethodeFlexPay.CarteBancaire && !marchand.ActifCarteBancaire)
                throw new InvalidOperationException("Carte bancaire non activée pour ce marchand.");

            var (clientFacture, idClient, idFacture, montantDu, codeDeviseFacture) =
                await ResolveTargetAsync(dto);

            if (clientFacture.IdClient <= 0)
                throw new ArgumentException("Client introuvable pour cette facture.");

            // Vérifier que le client appartient à la société (via ClientFacture / usages)
            await EnsureClientSocieteAsync(idClient, idSociete);

            await EnsureClientSelfPaymentAsync(idUtilisateur, idClient, dto.IdClient);

            var montant = dto.Montant ?? montantDu;
            if (montant <= 0)
                throw new ArgumentException("Le montant doit être supérieur à 0.");
            if (montant > montantDu + 0.001m)
                throw new ArgumentException($"Le montant ({montant}) dépasse le montant dû ({montantDu}).");

            var codeDevise = DeviseConversionService.NormalizeCode(
                !string.IsNullOrWhiteSpace(dto.CodeDevisePaiement)
                    ? dto.CodeDevisePaiement!
                    : codeDeviseFacture);

            if (codeDevise != "CDF" && codeDevise != "USD")
                throw new ArgumentException("FlexPay n'accepte que CDF ou USD. La facture doit être en CDF/USD.");

            if (!string.Equals(codeDevise, DeviseConversionService.NormalizeCode(codeDeviseFacture), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"La devise de paiement ({codeDevise}) doit correspondre à la devise de la facture ({codeDeviseFacture}).");

            string? phone = null;
            if (methode == MethodeFlexPay.MobileMoney)
            {
                phone = MethodePaiementHelper.NormalizePhoneRdCongo(dto.Telephone);
                if (string.IsNullOrWhiteSpace(phone))
                    throw new ArgumentException("Téléphone Mobile Money obligatoire (format RDC, ex. 243900000000).");
            }

            var cleRessource = clientFacture.IdClientFacture > 0
                ? $"CF-{clientFacture.IdClientFacture}"
                : $"F-{idFacture}-C-{idClient}";

            await ExpireAndCheckHoldAsync(idSociete, cleRessource);

            var holdMinutes = Math.Max(1, _options.HoldMinutes);
            var holdExpire = DateTime.UtcNow.AddMinutes(holdMinutes);
            var reference = $"KE-{Guid.NewGuid():N}"[..19];

            var pending = new PaiementElectroniqueEnAttente
            {
                IdSociete = idSociete,
                IdClient = idClient,
                IdClientFacture = clientFacture.IdClientFacture > 0 ? clientFacture.IdClientFacture : null,
                IdFacture = idFacture,
                IdUtilisateur = idUtilisateur,
                Montant = Math.Round(montant, 2, MidpointRounding.AwayFromZero),
                CodeDevisePaiement = codeDevise,
                Methode = methode,
                Telephone = phone,
                Reference = reference,
                Statut = StatutPaiementElectronique.EnAttente,
                HoldExpireAt = holdExpire,
                DateCreation = DateTime.UtcNow
            };

            _context.PaiementsElectroniquesEnAttente.Add(pending);
            await _context.SaveChangesAsync();

            var hold = new PaiementHold
            {
                IdSociete = idSociete,
                CleRessource = cleRessource,
                Telephone = phone,
                IdPaiementElectroniqueEnAttente = pending.IdPaiementElectroniqueEnAttente,
                ExpireAt = holdExpire,
                DateCreation = DateTime.UtcNow,
                EstLibere = false
            };
            _context.PaiementHolds.Add(hold);
            await _context.SaveChangesAsync();

            var callbackUrl = FlexPayUrlHelper.ResolveCallbackUrl(_options.CallbackBaseUrl);
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new InvalidOperationException("FlexPay:CallbackBaseUrl non configuré.");

            FlexPayInitResult initResult;
            string typeFlexPay;
            if (methode == MethodeFlexPay.MobileMoney)
            {
                typeFlexPay = "1";
                initResult = await _flexPayHttp.InitierMobileMoneyAsync(
                    marchand.ApiToken,
                    marchand.CodeMarchand,
                    reference,
                    phone!,
                    pending.Montant,
                    codeDevise,
                    callbackUrl);
            }
            else
            {
                typeFlexPay = "2";
                initResult = await _flexPayHttp.InitierCarteAsync(
                    marchand.ApiToken,
                    marchand.CodeMarchand,
                    reference,
                    pending.Montant,
                    codeDevise,
                    $"Paiement facture client {idClient}",
                    callbackUrl,
                    FlexPayUrlHelper.ResolveApproveUrl(callbackUrl),
                    FlexPayUrlHelper.ResolveCancelUrl(callbackUrl),
                    FlexPayUrlHelper.ResolveDeclineUrl(callbackUrl));
            }

            pending.OrderNumber = initResult.OrderNumber;
            pending.PaymentUrl = initResult.PaymentUrl;

            if (!initResult.Accepted)
            {
                pending.Statut = StatutPaiementElectronique.Echec;
                pending.MessageErreur = initResult.Message;
                hold.EstLibere = true;
            }

            _context.TransactionsFlexPay.Add(new TransactionFlexPay
            {
                IdPaiementElectroniqueEnAttente = pending.IdPaiementElectroniqueEnAttente,
                IdSociete = idSociete,
                Reference = reference,
                OrderNumber = initResult.OrderNumber,
                TypeFlexPay = typeFlexPay,
                Montant = pending.Montant,
                CodeDevise = codeDevise,
                NombreCallbacks = 0,
                DateCreation = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            if (!initResult.Accepted)
                throw new InvalidOperationException($"FlexPay a refusé l'initiation: {initResult.Message}");

            return MapPending(pending, true, "Paiement électronique initié.");
        }

        public async Task<PaiementElectroniquePendingDto?> GetPendingAsync(int idPending, int? idSocieteFilter)
        {
            var q = _context.PaiementsElectroniquesEnAttente.AsNoTracking()
                .Where(p => p.IdPaiementElectroniqueEnAttente == idPending);
            if (idSocieteFilter.HasValue)
                q = q.Where(p => p.IdSociete == idSocieteFilter.Value);

            var pending = await q.FirstOrDefaultAsync();
            return pending == null ? null : MapPending(pending, !string.IsNullOrWhiteSpace(pending.OrderNumber), null);
        }

        public async Task<FlexPayCallbackResponseDto> ProcessCallbackAsync(
            FlexPayCallbackDto payload,
            string? payloadJson,
            string? headersJson,
            string? ip,
            bool fromVerifier = false,
            string? transactionStatusFromCheck = null)
        {
            payload.NormalizeFromRawJson(payloadJson);

            var audit = new CallbackFlexPay
            {
                OrderNumber = payload.OrderNumber,
                Reference = payload.Reference,
                Code = payload.Code,
                PayloadJson = payloadJson,
                HeadersJson = headersJson,
                IpAddress = ip,
                DateReception = DateTime.UtcNow
            };
            _context.CallbacksFlexPay.Add(audit);
            await _context.SaveChangesAsync();

            var pending = await FindPendingAsync(payload.OrderNumber, payload.Reference);
            if (pending == null)
            {
                audit.TraiteAvecSucces = false;
                audit.MessageTraitement = "Pending introuvable";
                await _context.SaveChangesAsync();
                return new FlexPayCallbackResponseDto { Success = false, Message = "Pending introuvable" };
            }

            var deltaSec = (DateTime.UtcNow - pending.DateCreation).TotalSeconds;
            _logger.LogInformation(
                "FlexPay callback reçu pending={IdPending} order={Order} code={Code} providerRef={ProviderRef} deltaSec={Delta:F2} fromVerifier={FromVerifier}",
                pending.IdPaiementElectroniqueEnAttente,
                payload.OrderNumber,
                payload.Code,
                string.IsNullOrWhiteSpace(payload.ProviderReference) ? "(absent)" : payload.ProviderReference,
                deltaSec,
                fromVerifier);

            await IncrementCallbacksAsync(pending.IdPaiementElectroniqueEnAttente);

            if (pending.IdPaiementFinalise.HasValue || pending.Statut == StatutPaiementElectronique.Finalise)
            {
                audit.TraiteAvecSucces = true;
                audit.MessageTraitement = "AlreadyProcessed";
                await _context.SaveChangesAsync();
                return new FlexPayCallbackResponseDto
                {
                    Success = true,
                    AlreadyProcessed = true,
                    Message = "Déjà finalisé",
                    IdPaiement = pending.IdPaiementFinalise
                };
            }

            if (payload.Code != "0")
            {
                pending.Statut = StatutPaiementElectronique.Echec;
                pending.MessageErreur = $"Callback code={payload.Code}";
                await LibererHoldAsync(pending.IdPaiementElectroniqueEnAttente);
                audit.TraiteAvecSucces = true;
                audit.MessageTraitement = "Échec paiement";
                await _context.SaveChangesAsync();
                return new FlexPayCallbackResponseDto { Success = false, Message = "Paiement refusé" };
            }

            // Fallback : code=0 sans providerReference → check FlexPay API
            if (!fromVerifier
                && string.IsNullOrWhiteSpace(payload.ProviderReference)
                && !string.IsNullOrWhiteSpace(payload.OrderNumber ?? pending.OrderNumber))
            {
                await TryEnrichProviderReferenceFromCheckAsync(payload, pending);
            }

            if (!IsPaymentConfirmed(
                    payload,
                    pending,
                    fromVerifier,
                    transactionStatusFromCheck,
                    deltaSec,
                    out var confirmationReason))
            {
                _logger.LogWarning(
                    "FlexPay callback ignoré (non confirmé) pending={IdPending} deltaSec={Delta:F2} reason={Reason}",
                    pending.IdPaiementElectroniqueEnAttente,
                    deltaSec,
                    confirmationReason);

                audit.TraiteAvecSucces = true;
                audit.MessageTraitement = $"CallbackIgnoredNotConfirmed:{confirmationReason}";
                await _context.SaveChangesAsync();
                return new FlexPayCallbackResponseDto
                {
                    Success = false,
                    Message = confirmationReason
                };
            }

            if (!TryValidateMontantCallback(payload, pending, out var montantErreur))
            {
                pending.Statut = StatutPaiementElectronique.Echec;
                pending.MessageErreur = montantErreur;
                await LibererHoldAsync(pending.IdPaiementElectroniqueEnAttente);
                audit.TraiteAvecSucces = false;
                audit.MessageTraitement = pending.MessageErreur;
                await _context.SaveChangesAsync();
                return new FlexPayCallbackResponseDto { Success = false, Message = pending.MessageErreur };
            }

            try
            {
                var paiement = await FinalizePaiementAsync(pending, payload.OrderNumber);
                pending.Statut = StatutPaiementElectronique.Finalise;
                pending.IdPaiementFinalise = paiement.IdPaiement;
                pending.DateFinalisation = DateTime.UtcNow;
                await LibererHoldAsync(pending.IdPaiementElectroniqueEnAttente);
                audit.TraiteAvecSucces = true;
                audit.MessageTraitement = $"Finalisé Paiement#{paiement.IdPaiement}";
                await _context.SaveChangesAsync();

                await _postFinalizationService.NotifyAfterFinalizationAsync(pending, paiement);

                return new FlexPayCallbackResponseDto
                {
                    Success = true,
                    AlreadyProcessed = false,
                    Message = "Paiement finalisé",
                    IdPaiement = paiement.IdPaiement
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur finalisation FlexPay pending={Id}", pending.IdPaiementElectroniqueEnAttente);
                audit.TraiteAvecSucces = false;
                audit.MessageTraitement = ex.Message;
                await _context.SaveChangesAsync();
                return new FlexPayCallbackResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<FlexPayCallbackResponseDto> VerifierAsync(string orderNumber, int? idUtilisateur = null)
        {
            var pending = await FindPendingAsync(orderNumber, null)
                ?? throw new KeyNotFoundException("Transaction / pending introuvable.");

            if (idUtilisateur.HasValue)
            {
                await EnsureClientOwnsPendingAsync(idUtilisateur.Value, pending.IdClient);
            }

            var marchand = await _infoPaiement.GetActiveEntityForSocieteAsync(pending.IdSociete)
                ?? throw new InvalidOperationException("Paiement électronique non configuré. Veuillez contacter l'administrateur.");

            var check = await _flexPayHttp.VerifierTransactionAsync(marchand.ApiToken, orderNumber);

            if (check.IsPending)
            {
                return new FlexPayCallbackResponseDto
                {
                    Success = false,
                    Message = "Transaction en attente de confirmation FlexPay"
                };
            }

            if (!check.IsConfirmed)
            {
                return new FlexPayCallbackResponseDto
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(check.Message)
                        ? "Transaction FlexPay non confirmée"
                        : check.Message
                };
            }

            var synthetic = new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Reference = check.Reference ?? pending.Reference,
                ProviderReference = check.ProviderReference,
                Amount = check.Amount ?? pending.Montant.ToString(CultureInfo.InvariantCulture),
                Currency = check.Currency ?? pending.CodeDevisePaiement
            };

            return await ProcessCallbackAsync(
                synthetic,
                check.RawJson,
                null,
                "verifier",
                fromVerifier: true,
                transactionStatusFromCheck: check.TransactionStatus);
        }

        /// <summary>
        /// Si le callback succès n'a pas de providerReference, interroge l'API check FlexPay.
        /// </summary>
        private async Task TryEnrichProviderReferenceFromCheckAsync(
            FlexPayCallbackDto payload,
            PaiementElectroniqueEnAttente pending)
        {
            try
            {
                var marchand = await _infoPaiement.GetActiveEntityForSocieteAsync(pending.IdSociete);
                if (marchand == null || string.IsNullOrWhiteSpace(marchand.ApiToken))
                    return;

                var orderNumber = payload.OrderNumber ?? pending.OrderNumber;
                if (string.IsNullOrWhiteSpace(orderNumber))
                    return;

                var check = await _flexPayHttp.VerifierTransactionAsync(marchand.ApiToken, orderNumber);
                if (!check.IsConfirmed)
                {
                    _logger.LogInformation(
                        "FlexPay enrich check non confirmé pending={Id} status={Status}",
                        pending.IdPaiementElectroniqueEnAttente,
                        check.TransactionStatus);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(check.ProviderReference))
                    payload.ProviderReference = check.ProviderReference;
                if (string.IsNullOrWhiteSpace(payload.Amount) && !string.IsNullOrWhiteSpace(check.Amount))
                    payload.Amount = check.Amount;
                if (string.IsNullOrWhiteSpace(payload.Currency) && !string.IsNullOrWhiteSpace(check.Currency))
                    payload.Currency = check.Currency;

                _logger.LogInformation(
                    "FlexPay enrich providerReference via check pending={Id} providerRef={Ref}",
                    pending.IdPaiementElectroniqueEnAttente,
                    payload.ProviderReference);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "FlexPay enrich check échoué pending={Id}",
                    pending.IdPaiementElectroniqueEnAttente);
            }
        }

        private async Task EnsureClientOwnsPendingAsync(int idUtilisateur, int idClientPending)
        {
            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUtilisateur == idUtilisateur);

            if (utilisateur?.Role?.Nom != "Client")
                return;

            if (!utilisateur.IdClient.HasValue || utilisateur.IdClient.Value != idClientPending)
                throw new UnauthorizedAccessException("Vous ne pouvez vérifier que vos propres paiements.");
        }

        private bool IsPaymentConfirmed(
            FlexPayCallbackDto payload,
            PaiementElectroniqueEnAttente pending,
            bool fromVerifier,
            string? transactionStatusFromCheck,
            double deltaSec,
            out string reason)
        {
            reason = string.Empty;

            if (fromVerifier)
            {
                if (FlexPayTransactionStatusHelper.IsConfirmed(transactionStatusFromCheck))
                    return true;

                reason = $"Statut FlexPay non confirmé: {transactionStatusFromCheck ?? "inconnu"}";
                return false;
            }

            if (_options.MinSecondsBeforeFinalize > 0 && deltaSec < _options.MinSecondsBeforeFinalize)
            {
                reason =
                    $"Callback reçu {deltaSec:F1}s après initiation (minimum {_options.MinSecondsBeforeFinalize}s)";
                return false;
            }

            if (pending.Methode == MethodeFlexPay.MobileMoney && _options.RequireProviderReferenceForMobileMoney)
            {
                if (string.IsNullOrWhiteSpace(payload.ProviderReference))
                {
                    reason = "ProviderReference absent (Mobile Money non confirmé côté opérateur)";
                    return false;
                }
            }

            if (pending.Methode == MethodeFlexPay.CarteBancaire)
            {
                if (string.IsNullOrWhiteSpace(payload.ProviderReference)
                    && string.IsNullOrWhiteSpace(payload.Channel))
                {
                    reason = "Confirmation carte insuffisante (providerReference/channel absent)";
                    return false;
                }
            }

            return true;
        }

        private async Task<Paiement> FinalizePaiementAsync(PaiementElectroniqueEnAttente pending, string? orderNumber)
        {
            var estArriere = pending.IdClientFacture.HasValue && !pending.IdFacture.HasValue;
            // Si IdFacture non renseigné mais ClientFacture système, lire IdFacture
            int? idFacture = pending.IdFacture;
            int? idClientFacture = pending.IdClientFacture;
            if (idClientFacture.HasValue)
            {
                var cf = await _context.ClientFactures.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdClientFacture == idClientFacture.Value);
                if (cf != null)
                {
                    if (!idFacture.HasValue && cf.IdFacture.HasValue)
                        idFacture = cf.IdFacture;
                    estArriere = cf.EstArrierePreExistant;
                }
            }

            var paiement = new Paiement
            {
                IdFacture = idFacture,
                IdClientFacture = idClientFacture,
                IdClient = pending.IdClient,
                MontantPaye = pending.Montant,
                DatePaiement = DateTime.Now,
                MethodePaiement = MethodePaiementHelper.ToDisplayMethode(pending.Methode),
                ReferenceTransaction = orderNumber ?? pending.OrderNumber ?? pending.Reference,
                Commentaire = $"FlexPay {pending.Reference}",
                Statut = "Validé",
                IdUtilisateur = pending.IdUtilisateur,
                EstPaiementArriere = estArriere,
                CodeDevisePaiement = pending.CodeDevisePaiement
            };

            return await _paiementRepository.CreateAsync(paiement);
        }

        /// <summary>
        /// Compare le montant FlexPay (montant marchand) à pending.Montant.
        /// Préfère <c>Amount</c> ; fallback <c>AmountCustomer</c> seulement si Amount absent.
        /// </summary>
        private bool TryValidateMontantCallback(
            FlexPayCallbackDto payload,
            PaiementElectroniqueEnAttente pending,
            out string messageErreur)
        {
            messageErreur = string.Empty;

            // Montant marchand d'abord (aligné sur l'init) — AmountCustomer peut inclure des frais opérateur
            var amountStr = !string.IsNullOrWhiteSpace(payload.Amount)
                ? payload.Amount
                : payload.AmountCustomer;

            if (string.IsNullOrWhiteSpace(amountStr))
                return true; // certains callbacks ne renvoient pas amount — on tolère

            if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) &&
                !decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.GetCultureInfo("fr-FR"), out amount))
            {
                messageErreur =
                    $"Montant callback illisible (reçu={amountStr}, attendu={pending.Montant.ToString(CultureInfo.InvariantCulture)})";
                return false;
            }

            var currency = DeviseConversionService.NormalizeCode(payload.Currency ?? pending.CodeDevisePaiement);
            if (!string.IsNullOrWhiteSpace(payload.Currency) &&
                !string.Equals(currency, pending.CodeDevisePaiement, StringComparison.OrdinalIgnoreCase))
            {
                messageErreur =
                    $"Devise callback différente (reçu={currency}, attendu={pending.CodeDevisePaiement})";
                return false;
            }

            var delta = Math.Abs(amount - pending.Montant);
            if (delta > _options.MontantTolerance)
            {
                messageErreur =
                    $"Écart de montant hors tolérance (attendu={pending.Montant.ToString(CultureInfo.InvariantCulture)}, " +
                    $"reçu={amount.ToString(CultureInfo.InvariantCulture)}, " +
                    $"delta={delta.ToString(CultureInfo.InvariantCulture)}, " +
                    $"tolérance={_options.MontantTolerance.ToString(CultureInfo.InvariantCulture)})";
                return false;
            }

            return true;
        }

        private async Task<(ClientFacture cf, int idClient, int? idFacture, decimal montantDu, string codeDevise)>
            ResolveTargetAsync(InitierPaiementElectroniqueDto dto)
        {
            if (dto.IdClientFacture.HasValue)
            {
                var cf = await _context.ClientFactures
                    .FirstOrDefaultAsync(c => c.IdClientFacture == dto.IdClientFacture.Value && c.Statut)
                    ?? throw new KeyNotFoundException("ClientFacture introuvable.");

                var montantDu = cf.MontantDu ?? 0;
                var devise = DeviseConversionService.NormalizeCode(cf.CodeDevisePrix ?? "CDF");
                return (cf, cf.IdClient, cf.IdFacture, montantDu, devise);
            }

            if (dto.IdFacture.HasValue && dto.IdClient.HasValue)
            {
                var cf = await _context.ClientFactures
                    .FirstOrDefaultAsync(c => c.IdFacture == dto.IdFacture && c.IdClient == dto.IdClient && c.Statut)
                    ?? throw new KeyNotFoundException("ClientFacture introuvable pour cette facture/client.");

                var montantDu = cf.MontantDu ?? 0;
                var devise = DeviseConversionService.NormalizeCode(cf.CodeDevisePrix ?? "CDF");
                return (cf, cf.IdClient, cf.IdFacture, montantDu, devise);
            }

            throw new ArgumentException("IdClientFacture ou (IdFacture + IdClient) requis.");
        }

        private async Task EnsureClientSocieteAsync(int idClient, int idSociete)
        {
            var ok = await _context.ClientUsages
                .AnyAsync(cu => cu.IdClient == idClient &&
                                cu.Usage != null &&
                                cu.Usage.CategorieClient != null &&
                                cu.Usage.CategorieClient.IdSociete == idSociete);
            if (!ok)
            {
                // fallback via Axe/Cabine
                ok = await _context.Clients
                    .AnyAsync(c => c.IdClient == idClient &&
                                   c.Axe != null &&
                                   c.Axe.Cabine != null &&
                                   c.Axe.Cabine.IdSociete == idSociete);
            }
            if (!ok)
                throw new UnauthorizedAccessException("Le client n'appartient pas à votre société.");
        }

        /// <summary>
        /// Un utilisateur avec le rôle Client ne peut payer que ses propres factures.
        /// </summary>
        private async Task EnsureClientSelfPaymentAsync(int? idUtilisateur, int idClientCible, int? idClientDansDto)
        {
            if (!idUtilisateur.HasValue)
                return;

            var utilisateur = await _context.Utilisateurs
                .Include(u => u.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdUtilisateur == idUtilisateur.Value);

            if (utilisateur?.Role?.Nom != "Client")
                return;

            if (!utilisateur.IdClient.HasValue)
                throw new UnauthorizedAccessException("Compte client invalide.");

            if (idClientDansDto.HasValue && idClientDansDto.Value != utilisateur.IdClient.Value)
                throw new UnauthorizedAccessException("Vous ne pouvez payer que vos propres factures.");

            if (idClientCible != utilisateur.IdClient.Value)
                throw new UnauthorizedAccessException("Vous ne pouvez payer que vos propres factures.");
        }

        private async Task ExpireAndCheckHoldAsync(int idSociete, string cleRessource)
        {
            var now = DateTime.UtcNow;
            var holds = await _context.PaiementHolds
                .Where(h => h.IdSociete == idSociete && h.CleRessource == cleRessource && !h.EstLibere)
                .ToListAsync();

            foreach (var h in holds)
            {
                if (h.ExpireAt <= now)
                {
                    h.EstLibere = true;
                    if (h.IdPaiementElectroniqueEnAttente.HasValue)
                    {
                        var p = await _context.PaiementsElectroniquesEnAttente
                            .FirstOrDefaultAsync(x => x.IdPaiementElectroniqueEnAttente == h.IdPaiementElectroniqueEnAttente.Value);
                        if (p != null && p.Statut == StatutPaiementElectronique.EnAttente)
                        {
                            p.Statut = StatutPaiementElectronique.Expire;
                            p.MessageErreur = "Hold expiré";
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "Un paiement électronique est déjà en attente pour cette facture. Réessayez après expiration du hold.");
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task LibererHoldAsync(int idPending)
        {
            var holds = await _context.PaiementHolds
                .Where(h => h.IdPaiementElectroniqueEnAttente == idPending && !h.EstLibere)
                .ToListAsync();
            foreach (var h in holds)
                h.EstLibere = true;
        }

        private async Task IncrementCallbacksAsync(int idPending)
        {
            var tx = await _context.TransactionsFlexPay
                .FirstOrDefaultAsync(t => t.IdPaiementElectroniqueEnAttente == idPending);
            if (tx != null)
                tx.NombreCallbacks++;
        }

        private async Task<PaiementElectroniqueEnAttente?> FindPendingAsync(string? orderNumber, string? reference)
        {
            if (!string.IsNullOrWhiteSpace(orderNumber))
            {
                var byOrder = await _context.PaiementsElectroniquesEnAttente
                    .FirstOrDefaultAsync(p => p.OrderNumber == orderNumber);
                if (byOrder != null) return byOrder;
            }

            if (!string.IsNullOrWhiteSpace(reference))
            {
                return await _context.PaiementsElectroniquesEnAttente
                    .FirstOrDefaultAsync(p => p.Reference == reference);
            }

            return null;
        }

        private static PaiementElectroniquePendingDto MapPending(
            PaiementElectroniqueEnAttente p,
            bool accepted,
            string? message) => new()
        {
            IdPending = p.IdPaiementElectroniqueEnAttente,
            OrderNumberFlexPay = p.OrderNumber,
            ReferenceFlexPay = p.Reference,
            MontantFlexPay = p.Montant,
            CodeDevisePaiement = p.CodeDevisePaiement,
            Methode = p.Methode,
            Statut = p.Statut,
            HoldExpireAt = p.HoldExpireAt,
            PaymentUrl = p.PaymentUrl,
            FlexPayAccepted = accepted,
            IdPaiementFinalise = p.IdPaiementFinalise,
            EstConfirme = p.Statut == StatutPaiementElectronique.Finalise,
            DateFinalisation = p.DateFinalisation,
            Message = message ?? p.MessageErreur
        };
    }
}
