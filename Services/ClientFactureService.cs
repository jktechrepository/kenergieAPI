using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.ClientFacture;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour la gestion des ClientFacture
    /// </summary>
    public class ClientFactureService : IClientFactureRepository
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<ClientFactureService> _logger;

        public ClientFactureService(KenergieDbContext context, ILogger<ClientFactureService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ClientFacture?> GetByIdAsync(int idClientFacture)
        {
            return await _context.ClientFactures
                .Include(cf => cf.Client)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Where(cf => cf.Statut == true)
                .FirstOrDefaultAsync(cf => cf.IdClientFacture == idClientFacture);
        }

        public async Task<ClientFacture> CreateAsync(ClientFacture clientFacture)
        {
            clientFacture.DateCreation = DateTime.Now;
            
            // Calculer MontantDu si Montant et MontantPaye sont fournis
            if (clientFacture.Montant.HasValue && clientFacture.MontantPaye.HasValue)
            {
                clientFacture.MontantDu = clientFacture.Montant.Value - clientFacture.MontantPaye.Value;
            }
            else if (clientFacture.Montant.HasValue && !clientFacture.MontantPaye.HasValue)
            {
                clientFacture.MontantPaye = 0;
                clientFacture.MontantDu = clientFacture.Montant.Value;
            }

            _context.ClientFactures.Add(clientFacture);
            await _context.SaveChangesAsync();
            return clientFacture;
        }

        public async Task<ClientFacture?> UpdateAsync(ClientFacture clientFacture)
        {
            var existing = await _context.ClientFactures.FindAsync(clientFacture.IdClientFacture);
            if (existing == null)
                return null;

            // Mettre à jour DateModification
            clientFacture.DateModification = DateTime.Now;

            // Recalculer MontantDu si nécessaire
            if (clientFacture.Montant.HasValue && clientFacture.MontantPaye.HasValue)
            {
                clientFacture.MontantDu = clientFacture.Montant.Value - clientFacture.MontantPaye.Value;
            }

            _context.Entry(existing).CurrentValues.SetValues(clientFacture);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int idClientFacture)
        {
            var clientFacture = await _context.ClientFactures.FindAsync(idClientFacture);
            if (clientFacture == null)
                return false;

            // Soft delete : mettre Statut à false
            clientFacture.Statut = false;
            clientFacture.DateModification = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int idClientFacture)
        {
            return await _context.ClientFactures
                .AnyAsync(cf => cf.IdClientFacture == idClientFacture && cf.Statut == true);
        }

        public async Task<IEnumerable<ClientFacture>> GetByClientAsync(int idClient)
        {
            return await _context.ClientFactures
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Where(cf => cf.IdClient == idClient && cf.Statut == true)
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ThenByDescending(cf => cf.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientFacture>> GetByClientWithArrieresAsync(int idClient)
        {
            return await _context.ClientFactures
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Where(cf => cf.IdClient == idClient && 
                            cf.Statut == true && 
                            cf.MontantDu.HasValue && 
                            cf.MontantDu.Value > 0)
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientFacture>> GetAllArrieresAsync()
        {
            return await _context.ClientFactures
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Include(cf => cf.Client) // Inclure le client pour avoir les informations complètes
                .Where(cf => cf.Statut == true && 
                            cf.MontantDu.HasValue && 
                            cf.MontantDu.Value > 0)
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientFacture>> GetPreExistantsByClientAsync(int idClient)
        {
            return await _context.ClientFactures
                .Where(cf => cf.IdClient == idClient && 
                            cf.Statut == true && 
                            cf.EstArrierePreExistant == true)
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientFacture>> GetByFactureAsync(int idFacture)
        {
            return await _context.ClientFactures
                .Include(cf => cf.Client)
                .Where(cf => cf.IdFacture == idFacture && cf.Statut == true)
                .OrderByDescending(cf => cf.DateCreation)
                .ToListAsync();
        }

        public async Task<ClientFacture?> GetByClientAndFactureAsync(int idClient, int idFacture)
        {
            return await _context.ClientFactures
                .Include(cf => cf.Client)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .FirstOrDefaultAsync(cf => cf.IdClient == idClient && 
                                          cf.IdFacture == idFacture && 
                                          cf.Statut == true);
        }

        public async Task<IEnumerable<ClientFacture>> GetByClientAndMoisAnneeAsync(int idClient, string mois, int annee)
        {
            // ✨ NORMALISATION DU MOIS : Accepter les formats "1" et "01"
            var moisNormalise = NormaliserMois(mois.Trim());
            
            return await _context.ClientFactures
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Where(cf => cf.IdClient == idClient && 
                            cf.Mois == moisNormalise && 
                            cf.Annees == annee && 
                            cf.Statut == true)
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientFacture>> GetBySocieteAnneeMoisWithArrieresAsync(int idSociete, int annees, string mois)
        {
            // ✨ NORMALISATION DU MOIS : Accepter les formats "1" et "01"
            var moisNormalise = NormaliserMois(mois.Trim());
            
            return await _context.ClientFactures
                .Include(cf => cf.Client)
                    .ThenInclude(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Where(cf => 
                    // Filtre par statut actif
                    cf.Statut == true &&
                    // Filtre par année
                    cf.Annees == annees &&
                    // Filtre par mois (avec format normalisé)
                    cf.Mois == moisNormalise &&
                    // Filtre : MontantDu > MontantPaye (montant dû supérieur au montant payé)
                    cf.MontantDu.HasValue &&
                    cf.MontantPaye.HasValue &&
                    cf.MontantDu.Value > cf.MontantPaye.Value &&
                    // Filtre par société : deux chemins possibles
                    (
                        // Chemin 1 : Via Facture -> Usage -> CategorieClient -> Societe (pour factures système)
                        (cf.IdFacture != null && 
                         cf.Facture != null && 
                         cf.Facture.Usage != null && 
                         cf.Facture.Usage.CategorieClient != null && 
                         cf.Facture.Usage.CategorieClient.IdSociete == idSociete) ||
                        // Chemin 2 : Via Client -> Axe -> Cabine -> Societe (pour arriérés pré-existants)
                        (cf.IdFacture == null && 
                         cf.Client != null && 
                         cf.Client.Axe != null && 
                         cf.Client.Axe.Cabine != null && 
                         cf.Client.Axe.Cabine.IdSociete == idSociete)
                    ))
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ThenByDescending(cf => cf.DateCreation)
                .ToListAsync();
        }

        // ✨ NOUVEAU : Statistiques consolidées par société, année et mois
        /// <summary>
        /// Récupère les factures avec arriérés d'une société pour une période donnée avec statistiques consolidées
        /// </summary>
        public async Task<ClientFactureConsolideDto> GetBySocieteAnneeMoisWithStatsAsync(int idSociete, int annees, string mois)
        {
            // ✨ NORMALISATION DU MOIS : Accepter les formats "1" et "01"
            var moisNormalise = NormaliserMois(mois.Trim());
            
            // Récupérer les factures avec arriérés pour la période
            var clientFactures = await _context.ClientFactures
                .Include(cf => cf.Client)
                    .ThenInclude(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Where(cf => 
                    // Filtre par statut actif
                    cf.Statut == true &&
                    // Filtre par année
                    cf.Annees == annees &&
                    // Filtre par mois (avec format normalisé)
                    cf.Mois == moisNormalise &&
                    // Filtre : MontantDu > MontantPaye (montant dû supérieur au montant payé)
                    cf.MontantDu.HasValue &&
                    cf.MontantPaye.HasValue &&
                    cf.MontantDu.Value > cf.MontantPaye.Value &&
                    // Filtre par société : deux chemins possibles
                    (
                        // Chemin 1 : Via Facture -> Usage -> CategorieClient -> Societe (pour factures système)
                        (cf.IdFacture != null && 
                         cf.Facture != null && 
                         cf.Facture.Usage != null && 
                         cf.Facture.Usage.CategorieClient != null && 
                         cf.Facture.Usage.CategorieClient.IdSociete == idSociete) ||
                        // Chemin 2 : Via Client -> Axe -> Cabine -> Societe (pour arriérés pré-existants)
                        (cf.IdFacture == null && 
                         cf.Client != null && 
                         cf.Client.Axe != null && 
                         cf.Client.Axe.Cabine != null && 
                         cf.Client.Axe.Cabine.IdSociete == idSociete)
                    ))
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ThenByDescending(cf => cf.DateCreation)
                .ToListAsync();

            // Calculer les statistiques
            var totalGeneral = clientFactures
                .Where(cf => cf.MontantDu.HasValue)
                .Sum(cf => cf.MontantDu.Value);

            var nombreTotalClients = clientFactures
                .Select(cf => cf.IdClient)
                .Distinct()
                .Count();

            var nombreTotalFactures = clientFactures.Count;

            // Convertir en DTOs (optimisé)
            var facturesDto = clientFactures
                .Select(cf => ConvertToDtoOptimized(cf))
                .ToList();

            // Retourner le DTO consolidé
            return new ClientFactureConsolideDto
            {
                TotalGeneral = totalGeneral,
                NombreTotalClients = nombreTotalClients,
                NombreTotalFactures = nombreTotalFactures,
                Factures = facturesDto
            };
        }

        /// <summary>
        /// Normalise le mois pour accepter les formats "1" et "01"
        /// </summary>
        /// <param name="mois">Mois en entrée (ex: "1", "01", "Janvier", etc.)</param>
        /// <returns>Mois normalisé au format "01", "02", ..., "12"</returns>
        private static string NormaliserMois(string mois)
        {
            if (string.IsNullOrWhiteSpace(mois))
                return mois;

            // Si c'est déjà au format "01", "02", etc., le retourner tel quel
            if (mois.Length == 2 && char.IsDigit(mois[0]) && char.IsDigit(mois[1]))
                return mois;

            // Si c'est un chiffre simple "1", "2", ..., "9", le convertir en "01", "02", ..., "09"
            if (mois.Length == 1 && char.IsDigit(mois[0]))
            {
                var moisNum = int.Parse(mois);
                if (moisNum >= 1 && moisNum <= 9)
                    return $"0{moisNum}";
            }

            // Si c'est "10", "11", "12", le retourner tel quel
            if (mois.Length == 2 && char.IsDigit(mois[0]) && char.IsDigit(mois[1]))
            {
                var moisNum = int.Parse(mois);
                if (moisNum >= 10 && moisNum <= 12)
                    return mois;
            }

            // Sinon, retourner la valeur originale (pour les noms de mois comme "Janvier")
            return mois;
        }

        private static (string MoisNormalise, int Annee) GetMoisPrecedentCalendaire(DateTime reference)
        {
            var moisPrecedent = reference.Month == 1 ? 12 : reference.Month - 1;
            var annee = reference.Month == 1 ? reference.Year - 1 : reference.Year;
            return (NormaliserMois(moisPrecedent.ToString()), annee);
        }

        private const int AnneeRelanceMin = 2000;
        private const int AnneeRelanceMax = 2100;

        /// <summary>
        /// Résout la période de relance pour la sélection des clients (moisFacturePrecedentSeulement=true).
        /// Sans param : M-1 calendaire. mois seul : année courante. annee sans mois : ArgumentException.
        /// </summary>
        private static (string MoisNormalise, string MoisSansZero, int Annee) ResolvePeriodeRelance(
            string? mois,
            int? annee,
            DateTime reference)
        {
            if (string.IsNullOrWhiteSpace(mois))
            {
                if (annee.HasValue)
                    throw new ArgumentException("Le paramètre mois est requis lorsque annee est fourni.");

                var (moisM1, anneeM1) = GetMoisPrecedentCalendaire(reference);
                var numeroM1 = GetNumeroMois(moisM1);
                return (moisM1, numeroM1.ToString(), anneeM1);
            }

            if (!TryParseMoisNumero(mois, out var moisNumero))
                throw new ArgumentException($"Mois invalide : '{mois}'. Valeur attendue entre 1 et 12.");

            var moisNormalise = NormaliserMois(mois.Trim());
            var moisSansZero = moisNumero.ToString();
            var anneeResolue = annee ?? reference.Year;

            if (annee.HasValue && (annee.Value < AnneeRelanceMin || annee.Value > AnneeRelanceMax))
                throw new ArgumentException(
                    $"Année invalide : {annee.Value}. Valeur attendue entre {AnneeRelanceMin} et {AnneeRelanceMax}.");

            return (moisNormalise, moisSansZero, anneeResolue);
        }

        private static bool TryParseMoisNumero(string mois, out int numero)
        {
            numero = 0;
            if (string.IsNullOrWhiteSpace(mois))
                return false;

            var normalise = NormaliserMois(mois.Trim());
            if (!int.TryParse(normalise, out numero))
                return false;

            return numero is >= 1 and <= 12;
        }

        private static int GetNumeroMois(string mois)
        {
            return int.TryParse(NormaliserMois(mois), out var numero) ? numero : 0;
        }

        private static decimal CalculerDetteAnterieur(IEnumerable<ArriereParPeriodeDto> arrieresParPeriode)
        {
            var periodes = arrieresParPeriode.ToList();
            if (periodes.Count <= 1)
                return 0m;

            var derniere = periodes
                .OrderByDescending(p => p.Annees)
                .ThenByDescending(p => GetNumeroMois(p.Mois))
                .First();

            return periodes
                .Where(p => !(NormaliserMois(p.Mois) == NormaliserMois(derniere.Mois) && p.Annees == derniere.Annees))
                .Sum(p => p.MontantDuTotal);
        }

        public async Task<ClientFacture> CreatePreExistantAsync(int idClient, decimal montant, string mois, int annees, string? description = null, DateTime? dateEmission = null)
        {
            var clientFacture = new ClientFacture
            {
                IdClient = idClient,
                IdFacture = null, // NULL pour arriéré pré-existant
                Montant = montant,
                MontantPaye = 0,
                MontantDu = montant, // Tout le montant est dû
                Mois = mois,
                Annees = annees,
                DateEmission = dateEmission ?? DateTime.Now,
                EstArrierePreExistant = true,
                Description = description,
                Statut = true,
                DateCreation = DateTime.Now
            };

            return await CreateAsync(clientFacture);
        }

        public async Task<bool> UpdateMontantPayeAsync(int idClientFacture, decimal montantPaye)
        {
            var clientFacture = await _context.ClientFactures.FindAsync(idClientFacture);
            if (clientFacture == null)
                return false;

            clientFacture.MontantPaye = montantPaye;
            clientFacture.DateModification = DateTime.Now;

            // Recalculer MontantDu
            if (clientFacture.Montant.HasValue)
            {
                clientFacture.MontantDu = clientFacture.Montant.Value - montantPaye;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RecalculateMontantDuAsync(int idClientFacture)
        {
            var clientFacture = await _context.ClientFactures.FindAsync(idClientFacture);
            if (clientFacture == null)
                return false;

            if (clientFacture.Montant.HasValue && clientFacture.MontantPaye.HasValue)
            {
                clientFacture.MontantDu = clientFacture.Montant.Value - clientFacture.MontantPaye.Value;
                clientFacture.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        /// <summary>
        /// ✨ NOUVEAU : Récupère les factures d'un client groupées par période (mois/année) avec totaux consolidés
        /// </summary>
        public async Task<ClientFacturesConsolideesResponseDto> GetClientFacturesConsolideesAsync(int idClient)
        {
            // Récupérer toutes les ClientFacture du client
            var clientFactures = await _context.ClientFactures
                .Include(cf => cf.Client)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Where(cf => cf.IdClient == idClient && cf.Statut == true)
                .OrderByDescending(cf => cf.Annees)
                .ThenByDescending(cf => cf.Mois)
                .ThenByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();

            // Récupérer les informations du client
            var clientInfo = clientFactures.Any() 
                ? clientFactures.First().Client 
                : await _context.Clients.FindAsync(idClient);

            if (!clientFactures.Any())
            {
                return new ClientFacturesConsolideesResponseDto
                {
                    IdClient = idClient,
                    NomClient = clientInfo?.NomClient,
                    CodeCons = clientInfo?.CodeCons
                };
            }

            // Grouper par période (Mois/Annees)
            var groupedByPeriode = clientFactures
                .Where(cf => !string.IsNullOrWhiteSpace(cf.Mois) && cf.Annees.HasValue)
                .GroupBy(cf => new { cf.Mois, cf.Annees })
                .ToList();

            var facturesConsolidees = new List<ClientFactureConsolideeDto>();

            foreach (var groupe in groupedByPeriode)
            {
                var facturesDuGroupe = groupe.ToList();

                // Convertir les ClientFacture en DTOs (optimisé)
                var detailFactures = facturesDuGroupe
                    .Select(cf => ConvertToDtoOptimized(cf))
                    .ToList();

                var consolidee = new ClientFactureConsolideeDto
                {
                    Mois = groupe.Key.Mois ?? "",
                    Annees = groupe.Key.Annees ?? 0,
                    DateEmission = facturesDuGroupe
                        .Where(cf => cf.DateEmission.HasValue)
                        .OrderByDescending(cf => cf.DateEmission)
                        .FirstOrDefault()?.DateEmission,

                    // Totaux consolidés
                    MontantTotal = facturesDuGroupe
                        .Where(cf => cf.Montant.HasValue)
                        .Sum(cf => cf.Montant.Value),
                    MontantPayeTotal = facturesDuGroupe
                        .Where(cf => cf.MontantPaye.HasValue)
                        .Sum(cf => cf.MontantPaye.Value),
                    MontantDuTotal = facturesDuGroupe
                        .Where(cf => cf.MontantDu.HasValue)
                        .Sum(cf => cf.MontantDu.Value),

                    // Détail
                    DetailFactures = detailFactures,

                    // Informations client
                    IdClient = idClient,
                    NomClient = clientInfo?.NomClient,
                    CodeCons = clientInfo?.CodeCons,

                    // Statistiques
                    NombreFactures = facturesDuGroupe.Count,
                    NombreUsages = facturesDuGroupe
                        .Where(cf => cf.Facture?.Usage != null)
                        .Select(cf => cf.Facture.Usage.IdUsage)
                        .Distinct()
                        .Count()
                };

                facturesConsolidees.Add(consolidee);
            }

            // Calculer les totaux globaux
            var montantTotalGlobal = facturesConsolidees.Sum(f => f.MontantTotal);
            var montantPayeTotalGlobal = facturesConsolidees.Sum(f => f.MontantPayeTotal);
            var montantDuTotalGlobal = facturesConsolidees.Sum(f => f.MontantDuTotal);
            var nombreTotalFactures = clientFactures.Count;
            var nombreTotalPeriodes = facturesConsolidees.Count;

            // Construire la réponse avec totaux globaux (ordre réorganisé : totaux globaux juste après codeCons)
            return new ClientFacturesConsolideesResponseDto
            {
                IdClient = idClient,
                NomClient = clientInfo?.NomClient,
                CodeCons = clientInfo?.CodeCons,
                // Totaux globaux (placés juste après codeCons)
                MontantTotalGlobal = montantTotalGlobal,
                MontantPayeTotalGlobal = montantPayeTotalGlobal,
                MontantDuTotalGlobal = montantDuTotalGlobal,
                NombreTotalFactures = nombreTotalFactures,
                NombreTotalPeriodes = nombreTotalPeriodes,
                // Liste des factures consolidées (placée à la fin)
                FacturesConsolidees = facturesConsolidees
            };
        }

        /// <summary>
        /// ✨ NOUVEAU : Récupère la facture consolidée d'un client pour une période spécifique
        /// </summary>
        public async Task<ClientFactureConsolideeDto?> GetClientFactureConsolideeByPeriodeAsync(int idClient, string mois, int annee)
        {
            // ✨ NORMALISATION DU MOIS : Accepter les formats "1" et "01"
            var moisNormalise = NormaliserMois(mois.Trim());
            
            // Récupérer les ClientFacture du client pour cette période
            var clientFactures = await _context.ClientFactures
                .Include(cf => cf.Client)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Where(cf => cf.IdClient == idClient &&
                            cf.Mois == moisNormalise &&
                            cf.Annees == annee &&
                            cf.Statut == true)
                .OrderByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();

            if (!clientFactures.Any())
            {
                return null;
            }

            // Convertir les ClientFacture en DTOs
            var detailFactures = new List<ClientFactureDto>();
            foreach (var cf in clientFactures)
            {
                detailFactures.Add(ConvertToDtoOptimized(cf));
            }

            var clientInfo = clientFactures.First().Client;

            // ✨ NOUVEAU : Récupérer la liste des périodes disponibles pour ce client
            // (uniquement les périodes avec arriérés : MontantDu > 0)
            var periodesDisponibles = await _context.ClientFactures
                .Where(cf => cf.IdClient == idClient &&
                             cf.Statut == true &&
                             cf.MontantDu.HasValue &&
                             cf.MontantDu.Value > 0 &&
                             !string.IsNullOrWhiteSpace(cf.Mois) &&
                             cf.Annees.HasValue)
                .Select(cf => new { Mois = cf.Mois!, Annees = cf.Annees!.Value })
                .Distinct()
                .OrderByDescending(p => p.Annees)
                .ThenByDescending(p => p.Mois)
                .ToListAsync();

            return new ClientFactureConsolideeDto
            {
                Mois = mois,
                Annees = annee,
                DateEmission = clientFactures
                    .Where(cf => cf.DateEmission.HasValue)
                    .OrderByDescending(cf => cf.DateEmission)
                    .FirstOrDefault()?.DateEmission,

                // Totaux consolidés
                MontantTotal = clientFactures
                    .Where(cf => cf.Montant.HasValue)
                    .Sum(cf => cf.Montant.Value),
                MontantPayeTotal = clientFactures
                    .Where(cf => cf.MontantPaye.HasValue)
                    .Sum(cf => cf.MontantPaye.Value),
                MontantDuTotal = clientFactures
                    .Where(cf => cf.MontantDu.HasValue)
                    .Sum(cf => cf.MontantDu.Value),

                // Détail
                DetailFactures = detailFactures,

                // Informations client
                IdClient = idClient,
                NomClient = clientInfo?.NomClient,
                CodeCons = clientInfo?.CodeCons,

                // Statistiques
                NombreFactures = clientFactures.Count,
                NombreUsages = clientFactures
                    .Where(cf => cf.Facture?.Usage != null)
                    .Select(cf => cf.Facture.Usage.IdUsage)
                    .Distinct()
                    .Count(),

                // ✨ NOUVEAU : périodes disponibles
                PeriodesDisponibles = periodesDisponibles
                    .Select(p => new PeriodeClientFactureDto { Mois = p.Mois, Annees = p.Annees })
                    .ToList()
            };
        }

        /// <summary>
        /// Convertit une ClientFacture en DTO avec les informations supplémentaires
        /// 🚀 OPTIMISÉ : Évite les N+1 queries en utilisant les données déjà chargées
        /// </summary>
        private ClientFactureDto ConvertToDtoOptimized(ClientFacture clientFacture)
        {
            var dto = new ClientFactureDto
            {
                IdClientFacture = clientFacture.IdClientFacture,
                IdFacture = clientFacture.IdFacture,
                IdClient = clientFacture.IdClient,
                Montant = clientFacture.Montant,
                nombreBatiment = clientFacture.nombreBatiment,
                MontantPaye = clientFacture.MontantPaye,
                MontantDu = clientFacture.MontantDu,
                Mois = clientFacture.Mois,
                Annees = clientFacture.Annees,
                DateEmission = clientFacture.DateEmission,
                EstArrierePreExistant = clientFacture.EstArrierePreExistant,
                Description = clientFacture.Description,
                Statut = clientFacture.Statut,
                DateCreation = clientFacture.DateCreation,
                DateModification = clientFacture.DateModification
            };

            // 🚀 OPTIMISATION: Utiliser les données déjà chargées via Include
            // Plus besoin de requêtes supplémentaires
            dto.NomClient = clientFacture.Client?.NomClient;
            dto.NumeroFacture = clientFacture.Facture?.NumeroFacture;
            dto.LibelleUsage = clientFacture.Facture?.Usage?.Libelle;
            dto.IdTypeDeCourant = clientFacture.Facture?.IdTypeDeCourant;
            dto.TypeDeCourant = clientFacture.Facture?.TypeDeCourant?.Libelle;

            return dto;
        }

        /// <summary>
        /// ✨ NOUVEAU : Récupère les arriérés d'un client groupés par période (mois/année) avec totaux consolidés
        /// Seules les factures avec MontantDu > 0 sont incluses
        /// </summary>
        public async Task<ArrieresConsolidesResponseDto> GetArrieresConsolidesByClientAsync(int idClient)
        {
            // Récupérer toutes les ClientFacture du client avec arriérés (MontantDu > 0)
            var clientFactures = await _context.ClientFactures
                .Include(cf => cf.Client)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.TypeDeCourant)
                .Where(cf => cf.IdClient == idClient && 
                            cf.Statut == true && 
                            cf.MontantDu.HasValue && 
                            cf.MontantDu.Value > 0)
                .OrderByDescending(cf => cf.Annees)
                .ThenByDescending(cf => cf.Mois)
                .ThenByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();

            // Récupérer les informations du client
            var clientInfo = clientFactures.Any() 
                ? clientFactures.First().Client 
                : await _context.Clients.FindAsync(idClient);

            if (!clientFactures.Any())
            {
                return new ArrieresConsolidesResponseDto
                {
                    IdClient = idClient,
                    NomClient = clientInfo?.NomClient,
                    CodeCons = clientInfo?.CodeCons,
                    TotalGeneral = 0,
                    NombreTotalFactures = 0,
                    NombreTotalPeriodes = 0,
                    PeriodesDisponibles = new List<PeriodeClientFactureDto>()
                };
            }

            // Grouper par période (Mois/Annees)
            var groupedByPeriode = clientFactures
                .Where(cf => !string.IsNullOrWhiteSpace(cf.Mois) && cf.Annees.HasValue)
                .GroupBy(cf => new { cf.Mois, cf.Annees })
                .ToList();

            var arrieresParPeriode = new List<ArriereParPeriodeDto>();

            foreach (var groupe in groupedByPeriode)
            {
                var facturesDuGroupe = groupe.ToList();

                // Convertir les ClientFacture en DTOs (optimisé)
                var detailFactures = facturesDuGroupe
                    .Select(cf => ConvertToDtoOptimized(cf))
                    .ToList();

                var arriereParPeriode = new ArriereParPeriodeDto
                {
                    Mois = groupe.Key.Mois ?? "",
                    Annees = groupe.Key.Annees ?? 0,
                    DateEmission = facturesDuGroupe
                        .Where(cf => cf.DateEmission.HasValue)
                        .OrderByDescending(cf => cf.DateEmission)
                        .FirstOrDefault()?.DateEmission,

                    // Totaux consolidés
                    MontantTotal = facturesDuGroupe
                        .Where(cf => cf.Montant.HasValue)
                        .Sum(cf => cf.Montant.Value),
                    MontantPayeTotal = facturesDuGroupe
                        .Where(cf => cf.MontantPaye.HasValue)
                        .Sum(cf => cf.MontantPaye.Value),
                    MontantDuTotal = facturesDuGroupe
                        .Where(cf => cf.MontantDu.HasValue)
                        .Sum(cf => cf.MontantDu.Value),

                    // Détail
                    DetailFactures = detailFactures,

                    // Statistiques
                    NombreFactures = facturesDuGroupe.Count,
                    NombreUsages = facturesDuGroupe
                        .Where(cf => cf.Facture?.Usage != null)
                        .Select(cf => cf.Facture.Usage.IdUsage)
                        .Distinct()
                        .Count()
                };

                arrieresParPeriode.Add(arriereParPeriode);
            }

            // Calculer le total général (somme de tous les montantDuTotal)
            var totalGeneral = arrieresParPeriode.Sum(a => a.MontantDuTotal);
            var nombreTotalFactures = clientFactures.Count; // Nombre total de ClientFacture avec arriérés
            var nombreTotalPeriodes = arrieresParPeriode.Count; // Nombre de périodes distinctes

            // ✨ NOUVEAU : Périodes disponibles (uniquement celles avec arriérés)
            // On les déduit directement des résultats déjà filtrés (MontantDu > 0).
            var periodesDisponibles = arrieresParPeriode
                .Select(p => new PeriodeClientFactureDto { Mois = p.Mois, Annees = p.Annees })
                .OrderByDescending(p => p.Annees)
                .ThenByDescending(p => p.Mois)
                .ToList();

            // Construire la réponse
            return new ArrieresConsolidesResponseDto
            {
                IdClient = idClient,
                NomClient = clientInfo?.NomClient,
                CodeCons = clientInfo?.CodeCons,
                TotalGeneral = totalGeneral,
                NombreTotalFactures = nombreTotalFactures,
                NombreTotalPeriodes = nombreTotalPeriodes,
                ArrieresParPeriode = arrieresParPeriode,
                PeriodesDisponibles = periodesDisponibles
            };
        }

        /// <summary>
        /// ✨ NOUVEAU : Récupère un rapport global des arriérés consolidés pour tous les clients
        /// Retourne les totaux globaux et la liste des arriérés par client groupés par période
        /// </summary>
        /// <param name="moisFacturePrecedentSeulement">Si true, filtre uniquement les clients facturés le mois précédent (défaut: true)</param>
        /// <param name="idAxe">Optionnel: filtre par axe spécifique</param>
        /// <param name="idTypeDeCourant">Optionnel: filtre par type de courant (ClientUsage actif)</param>
        /// <param name="mois">Optionnel: mois de la période de relance (ex. "04" ou "4"). Défaut: M-1 calendaire.</param>
        /// <param name="annee">Optionnel: année de la période de relance. Défaut: année de M-1, ou année courante si mois seul.</param>
        public async Task<ArrieresConsolidesGlobauxResponseDto> GetArrieresConsolidesGlobauxAsync(
            bool moisFacturePrecedentSeulement = true, 
            int? idAxe = null,
            int? idTypeDeCourant = null,
            string? mois = null,
            int? annee = null)
        {
            List<int> clientIds;
            var dateActuelle = DateTime.Now;

            if (moisFacturePrecedentSeulement)
            {
                var (moisPeriodeNormalise, moisPeriodeSansZero, anneePeriode) =
                    ResolvePeriodeRelance(mois, annee, dateActuelle);

                var clientsFacturesMoisPrecedent = await _context.ClientFactures
                    .AsNoTracking()
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.Axe)
                    .Include(cf => cf.Facture)
                        .ThenInclude(f => f.Usage)
                    .Where(cf => cf.Statut == true &&
                               cf.Annees == anneePeriode &&
                               (cf.Mois == moisPeriodeNormalise || cf.Mois == moisPeriodeSansZero) &&
                               cf.Montant.HasValue &&
                               cf.Montant.Value > 0 &&
                               (!idAxe.HasValue || cf.Client.IdAxe == idAxe.Value) &&
                               (!idTypeDeCourant.HasValue ||
                                   cf.Client.ClientsUsages.Any(cu =>
                                       cu.Statut && cu.IdTypeDeCourant == idTypeDeCourant.Value)))
                    .ToListAsync();
                    
                clientIds = clientsFacturesMoisPrecedent
                    .Select(cf => cf.IdClient)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // Logique existante (tous les clients avec arriérés) - OPTIMISÉ
                // 🚀 OPTIMISATION: Limiter aux 24 derniers mois pour éviter les timeouts
                var dateLimite = DateTime.Now.AddMonths(-24);
                
                var allArrieres = await _context.ClientFactures
                    .AsNoTracking()
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.Axe)
                    .Include(cf => cf.Facture)
                        .ThenInclude(f => f.Usage)
                    .Where(cf => cf.Statut == true && 
                               cf.MontantDu.HasValue && 
                               cf.MontantDu.Value > 0 &&
                               cf.DateCreation >= dateLimite &&  // 🚀 LIMITE TEMPORELLE
                               (!idAxe.HasValue || cf.Client.IdAxe == idAxe.Value) &&
                               (!idTypeDeCourant.HasValue ||
                                   cf.Client.ClientsUsages.Any(cu =>
                                       cu.Statut && cu.IdTypeDeCourant == idTypeDeCourant.Value)))
                    .ToListAsync();
                    
                clientIds = allArrieres
                    .Select(cf => cf.IdClient)
                    .Distinct()
                    .ToList();
                    
                _logger.LogInformation("🚀 OPTIMISATION: Limitation aux 24 derniers mois - {Count} factures chargées", allArrieres.Count);
            }

            if (!clientIds.Any())
            {
                return new ArrieresConsolidesGlobauxResponseDto
                {
                    TotalGeneralGlobal = 0,
                    NombreTotalClients = 0,
                    NombreTotalFactures = 0,
                    NombreTotalPeriodes = 0,
                    ArrieresParClient = new List<ArrieresConsolidesResponseDto>(),
                    PeriodesDisponibles = new List<PeriodeClientFactureDto>()
                };
            }

            // 2. Récupérer TOUTES les factures des clients sélectionnés avec données pré-chargées
            // 🚀 OPTIMISATION: Appliquer la même limite temporelle si nécessaire
            var dateLimiteFactures = DateTime.Now.AddMonths(-24);
            
            var query = _context.ClientFactures
                .AsNoTracking()
                .Include(cf => cf.Client)
                    .ThenInclude(c => c.Axe)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.TypeDeCourant)
                .Where(cf => cf.Statut == true && 
                           clientIds.Contains(cf.IdClient));
                           
            // Appliquer la limite temporelle seulement si on n'est pas en mode mois précédent
            if (!moisFacturePrecedentSeulement)
            {
                query = query.Where(cf => cf.DateCreation >= dateLimiteFactures);
                _logger.LogInformation("🚀 OPTIMISATION: Application limite temporelle sur les factures détaillées");
            }
            
            var allClientFactures = await query
                .OrderByDescending(cf => cf.Annees)
                .ThenByDescending(cf => cf.Mois)
                .ThenByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
                .ToListAsync();

            if (!allClientFactures.Any())
            {
                return new ArrieresConsolidesGlobauxResponseDto
                {
                    TotalGeneralGlobal = 0,
                    NombreTotalClients = 0,
                    NombreTotalFactures = 0,
                    NombreTotalPeriodes = 0,
                    ArrieresParClient = new List<ArrieresConsolidesResponseDto>(),
                    PeriodesDisponibles = new List<PeriodeClientFactureDto>()
                };
            }

            // Grouper par client
            var groupedByClient = allClientFactures
                .GroupBy(cf => cf.IdClient)
                .ToList();

            // Récupérer les informations complémentaires des clients en une seule requête optimisée
            var clientsInfo = await _context.Clients
                .Where(c => clientIds.Contains(c.IdClient))
                .Select(c => new {
                    c.IdClient,
                    c.AdresseClient,
                    NombreUsages = c.ClientsUsages.Count(cu => cu.Statut),
                    PremierUsageLibelle = c.ClientsUsages
                        .Where(cu => cu.Statut)
                        .Select(cu => cu.Usage.Libelle)
                        .FirstOrDefault(),
                    CategorieClient = c.ClientsUsages
                        .Where(cu => cu.Statut)
                        .Select(cu => cu.Usage.CategorieClient.NomCategorie)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var clientsInfoDict = clientsInfo.ToDictionary(x => x.IdClient);

            var arrieresParClient = new List<ArrieresConsolidesResponseDto>();
            var allPeriodes = new HashSet<string>(); // Pour compter les périodes distinctes globalement

            foreach (var clientGroup in groupedByClient)
            {
                var idClient = clientGroup.Key;
                var clientFactures = clientGroup.ToList();
                var clientInfo = clientFactures.First().Client;

                // Grouper par période (Mois/Annees) pour ce client
                var groupedByPeriode = clientFactures
                    .Where(cf => !string.IsNullOrWhiteSpace(cf.Mois) && cf.Annees.HasValue)
                    .GroupBy(cf => new { cf.Mois, cf.Annees })
                    .ToList();

                var arrieresParPeriode = new List<ArriereParPeriodeDto>();

                foreach (var groupe in groupedByPeriode)
                {
                    var facturesDuGroupe = groupe.ToList();
                    var periodeKey = $"{groupe.Key.Mois}/{groupe.Key.Annees}";
                    allPeriodes.Add(periodeKey); // Ajouter à l'ensemble global des périodes

                    // Convertir les ClientFacture en DTOs (optimisé - plus de N+1 queries)
                    var detailFactures = facturesDuGroupe
                        .Select(cf => ConvertToDtoOptimized(cf))
                        .ToList();

                    var arriereParPeriode = new ArriereParPeriodeDto
                    {
                        Mois = groupe.Key.Mois ?? "",
                        Annees = groupe.Key.Annees ?? 0,
                        DateEmission = facturesDuGroupe
                            .Where(cf => cf.DateEmission.HasValue)
                            .OrderByDescending(cf => cf.DateEmission)
                            .FirstOrDefault()?.DateEmission,

                        // Totaux consolidés
                        MontantTotal = facturesDuGroupe
                            .Where(cf => cf.Montant.HasValue)
                            .Sum(cf => cf.Montant.Value),
                        MontantPayeTotal = facturesDuGroupe
                            .Where(cf => cf.MontantPaye.HasValue)
                            .Sum(cf => cf.MontantPaye.Value),
                        MontantDuTotal = facturesDuGroupe
                            .Where(cf => cf.MontantDu.HasValue)
                            .Sum(cf => cf.MontantDu.Value),

                        // Détail
                        DetailFactures = detailFactures,

                        // Statistiques
                        NombreFactures = facturesDuGroupe.Count,
                        // 🚀 NOUVEAU: Optimisation - nombreUsages depuis ClientUsages
                        NombreUsages = clientsInfoDict.ContainsKey(idClient) 
                            ? clientsInfoDict[idClient].NombreUsages 
                            : 0
                    };

                    arrieresParPeriode.Add(arriereParPeriode);
                }

                // Calculer les totaux pour ce client
                var totalGeneral = arrieresParPeriode.Sum(a => a.MontantDuTotal);
                var nombreFacturesClient = clientFactures.Count;
                var nombrePeriodesClient = arrieresParPeriode.Count;

                // Dette antérieure : somme des montantDuTotal hors la dernière période du client
                var detteAnterieur = moisFacturePrecedentSeulement
                    ? CalculerDetteAnterieur(arrieresParPeriode)
                    : 0m;

                arrieresParClient.Add(new ArrieresConsolidesResponseDto
                {
                    IdClient = idClient,
                    NomClient = clientInfo?.NomClient,
                    CodeCons = clientInfo?.CodeCons,
                    TotalGeneral = totalGeneral,
                    NombreTotalFactures = nombreFacturesClient,
                    NombreTotalPeriodes = nombrePeriodesClient,
                    DetteAnterieur = detteAnterieur,
                    ArrieresParPeriode = arrieresParPeriode,
                    
                    // 🆕 NOUVELLES PROPRIÉTÉS AU NIVEAU CLIENT
                    AdresseClient = clientsInfoDict.ContainsKey(idClient) 
                        ? clientsInfoDict[idClient].AdresseClient 
                        : null,
                    CategorieClient = clientsInfoDict.ContainsKey(idClient) 
                        ? clientsInfoDict[idClient].CategorieClient 
                        : null,
                    LibelleUsage = clientsInfoDict.ContainsKey(idClient) 
                        ? clientsInfoDict[idClient].PremierUsageLibelle 
                        : null
                });
            }

            // Calculer les totaux globaux
            var totalGeneralGlobal = arrieresParClient.Sum(c => c.TotalGeneral);
            var nombreTotalClients = arrieresParClient.Count;
            var nombreTotalFactures = allClientFactures.Count;
            var nombreTotalPeriodes = allPeriodes.Count;

            // ✨ NOUVEAU : Liste des périodes globales (uniquement celles avec arriérés)
            // On les déduit du HashSet allPeriodes rempli pendant le groupement.
            var periodesDisponibles = allPeriodes
                .Select(k =>
                {
                    var parts = k.Split('/');
                    return new PeriodeClientFactureDto
                    {
                        Mois = parts.Length > 0 ? parts[0] : "",
                        Annees = (parts.Length > 1 && int.TryParse(parts[1], out var a)) ? a : 0
                    };
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Mois) && p.Annees > 0)
                .OrderByDescending(p => p.Annees)
                .ThenByDescending(p => p.Mois)
                .ToList();

            // Construire la réponse globale
            return new ArrieresConsolidesGlobauxResponseDto
            {
                TotalGeneralGlobal = totalGeneralGlobal,
                NombreTotalClients = nombreTotalClients,
                NombreTotalFactures = nombreTotalFactures,
                NombreTotalPeriodes = nombreTotalPeriodes,
                ArrieresParClient = arrieresParClient,
                PeriodesDisponibles = periodesDisponibles
            };
        }

        /// <summary>
        /// Récupère le rapport des client-factures agrégées par mois/année
        /// Correspond à la requête SQL de reporting des factures clients avec jointures multiples
        /// </summary>
        /// <param name="mois">Mois de facturation (optionnel, défaut: mois-1)</param>
        /// <param name="annees">Année de facturation (optionnel, défaut: année courante)</param>
        /// <param name="axe">Filtre optionnel par nom d'axe</param>
        /// <param name="usage">Filtre optionnel par libellé d'usage</param>
        /// <param name="limit">Nombre maximum de résultats (défaut: 200)</param>
        /// <returns>Liste des factures clients agrégées avec informations client, axe et usage</returns>
        public async Task<IEnumerable<ClientFactureReportDto>> GetClientFacturesReportAsync(
            string? mois = null, 
            int? annees = null, 
            string? axe = null,
            string? usage = null,
            int limit = 200)
        {
            try
            {
                // Valeurs par défaut si non spécifiées
                var dateRef = DateTime.Now.AddMonths(-1); // Mois précédent
                var targetMois = mois ?? dateRef.ToString("MM");
                var targetAnnees = annees ?? dateRef.Year;

                _logger.LogInformation("Génération du rapport ClientFacture pour la période {Mois}/{Annees} - Axe: {Axe}, Usage: {Usage}", 
                    targetMois, targetAnnees, axe, usage);

                // Requête LINQ optimisée correspondant à la requête SQL
                var query = from clf in _context.ClientFactures
                           join cl in _context.Clients on clf.IdClient equals cl.IdClient
                           join a in _context.Axes on cl.IdAxe equals a.IdAxe into axeGroup
                           from a in axeGroup.DefaultIfEmpty()
                           join f in _context.Factures on clf.IdFacture equals f.IdFacture into factureGroup
                           from f in factureGroup.DefaultIfEmpty()
                           join u in _context.Usages on f.IdUsage equals u.IdUsage into usageGroup
                           from u in usageGroup.DefaultIfEmpty()
                           where clf.Mois == targetMois 
                                 && clf.Annees == targetAnnees
                                 && clf.Statut == true
                                 && cl.Statut == true
                                 // Filtres optionnels par axe et usage
                                 && (string.IsNullOrEmpty(axe) || a.NomAxe != null && a.NomAxe.ToLower().Contains(axe.ToLower()))
                                 && (string.IsNullOrEmpty(usage) || u.Libelle != null && u.Libelle.ToLower().Contains(usage.ToLower()))
                           group clf by new 
                           { 
                               cl.CodeCons, 
                               cl.NomClient, 
                               NomAxe = a.NomAxe ?? "Non spécifié", 
                               LibelleUsage = u.Libelle ?? "Non spécifié",
                               clf.Mois, 
                               clf.Annees 
                           } into g
                           select new ClientFactureReportDto
                           {
                               CodeCons = g.Key.CodeCons ?? string.Empty,
                               NomClient = g.Key.NomClient,
                               Axe = g.Key.NomAxe,
                               Usage = g.Key.LibelleUsage,
                               Montant = g.Sum(cf => cf.Montant ?? 0),
                               Mois = g.Key.Mois ?? string.Empty,
                               Annees = g.Key.Annees ?? 0
                           };

                var results = await query
                    .OrderBy(r => r.NomClient)
                    .ThenBy(r => r.Axe)
                    .ThenBy(r => r.Usage)
                    .Take(limit)
                    .ToListAsync();

                _logger.LogInformation("Rapport ClientFacture généré: {Count} résultats pour la période {Mois}/{Annees} - Axe: {Axe}, Usage: {Usage}", 
                    results.Count, targetMois, targetAnnees, axe, usage);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du rapport ClientFacture pour la période {Mois}/{Annees} - Axe: {Axe}, Usage: {Usage}", 
                    mois, annees, axe, usage);
                return Enumerable.Empty<ClientFactureReportDto>();
            }
        }

        /// <summary>
        /// Récupère le rapport des client-factures agrégées par mois/année avec pagination
        /// Correspond à la requête SQL de reporting des factures clients avec jointures multiples
        /// </summary>
        /// <param name="request">Paramètres de pagination</param>
        /// <param name="mois">Mois de facturation (optionnel, défaut: mois-1)</param>
        /// <param name="annees">Année de facturation (optionnel, défaut: année courante)</param>
        /// <param name="axe">Filtre optionnel par nom d'axe</param>
        /// <param name="usage">Filtre optionnel par libellé d'usage</param>
        /// <returns>Résultat paginé des factures clients agrégées avec informations client, axe et usage</returns>
        public async Task<PagedResult<ClientFactureReportDto>> GetClientFacturesReportPagedAsync(
            PagedRequest request,
            string? mois = null, 
            int? annees = null, 
            string? axe = null,
            string? usage = null)
        {
            try
            {
                request ??= new PagedRequest();

                // Valeurs par défaut si non spécifiées
                var dateRef = DateTime.Now.AddMonths(-1); // Mois précédent
                var targetMois = mois ?? dateRef.ToString("MM");
                var targetAnnees = annees ?? dateRef.Year;

                _logger.LogInformation("Génération du rapport ClientFacture paginé pour la période {Mois}/{Annees} - Axe: {Axe}, Usage: {Usage}, Page: {Page}, Size: {Size}", 
                    targetMois, targetAnnees, axe, usage, request.PageNumber, request.PageSize);

                // Requête LINQ optimisée correspondant à la requête SQL
                var query = from clf in _context.ClientFactures
                           join cl in _context.Clients on clf.IdClient equals cl.IdClient
                           join a in _context.Axes on cl.IdAxe equals a.IdAxe into axeGroup
                           from a in axeGroup.DefaultIfEmpty()
                           join f in _context.Factures on clf.IdFacture equals f.IdFacture into factureGroup
                           from f in factureGroup.DefaultIfEmpty()
                           join u in _context.Usages on f.IdUsage equals u.IdUsage into usageGroup
                           from u in usageGroup.DefaultIfEmpty()
                           where clf.Mois == targetMois 
                                 && clf.Annees == targetAnnees
                                 && clf.Statut == true
                                 && cl.Statut == true
                                 // Filtres optionnels par axe et usage
                                 && (string.IsNullOrEmpty(axe) || a.NomAxe != null && a.NomAxe.ToLower().Contains(axe.ToLower()))
                                 && (string.IsNullOrEmpty(usage) || u.Libelle != null && u.Libelle.ToLower().Contains(usage.ToLower()))
                           group clf by new 
                           { 
                               cl.CodeCons, 
                               cl.NomClient, 
                               NomAxe = a.NomAxe ?? "Non spécifié", 
                               LibelleUsage = u.Libelle ?? "Non spécifié",
                               clf.Mois, 
                               clf.Annees 
                           } into g
                           select new ClientFactureReportDto
                           {
                               CodeCons = g.Key.CodeCons ?? string.Empty,
                               NomClient = g.Key.NomClient,
                               Axe = g.Key.NomAxe,
                               Usage = g.Key.LibelleUsage,
                               Montant = g.Sum(cf => cf.Montant ?? 0),
                               Mois = g.Key.Mois ?? string.Empty,
                               Annees = g.Key.Annees ?? 0
                           };

                // Calcul du total avant pagination
                var totalCount = await query.CountAsync();

                // Application du tri
                if (!string.IsNullOrEmpty(request.SortBy))
                {
                    switch (request.SortBy.ToLower())
                    {
                        case "nomclient":
                            query = request.SortDescending ? query.OrderByDescending(r => r.NomClient) : query.OrderBy(r => r.NomClient);
                            break;
                        case "axe":
                            query = request.SortDescending ? query.OrderByDescending(r => r.Axe) : query.OrderBy(r => r.Axe);
                            break;
                        case "usage":
                            query = request.SortDescending ? query.OrderByDescending(r => r.Usage) : query.OrderBy(r => r.Usage);
                            break;
                        case "montant":
                            query = request.SortDescending ? query.OrderByDescending(r => r.Montant) : query.OrderBy(r => r.Montant);
                            break;
                        case "mois":
                            query = request.SortDescending ? query.OrderByDescending(r => r.Mois) : query.OrderBy(r => r.Mois);
                            break;
                        case "annees":
                            query = request.SortDescending ? query.OrderByDescending(r => r.Annees) : query.OrderBy(r => r.Annees);
                            break;
                        default:
                            // Tri par défaut
                            query = query.OrderBy(r => r.NomClient).ThenBy(r => r.Axe).ThenBy(r => r.Usage);
                            break;
                    }
                }
                else
                {
                    // Tri par défaut si non spécifié
                    query = query.OrderBy(r => r.NomClient).ThenBy(r => r.Axe).ThenBy(r => r.Usage);
                }

                // Application de la pagination
                var data = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                _logger.LogInformation("Rapport ClientFacture paginé généré: {Count} résultats sur {Total} pour la période {Mois}/{Annees} - Axe: {Axe}, Usage: {Usage}", 
                    data.Count, totalCount, targetMois, targetAnnees, axe, usage);

                return new PagedResult<ClientFactureReportDto>(data, totalCount, request.PageNumber, request.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du rapport ClientFacture paginé pour la période {Mois}/{Annees} - Axe: {Axe}, Usage: {Usage}", 
                    mois, annees, axe, usage);
                return new PagedResult<ClientFactureReportDto>(new List<ClientFactureReportDto>(), 0, request.PageNumber, request.PageSize);
            }
        }
    }
}
