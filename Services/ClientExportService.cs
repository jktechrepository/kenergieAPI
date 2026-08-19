using Kenergie.Models;
using Kenergie.Models.DTOs.Client;
using Kenergie.Data;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Serilog;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour l'export des données clients
    /// </summary>
    public class ClientExportService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<ClientExportService> _logger;
        private readonly MetricsService _metricsService;

        public ClientExportService(KenergieDbContext context, ILogger<ClientExportService> logger, MetricsService metricsService)
        {
            _context = context;
            _logger = logger;
            _metricsService = metricsService;
        }

        /// <summary>
        /// Exporte les clients avec leurs usages au format Excel
        /// </summary>
        public async Task<byte[]> ExportToExcelAsync(int idSociete, ClientExportRequest request)
        {
            try
            {
                _logger.LogInformation("Début de l'export Excel pour la société {SocieteId}", idSociete);

                var clients = await GetClientsWithUsagesAsync(idSociete, request);
                var exportData = MapToExportDtos(clients);

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Clients");

                // Configuration EPPlus
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                // En-têtes
                SetupHeaders(worksheet);

                // Données
                var row = 2;
                foreach (var client in exportData)
                {
                    PopulateRow(worksheet, row, client);
                    row++;
                }

                // Mise en forme
                FormatWorksheet(worksheet, row - 1);

                _logger.LogInformation("Export terminé avec succès");

                // Enregistrer l'export dans les métriques
                MetricsService.RecordExport();

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel");
                throw;
            }
        }

        /// <summary>
        /// Récupère les clients avec leurs usages selon les filtres
        /// </summary>
        private async Task<List<Client>> GetClientsWithUsagesAsync(int idSociete, ClientExportRequest request)
        {
            var query = _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                .Where(c => c.Statut == true &&
                           c.ClientsUsages != null &&
                           c.ClientsUsages.Any(cu => cu.Usage != null &&
                                                     cu.Usage.CategorieClient != null &&
                                                     cu.Usage.CategorieClient.IdSociete == idSociete &&
                                                     cu.Statut == true &&
                                                     cu.Usage.Statut == true));

            // Filtre par Axe
            if (request.IdAxe.HasValue)
            {
                query = query.Where(c => c.IdAxe == request.IdAxe.Value);
            }

            // Filtre par recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.NomClient.ToLower().Contains(term) ||
                    (c.AdresseClient ?? string.Empty).ToLower().Contains(term) ||
                    (c.Telephone ?? string.Empty).ToLower().Contains(term) ||
                    (c.EmailClient ?? string.Empty).ToLower().Contains(term) ||
                    (c.CodeCons ?? string.Empty).ToLower().Contains(term));
            }

            // Filtre IsActif : explicite > IncludeInactive > défaut actifs
            if (request.HasIsActifFilter)
            {
                query = query.Where(c => c.IsActif == request.ActifFilterValue);
            }
            else if (!request.IncludeInactive)
            {
                query = query.Where(c => c.IsActif == true);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Convertit les entités Client en DTOs d'export
        /// </summary>
        private List<ClientExportDto> MapToExportDtos(List<Client> clients)
        {
            return clients.Select(client =>
            {
                var usages = client.ClientsUsages?.Where(cu => cu.Usage != null && cu.Statut == true).ToList() ?? new List<ClientUsage>();

                return new ClientExportDto
                {
                    IdClient = client.IdClient,
                    NomClient = client.NomClient,
                    AdresseClient = client.AdresseClient,
                    Telephone = client.Telephone,
                    EmailClient = client.EmailClient,
                    GenreClient = client.GenreClient,
                    CodeCons = client.CodeCons,
                    Statut = client.Statut,
                    IsActif = client.IsActif,
                    DateCreation = client.DateCreation,
                    CodeAxe = client.Axe?.CodeAxe,
                    NomAxe = client.Axe?.NomAxe,
                    DescriptionAxe = client.Axe?.Description,
                    CodeCabine = client.Axe?.Cabine?.CodeCabine,
                    NomCabine = client.Axe?.Cabine?.Nom,
                    DescriptionCabine = client.Axe?.Cabine?.Description,
                    UsagesLibelles = string.Join("; ", usages.Select(cu => cu.Usage!.Libelle)),
                    UsagesMontants = string.Join("; ", usages.Select(cu => cu.nombreBatiment.ToString())),
                    UsagesCategories = string.Join("; ", usages.Select(cu => cu.Usage!.CategorieClient!.NomCategorie)),
                    NombreUsages = usages.Count
                };
            }).ToList();
        }

        /// <summary>
        /// Configure les en-têtes de la feuille Excel
        /// </summary>
        private void SetupHeaders(ExcelWorksheet worksheet)
        {
            worksheet.Cells["A1"].Value = "Nom Client";
            worksheet.Cells["B1"].Value = "Adresse";
            worksheet.Cells["C1"].Value = "Téléphone";
            worksheet.Cells["D1"].Value = "Email";
            worksheet.Cells["E1"].Value = "Genre";
            worksheet.Cells["F1"].Value = "Code Cons";
            worksheet.Cells["G1"].Value = "Actif";
            worksheet.Cells["H1"].Value = "Date Création";
            worksheet.Cells["I1"].Value = "Nom Axe";
            worksheet.Cells["J1"].Value = "Nom Cabine";
            worksheet.Cells["K1"].Value = "Usages";
            worksheet.Cells["L1"].Value = "Nombre Bâtiments";
            worksheet.Cells["M1"].Value = "Catégories Usages";
            worksheet.Cells["N1"].Value = "Nombre Usages";
        }

        /// <summary>
        /// Remplit une ligne avec les données d'un client
        /// </summary>
        private void PopulateRow(ExcelWorksheet worksheet, int row, ClientExportDto client)
        {
            worksheet.Cells[$"A{row}"].Value = client.NomClient;
            worksheet.Cells[$"B{row}"].Value = client.AdresseClient;
            worksheet.Cells[$"C{row}"].Value = client.Telephone;
            worksheet.Cells[$"D{row}"].Value = client.EmailClient;
            worksheet.Cells[$"E{row}"].Value = client.GenreClient;
            worksheet.Cells[$"F{row}"].Value = client.CodeCons;
            worksheet.Cells[$"G{row}"].Value = client.IsActif ? "Oui" : "Non";
            worksheet.Cells[$"H{row}"].Value = client.DateCreation.ToString("dd/MM/yyyy HH:mm");
            worksheet.Cells[$"I{row}"].Value = client.NomAxe;
            worksheet.Cells[$"J{row}"].Value = client.NomCabine;
            worksheet.Cells[$"K{row}"].Value = client.UsagesLibelles;
            worksheet.Cells[$"L{row}"].Value = client.UsagesMontants;
            worksheet.Cells[$"M{row}"].Value = client.UsagesCategories;
            worksheet.Cells[$"N{row}"].Value = client.NombreUsages;
        }

        /// <summary>
        /// Applique le formatage à la feuille Excel
        /// </summary>
        private void FormatWorksheet(ExcelWorksheet worksheet, int lastRow)
        {
            // Style des en-têtes
            using (var range = worksheet.Cells[1, 1, 1, 14])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Bordures pour toutes les données
            using (var range = worksheet.Cells[1, 1, lastRow, 14])
            {
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }

            // Auto-fit des colonnes
            for (int col = 1; col <= 14; col++)
            {
                worksheet.Column(col).AutoFit();
            }

            // Formatage des colonnes spécifiques
            worksheet.Column(8).Style.Numberformat.Format = "dd/mm/yyyy hh:mm"; // Date Création
            worksheet.Column(12).Style.Numberformat.Format = "0"; // Nombre Bâtiments
            worksheet.Column(14).Style.Numberformat.Format = "0"; // Nombre Usages

            // Congeler les en-têtes
            worksheet.View.FreezePanes(2, 1);
        }
    }
}
