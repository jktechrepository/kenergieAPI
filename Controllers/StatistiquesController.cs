using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kenergie.Services;
using Kenergie.Models.DTOs.Statistiques;
using System;
using System.Threading.Tasks;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour les statistiques et indicateurs de performance
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Super-Admin,Admin,Financier,Gerant,Responsable Commercial")]
    public class StatistiquesController : ControllerBase
    {
        private readonly IStatistiquesService _statistiquesService;
        private readonly ILogger<StatistiquesController> _logger;

        public StatistiquesController(
            IStatistiquesService statistiquesService,
            ILogger<StatistiquesController> logger)
        {
            _statistiquesService = statistiquesService;
            _logger = logger;
        }

        /// <summary>
        /// Obtient les statistiques générales pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="idCategorieClient">Filtre optionnel par catégorie de client</param>
        /// <param name="idCabine">Filtre optionnel par cabine</param>
        /// <param name="idAxe">Filtre optionnel par axe</param>
        /// <param name="idTypeDeCourant">Filtre optionnel par type de courant</param>
        /// <param name="idUsage">Filtre optionnel par usage</param>
        /// <returns>Statistiques générales</returns>
        [HttpGet("generales/{idSociete}")]
        public async Task<ActionResult<StatistiquesGeneralesDto>> GetStatistiquesGenerales(
            int idSociete,
            [FromQuery] int? idCategorieClient = null,
            [FromQuery] int? idCabine = null,
            [FromQuery] int? idAxe = null,
            [FromQuery] int? idTypeDeCourant = null,
            [FromQuery] int? idUsage = null)
        {
            try
            {
                _logger.LogInformation("Récupération des statistiques générales pour la société {SocieteId} avec filtres: Categorie={Categorie}, Cabine={Cabine}, Axe={Axe}, TypeDeCourant={TypeDeCourant}, Usage={Usage}", 
                    idSociete, idCategorieClient, idCabine, idAxe, idTypeDeCourant, idUsage);

                var filtres = new StatistiquesFiltresDto
                {
                    IdCategorieClient = idCategorieClient,
                    IdCabine = idCabine,
                    IdAxe = idAxe,
                    IdTypeDeCourant = idTypeDeCourant,
                    IdUsage = idUsage
                };

                var statistiques = await _statistiquesService.GetStatistiquesGeneralesAsync(idSociete, filtres);

                if (statistiques == null)
                {
                    return NotFound(new { message = "Statistiques non trouvées pour cette société" });
                }

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques générales pour la société {SocieteId}", idSociete);
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques générales" });
            }
        }

        /// <summary>
        /// Obtient les statistiques financières pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="debut">Date de début (optionnel)</param>
        /// <param name="fin">Date de fin (optionnel)</param>
        /// <param name="idCategorieClient">Filtre optionnel par catégorie de client</param>
        /// <param name="idCabine">Filtre optionnel par cabine</param>
        /// <param name="idAxe">Filtre optionnel par axe</param>
        /// <param name="idTypeDeCourant">Filtre optionnel par type de courant</param>
        /// <param name="idUsage">Filtre optionnel par usage</param>
        /// <returns>Statistiques financières</returns>
        [HttpGet("financieres/{idSociete}")]
        public async Task<ActionResult<StatistiquesFinancieresDto>> GetStatistiquesFinancieres(
            int idSociete,
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null,
            [FromQuery] int? idCategorieClient = null,
            [FromQuery] int? idCabine = null,
            [FromQuery] int? idAxe = null,
            [FromQuery] int? idTypeDeCourant = null,
            [FromQuery] int? idUsage = null)
        {
            try
            {
                _logger.LogInformation("Récupération des statistiques financières pour la société {SocieteId} (période: {Debut} - {Fin}) avec filtres: Categorie={Categorie}, Cabine={Cabine}, Axe={Axe}, TypeDeCourant={TypeDeCourant}, Usage={Usage}", 
                    idSociete, debut?.ToString("yyyy-MM-dd"), fin?.ToString("yyyy-MM-dd"), idCategorieClient, idCabine, idAxe, idTypeDeCourant, idUsage);

                var filtres = new StatistiquesFiltresDto
                {
                    IdCategorieClient = idCategorieClient,
                    IdCabine = idCabine,
                    IdAxe = idAxe,
                    IdTypeDeCourant = idTypeDeCourant,
                    IdUsage = idUsage
                };

                var statistiques = await _statistiquesService.GetStatistiquesFinancieresAsync(idSociete, debut, fin, filtres);

                if (statistiques == null)
                {
                    return NotFound(new { message = "Statistiques financières non trouvées pour cette société" });
                }

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques financières pour la société {SocieteId}", idSociete);
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques financières" });
            }
        }

        /// <summary>
        /// Obtient les statistiques opérationnelles pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="idCategorieClient">Filtre optionnel par catégorie de client</param>
        /// <param name="idCabine">Filtre optionnel par cabine</param>
        /// <param name="idAxe">Filtre optionnel par axe</param>
        /// <param name="idTypeDeCourant">Filtre optionnel par type de courant</param>
        /// <param name="idUsage">Filtre optionnel par usage</param>
        /// <returns>Statistiques opérationnelles</returns>
        [HttpGet("operationnelles/{idSociete}")]
        public async Task<ActionResult<StatistiquesOperationnellesDto>> GetStatistiquesOperationnelles(
            int idSociete,
            [FromQuery] int? idCategorieClient = null,
            [FromQuery] int? idCabine = null,
            [FromQuery] int? idAxe = null,
            [FromQuery] int? idTypeDeCourant = null,
            [FromQuery] int? idUsage = null)
        {
            try
            {
                _logger.LogInformation("Récupération des statistiques opérationnelles pour la société {SocieteId} avec filtres: Categorie={Categorie}, Cabine={Cabine}, Axe={Axe}, TypeDeCourant={TypeDeCourant}, Usage={Usage}", 
                    idSociete, idCategorieClient, idCabine, idAxe, idTypeDeCourant, idUsage);

                var filtres = new StatistiquesFiltresDto
                {
                    IdCategorieClient = idCategorieClient,
                    IdCabine = idCabine,
                    IdAxe = idAxe,
                    IdTypeDeCourant = idTypeDeCourant,
                    IdUsage = idUsage
                };

                var statistiques = await _statistiquesService.GetStatistiquesOperationnellesAsync(idSociete, filtres);

                if (statistiques == null)
                {
                    return NotFound(new { message = "Statistiques opérationnelles non trouvées pour cette société" });
                }

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques opérationnelles pour la société {SocieteId}", idSociete);
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques opérationnelles" });
            }
        }

        /// <summary>
        /// Obtient les statistiques de performance pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="idCategorieClient">Filtre optionnel par catégorie de client</param>
        /// <param name="idCabine">Filtre optionnel par cabine</param>
        /// <param name="idAxe">Filtre optionnel par axe</param>
        /// <param name="idTypeDeCourant">Filtre optionnel par type de courant</param>
        /// <param name="idUsage">Filtre optionnel par usage</param>
        /// <returns>Statistiques de performance</returns>
        [HttpGet("performance/{idSociete}")]
        public async Task<ActionResult<StatistiquesPerformanceDto>> GetStatistiquesPerformance(
            int idSociete,
            [FromQuery] int? idCategorieClient = null,
            [FromQuery] int? idCabine = null,
            [FromQuery] int? idAxe = null,
            [FromQuery] int? idTypeDeCourant = null,
            [FromQuery] int? idUsage = null)
        {
            try
            {
                _logger.LogInformation("Récupération des statistiques de performance pour la société {SocieteId} avec filtres: Categorie={Categorie}, Cabine={Cabine}, Axe={Axe}, TypeDeCourant={TypeDeCourant}, Usage={Usage}", 
                    idSociete, idCategorieClient, idCabine, idAxe, idTypeDeCourant, idUsage);

                var filtres = new StatistiquesFiltresDto
                {
                    IdCategorieClient = idCategorieClient,
                    IdCabine = idCabine,
                    IdAxe = idAxe,
                    IdTypeDeCourant = idTypeDeCourant,
                    IdUsage = idUsage
                };

                var statistiques = await _statistiquesService.GetStatistiquesPerformanceAsync(idSociete, filtres);

                if (statistiques == null)
                {
                    return NotFound(new { message = "Statistiques de performance non trouvées pour cette société" });
                }

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de performance pour la société {SocieteId}", idSociete);
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques de performance" });
            }
        }

        /// <summary>
        /// Obtient toutes les statistiques consolidées pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="debut">Date de début (optionnel)</param>
        /// <param name="fin">Date de fin (optionnel)</param>
        /// <param name="idCategorieClient">Filtre optionnel par catégorie de client</param>
        /// <param name="idCabine">Filtre optionnel par cabine</param>
        /// <param name="idAxe">Filtre optionnel par axe</param>
        /// <param name="idTypeDeCourant">Filtre optionnel par type de courant</param>
        /// <param name="idUsage">Filtre optionnel par usage</param>
        /// <returns>Statistiques consolidées</returns>
        [HttpGet("consolidees/{idSociete}")]
        public async Task<ActionResult<StatistiquesConsolideesDto>> GetStatistiquesConsolidees(
            int idSociete,
            [FromQuery] DateTime? debut = null,
            [FromQuery] DateTime? fin = null,
            [FromQuery] int? idCategorieClient = null,
            [FromQuery] int? idCabine = null,
            [FromQuery] int? idAxe = null,
            [FromQuery] int? idTypeDeCourant = null,
            [FromQuery] int? idUsage = null)
        {
            try
            {
                _logger.LogInformation("Récupération des statistiques consolidées pour la société {SocieteId} (période: {Debut} - {Fin}) avec filtres: Categorie={Categorie}, Cabine={Cabine}, Axe={Axe}, TypeDeCourant={TypeDeCourant}, Usage={Usage}", 
                    idSociete, debut?.ToString("yyyy-MM-dd"), fin?.ToString("yyyy-MM-dd"), idCategorieClient, idCabine, idAxe, idTypeDeCourant, idUsage);

                var filtres = new StatistiquesFiltresDto
                {
                    IdCategorieClient = idCategorieClient,
                    IdCabine = idCabine,
                    IdAxe = idAxe,
                    IdTypeDeCourant = idTypeDeCourant,
                    IdUsage = idUsage
                };

                var statistiques = await _statistiquesService.GetStatistiquesConsolideesAsync(idSociete, debut, fin, filtres);

                if (statistiques == null)
                {
                    return NotFound(new { message = "Statistiques consolidées non trouvées pour cette société" });
                }

                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques consolidées pour la société {SocieteId}", idSociete);
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques consolidées" });
            }
        }
    }
}
