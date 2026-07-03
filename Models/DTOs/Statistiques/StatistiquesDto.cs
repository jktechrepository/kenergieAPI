using System;
using System.Collections.Generic;

namespace Kenergie.Models.DTOs.Statistiques
{
    /// <summary>
    /// DTO pour les statistiques générales
    /// </summary>
    public class StatistiquesGeneralesDto
    {
        /// <summary>
        /// Nombre de clients actifs opérationnels (IsActif, Statut, hors soft-delete).
        /// Les montants financiers utilisent un périmètre plus large (tous clients rattachés).
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// Nombre total de factures
        /// </summary>
        public int TotalFactures { get; set; }

        /// <summary>
        /// Montant total des arriérés
        /// </summary>
        public decimal TotalArrieres { get; set; }

        /// <summary>
        /// Montant total des paiements validés du mois calendaire en cours.
        /// </summary>
        public decimal TotalPaiements { get; set; }

        /// <summary>
        /// Taux de recouvrement global (%)
        /// </summary>
        public decimal TauxRecouvrement { get; set; }

        /// <summary>
        /// Nombre de paiements validés du mois calendaire en cours.
        /// </summary>
        public int TotalPaiementsCount { get; set; }

        /// <summary>
        /// Date de génération des statistiques
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO pour les statistiques financières
    /// </summary>
    public class StatistiquesFinancieresDto
    {
        /// <summary>
        /// Collecte du mois calendaire en cours (paiements validés).
        /// </summary>
        public decimal ChiffreAffaires { get; set; }

        /// <summary>
        /// Montant total des arriérés
        /// </summary>
        public decimal MontantArrieres { get; set; }

        /// <summary>
        /// Montant total des paiements validés sur la période des paiements
        /// (mois en cours par défaut ; personnalisable via ?debut= et ?fin=).
        /// </summary>
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Montant total dû
        /// </summary>
        public decimal MontantDu { get; set; }

        /// <summary>
        /// Évolution mensuelle des montants
        /// </summary>
        public List<EvolutionMensuelleDto> EvolutionMensuelle { get; set; } = new();

        /// <summary>
        /// Répartition des paiements par méthode
        /// </summary>
        public List<RepartitionPaiementDto> RepartitionPaiements { get; set; } = new();

        /// <summary>
        /// Date de génération des statistiques
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO pour les statistiques opérationnelles
    /// </summary>
    public class StatistiquesOperationnellesDto
    {
        /// <summary>
        /// Répartition des clients par catégorie
        /// </summary>
        public List<RepartitionClientParCategorieDto> RepartitionClientsParCategorie { get; set; } = new();

        /// <summary>
        /// Répartition des clients par axe/cabine
        /// </summary>
        public List<RepartitionClientParAxeDto> RepartitionClientsParAxe { get; set; } = new();

        /// <summary>
        /// Statistiques des factures par mois
        /// </summary>
        public List<StatistiqueFactureMoisDto> StatistiquesFacturesMois { get; set; } = new();

        /// <summary>
        /// Nombre de clients actifs vs inactifs
        /// </summary>
        public ClientActiviteDto ClientActivite { get; set; }

        /// <summary>
        /// Date de génération des statistiques
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO pour les statistiques de performance
    /// </summary>
    public class StatistiquesPerformanceDto
    {
        /// <summary>
        /// Taux de recouvrement global (%)
        /// </summary>
        public decimal TauxRecouvrementGlobal { get; set; }

        /// <summary>
        /// Taux de recouvrement par catégorie
        /// </summary>
        public List<TauxRecouvrementParCategorieDto> TauxRecouvrementParCategorie { get; set; } = new();

        /// <summary>
        /// Top des agents caissiers par montant collecté sur le mois en cours (max. 10, collecte &gt; 0).
        /// </summary>
        public List<TopAgentDto> TopAgents { get; set; } = new();

        /// <summary>
        /// Performance mensuelle
        /// </summary>
        public List<PerformanceMensuelleDto> PerformanceMensuelle { get; set; } = new();

        /// <summary>
        /// Date de génération des statistiques
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO pour les statistiques consolidées
    /// </summary>
    public class StatistiquesConsolideesDto
    {
        /// <summary>
        /// Statistiques générales
        /// </summary>
        public StatistiquesGeneralesDto Generales { get; set; }

        /// <summary>
        /// Statistiques financières
        /// </summary>
        public StatistiquesFinancieresDto Financieres { get; set; }

        /// <summary>
        /// Statistiques opérationnelles
        /// </summary>
        public StatistiquesOperationnellesDto Operationnelles { get; set; }

        /// <summary>
        /// Statistiques de performance
        /// </summary>
        public StatistiquesPerformanceDto Performance { get; set; }

        /// <summary>
        /// Période des statistiques
        /// </summary>
        public PeriodeStatistiquesDto Periode { get; set; }

        /// <summary>
        /// Date de génération des statistiques
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.Now;
    }

    // DTOs de support

    /// <summary>
    /// DTO pour l'évolution mensuelle
    /// </summary>
    public class EvolutionMensuelleDto
    {
        public string Mois { get; set; }
        public decimal MontantFactures { get; set; }
        public decimal MontantPaiements { get; set; }
        public decimal MontantArrieres { get; set; }
        public int NombreFactures { get; set; }
        public int NombrePaiements { get; set; }
    }

    /// <summary>
    /// DTO pour la répartition des paiements par méthode
    /// </summary>
    public class RepartitionPaiementDto
    {
        public string MethodePaiement { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombrePaiements { get; set; }
        public decimal Pourcentage { get; set; }
    }

    /// <summary>
    /// DTO pour la répartition des clients par catégorie
    /// </summary>
    public class RepartitionClientParCategorieDto
    {
        public int IdCategorie { get; set; }
        public string NomCategorie { get; set; }
        public int NombreClients { get; set; }
        public decimal Pourcentage { get; set; }
        public decimal MontantTotal { get; set; }
    }

    /// <summary>
    /// DTO pour la répartition des clients par axe
    /// </summary>
    public class RepartitionClientParAxeDto
    {
        public int IdAxe { get; set; }
        public string NomAxe { get; set; }
        public string NomCabine { get; set; }
        public int NombreClients { get; set; }
        public decimal Pourcentage { get; set; }
    }

    /// <summary>
    /// DTO pour les statistiques de factures par mois
    /// </summary>
    public class StatistiqueFactureMoisDto
    {
        public string Mois { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombreFactures { get; set; }
        public decimal MontantMoyen { get; set; }
    }

    /// <summary>
    /// DTO pour l'activité des clients
    /// </summary>
    public class ClientActiviteDto
    {
        public int NombreClientsActifs { get; set; }
        public int NombreClientsInactifs { get; set; }
        public int TotalClients { get; set; }
        public decimal PourcentageActifs { get; set; }
        public decimal PourcentageInactifs { get; set; }
    }

    /// <summary>
    /// DTO pour le taux de recouvrement par catégorie
    /// </summary>
    public class TauxRecouvrementParCategorieDto
    {
        public int IdCategorie { get; set; }
        public string NomCategorie { get; set; }
        public decimal TauxRecouvrement { get; set; }
        public decimal MontantDu { get; set; }
        public decimal MontantPaye { get; set; }
    }

    /// <summary>
    /// Agent caissier dans le top collecte du mois calendaire en cours.
    /// </summary>
    public class TopAgentDto
    {
        public int IdAgent { get; set; }
        public string NomAgent { get; set; }
        public decimal MontantCollecte { get; set; }
        public int NombrePaiements { get; set; }
        public decimal TauxConversion { get; set; }
    }

    /// <summary>
    /// DTO pour la performance mensuelle
    /// </summary>
    public class PerformanceMensuelleDto
    {
        public string Mois { get; set; }
        public decimal TauxRecouvrement { get; set; }
        public decimal MontantCollecte { get; set; }
        public int NombrePaiements { get; set; }
        public decimal TicketMoyen { get; set; }
    }

    /// <summary>
    /// DTO pour la période des statistiques
    /// </summary>
    public class PeriodeStatistiquesDto
    {
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string LibellePeriode { get; set; }
    }
}
