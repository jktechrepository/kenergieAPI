using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.ClientFacture;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour l'import en masse de ClientFacture (arriérés pré-existants) depuis un fichier Excel
    /// </summary>
    public class ExcelClientFactureService
    {
        private readonly KenergieDbContext _context;
        private readonly IClientFactureRepository _clientFactureRepository;
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<ExcelClientFactureService> _logger;
        private const int MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private const int BatchSize = 50; // Traiter 50 ClientFacture à la fois

        public ExcelClientFactureService(
            KenergieDbContext context,
            IClientFactureRepository clientFactureRepository,
            IClientRepository clientRepository,
            ILogger<ExcelClientFactureService> logger)
        {
            _context = context;
            _clientFactureRepository = clientFactureRepository;
            _clientRepository = clientRepository;
            _logger = logger;
        }

        /// <summary>
        /// Point d'entrée principal pour traiter un fichier Excel
        /// </summary>
        public async Task<BulkClientFactureResult> ProcessExcelFileAsync(IFormFile file)
        {
            var result = new BulkClientFactureResult();

            try
            {
                // Pass 1: Validation du fichier
                var fileValidation = ValidateFile(file);
                if (!fileValidation.IsValid)
                {
                    result.Success = false;
                    result.Message = fileValidation.ErrorMessage;
                    return result;
                }

                // Pass 2: Lecture du fichier Excel
                var rawData = await ReadExcelFileAsync(file);
                if (rawData == null || rawData.Count == 0)
                {
                    result.Success = false;
                    result.Message = "Le fichier Excel est vide ou ne contient pas de données valides.";
                    return result;
                }

                result.TotalLignes = rawData.Count;

                // Pass 3: Conversion et enrichissement (récupération des IdClient via CodeCons)
                var enrichedData = await ConvertToClientFactureExcelDtoAsync(rawData);

                // Pass 4: Validation des données
                ValidateClientFactures(enrichedData);

                // Pass 5: Déduplication
                DeduplicateInFile(enrichedData, result);

                // Pass 6: Séparation des lignes valides/invalides
                var lignesValides = enrichedData.Where(d => d.Erreurs.Count == 0).ToList();
                var lignesInvalides = enrichedData.Where(d => d.Erreurs.Count > 0).ToList();

                result.LignesAvecErreurs = lignesInvalides.Select(l => new LigneErreurClientFacture
                {
                    NumeroLigne = l.NumeroLigne,
                    CodeCons = l.CodeCons,
                    Erreurs = l.Erreurs
                }).ToList();

                result.LignesEchouees = lignesInvalides.Count;

                // ✨ NOUVEAU : Pass 6.5: Sauvegarder les lignes échouées dans ArriereeCrashed
                if (lignesInvalides.Count > 0)
                {
                    await SaveFailedLinesAsync(lignesInvalides);
                }

                // Pass 7: Traitement par lots
                if (lignesValides.Count > 0)
                {
                    await ProcessBatchesAsync(lignesValides, result);
                }

                // Génération du message final
                result.Message = GenerateResultMessage(result);
                result.Success = result.LignesReussies > 0;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du traitement du fichier Excel");
                result.Success = false;
                result.Message = $"Erreur lors du traitement : {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Valide le fichier Excel (Pass 1)
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Le fichier est vide ou n'a pas été fourni");
            }

            if (file.Length > MaxFileSize)
            {
                return (false, $"Le fichier dépasse la taille maximale autorisée ({MaxFileSize / 1024 / 1024} MB)");
            }

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx")
            {
                return (false, "Le fichier doit être au format .xlsx (Excel 2007+)");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Lit le fichier Excel et retourne les données brutes (Pass 2)
        /// </summary>
        private async Task<List<ClientFactureExcelRaw>> ReadExcelFileAsync(IFormFile file)
        {
            var rawData = new List<ClientFactureExcelRaw>();
            var requiredColumns = new[] { "CodeCons", "Montant", "Mois", "Annees" };

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        throw new InvalidOperationException("Le fichier Excel ne contient aucune feuille de calcul");
                    }

                    var worksheet = package.Workbook.Worksheets[0];

                    if (worksheet.Dimension == null)
                    {
                        throw new InvalidOperationException("La feuille de calcul est vide");
                    }

                    // Détecter la ligne des en-têtes (peut être ligne 1 ou 2 selon la présence d'instructions)
                    int headerRow = 1;
                    int startRow = 2;
                    
                    // Vérifier si la ligne 1 contient "INSTRUCTIONS"
                    var firstCell = worksheet.Cells[1, 1].Value?.ToString();
                    if (firstCell != null && firstCell.Contains("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRow = 2;
                        startRow = 3;
                    }

                    // Lire les en-têtes
                    var headers = new Dictionary<string, int>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var headerValue = worksheet.Cells[headerRow, col].Value?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(headerValue))
                        {
                            headers[headerValue] = col;
                        }
                    }

                    // Vérifier que toutes les colonnes requises sont présentes
                    var missingColumns = requiredColumns.Where(c => !headers.ContainsKey(c)).ToList();
                    if (missingColumns.Any())
                    {
                        throw new InvalidOperationException(
                            $"Colonnes manquantes dans le fichier Excel : {string.Join(", ", missingColumns)}");
                    }
                    
                    // Lire les données
                    for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var raw = new ClientFactureExcelRaw
                        {
                            CodeCons = GetCellValue(worksheet, row, headers, "CodeCons"),
                            Montant = GetCellValue(worksheet, row, headers, "Montant"),
                            Mois = GetCellValue(worksheet, row, headers, "Mois"),
                            Annees = GetCellValue(worksheet, row, headers, "Annees")
                        };

                        // Ne pas ajouter les lignes complètement vides
                        if (!string.IsNullOrWhiteSpace(raw.CodeCons) || 
                            !string.IsNullOrWhiteSpace(raw.Montant) ||
                            !string.IsNullOrWhiteSpace(raw.Mois) ||
                            !string.IsNullOrWhiteSpace(raw.Annees))
                        {
                            rawData.Add(raw);
                        }
                    }
                }
            }

            return rawData;
        }

        /// <summary>
        /// Récupère la valeur d'une cellule
        /// </summary>
        private string? GetCellValue(ExcelWorksheet worksheet, int row, Dictionary<string, int> headers, string columnName)
        {
            if (!headers.ContainsKey(columnName))
                return null;

            var col = headers[columnName];
            var cell = worksheet.Cells[row, col];
            
            if (cell.Value == null)
                return null;

            var stringValue = cell.Value.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
        }

        /// <summary>
        /// Convertit et enrichit les données brutes avec les IdClient (Pass 3)
        /// </summary>
        private async Task<List<ClientFactureExcelDto>> ConvertToClientFactureExcelDtoAsync(List<ClientFactureExcelRaw> rawData)
        {
            var result = new List<ClientFactureExcelDto>();

            // ✨ OPTIMISATION : Charger tous les clients en mémoire pour éviter N+1 queries
            var allClients = await _context.Clients
                .Where(c => c.Statut == true && !string.IsNullOrWhiteSpace(c.CodeCons))
                .ToListAsync();

            var clientsDict = allClients
                .GroupBy(c => c.CodeCons?.Trim() ?? "")
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("✅ {Count} clients chargés en mémoire pour lookup rapide", allClients.Count);

            for (int i = 0; i < rawData.Count; i++)
            {
                var raw = rawData[i];
                var dto = new ClientFactureExcelDto
                {
                    NumeroLigne = i + 1,
                    CodeCons = raw.CodeCons?.Trim(),
                    Montant = null,
                    Mois = null,
                    Annees = null
                };

                // Récupérer IdClient via CodeCons
                if (!string.IsNullOrWhiteSpace(dto.CodeCons))
                {
                    if (clientsDict.TryGetValue(dto.CodeCons, out var client))
                    {
                        dto.IdClient = client.IdClient;
                        _logger.LogDebug("✅ Client trouvé pour CodeCons '{CodeCons}': IdClient={IdClient}", 
                            dto.CodeCons, dto.IdClient);
                    }
                    else
                    {
                        dto.Erreurs.Add($"Aucun client trouvé avec le CodeCons '{dto.CodeCons}'");
                        _logger.LogWarning("⚠️ Client non trouvé pour CodeCons '{CodeCons}'", dto.CodeCons);
                    }
                }

                // Convertir Montant
                if (!string.IsNullOrWhiteSpace(raw.Montant))
                {
                    if (decimal.TryParse(raw.Montant.Trim(), out var montant))
                    {
                        dto.Montant = montant;
                    }
                    else
                    {
                        dto.Erreurs.Add($"Le montant '{raw.Montant}' n'est pas un nombre valide");
                    }
                }

                // Convertir Mois (normaliser en format "01"-"12")
                if (!string.IsNullOrWhiteSpace(raw.Mois))
                {
                    var moisTrimmed = raw.Mois.Trim();
                    if (int.TryParse(moisTrimmed, out var moisInt))
                    {
                        if (moisInt >= 1 && moisInt <= 12)
                        {
                            dto.Mois = moisInt.ToString("D2"); // "01", "02", ..., "12"
                        }
                        else
                        {
                            dto.Erreurs.Add($"Le mois '{raw.Mois}' doit être entre 1 et 12");
                        }
                    }
                    else
                    {
                        dto.Erreurs.Add($"Le mois '{raw.Mois}' n'est pas un nombre valide");
                    }
                }

                // Convertir Annees
                if (!string.IsNullOrWhiteSpace(raw.Annees))
                {
                    if (int.TryParse(raw.Annees.Trim(), out var annees))
                    {
                        dto.Annees = annees;
                    }
                    else
                    {
                        dto.Erreurs.Add($"L'année '{raw.Annees}' n'est pas un nombre valide");
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Valide les données (Pass 4)
        /// </summary>
        private void ValidateClientFactures(List<ClientFactureExcelDto> data)
        {
            foreach (var dto in data)
            {
                // CodeCons (obligatoire)
                if (string.IsNullOrWhiteSpace(dto.CodeCons))
                {
                    dto.Erreurs.Add("Le CodeCons est obligatoire");
                }

                // IdClient (doit être récupéré)
                if (!dto.IdClient.HasValue)
                {
                    // Erreur déjà ajoutée dans ConvertToClientFactureExcelDtoAsync
                }

                // Montant (obligatoire et > 0)
                if (!dto.Montant.HasValue)
                {
                    dto.Erreurs.Add("Le montant est obligatoire");
                }
                else if (dto.Montant.Value <= 0)
                {
                    dto.Erreurs.Add("Le montant doit être supérieur à 0");
                }

                // Mois (obligatoire)
                if (string.IsNullOrWhiteSpace(dto.Mois))
                {
                    dto.Erreurs.Add("Le mois est obligatoire");
                }

                // Annees (obligatoire et valide)
                if (!dto.Annees.HasValue)
                {
                    dto.Erreurs.Add("L'année est obligatoire");
                }
                else if (dto.Annees.Value < 2000 || dto.Annees.Value > 2100)
                {
                    dto.Erreurs.Add($"L'année doit être entre 2000 et 2100 (valeur: {dto.Annees.Value})");
                }
            }
        }

        /// <summary>
        /// Détecte les doublons dans le fichier (Pass 5)
        /// </summary>
        private void DeduplicateInFile(List<ClientFactureExcelDto> data, BulkClientFactureResult result)
        {
            var seen = new HashSet<string>();

            foreach (var dto in data)
            {
                if (dto.Erreurs.Count > 0)
                    continue;

                // Clé unique : CodeCons + Mois + Annees
                var key = $"{dto.CodeCons?.Trim() ?? ""}_{dto.Mois ?? ""}_{dto.Annees ?? 0}";

                if (string.IsNullOrWhiteSpace(key) || key == "__0")
                {
                    continue;
                }

                if (seen.Contains(key))
                {
                    dto.Erreurs.Add($"Doublon détecté dans le fichier (même CodeCons/Mois/Annees déjà traité)");
                    result.DoublonsDetectes++;
                }
                else
                {
                    seen.Add(key);
                }
            }
        }

        /// <summary>
        /// Traite les ClientFacture par lots (Pass 7)
        /// </summary>
        private async Task ProcessBatchesAsync(
            List<ClientFactureExcelDto> lignesValides,
            BulkClientFactureResult result)
        {
            var batches = lignesValides
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / BatchSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                foreach (var dto in batch)
                {
                    try
                    {
                        // Vérifier si une ClientFacture existe déjà (doublon avec la base)
                        var existing = await _context.ClientFactures
                            .FirstOrDefaultAsync(cf => 
                                cf.IdClient == dto.IdClient &&
                                cf.Mois == dto.Mois &&
                                cf.Annees == dto.Annees &&
                                cf.EstArrierePreExistant == true &&
                                cf.Statut == true);

                        if (existing != null)
                        {
                            result.ClientFacturesCrees.Add(new ClientFactureCree
                            {
                                Success = false,
                                CodeCons = dto.CodeCons,
                                Message = $"Un arriéré pré-existant existe déjà pour ce client (CodeCons: {dto.CodeCons}, Mois: {dto.Mois}, Annees: {dto.Annees}, IdClientFacture: {existing.IdClientFacture})"
                            });
                            result.LignesEchouees++;
                            continue;
                        }

                        // Créer l'arriéré pré-existant
                        var clientFacture = await _clientFactureRepository.CreatePreExistantAsync(
                            dto.IdClient!.Value,
                            dto.Montant!.Value,
                            dto.Mois!,
                            dto.Annees!.Value,
                            null, // Description non fournie dans Excel
                            null  // DateEmission non fournie dans Excel (utilisera DateTime.Now)
                        );

                        result.ClientFacturesCrees.Add(new ClientFactureCree
                        {
                            Success = true,
                            IdClientFacture = clientFacture.IdClientFacture,
                            CodeCons = dto.CodeCons,
                            Message = "Arriéré pré-existant créé avec succès"
                        });

                        result.LignesReussies++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erreur lors de la création de l'arriéré ligne {Ligne}", dto.NumeroLigne);
                        result.ClientFacturesCrees.Add(new ClientFactureCree
                        {
                            Success = false,
                            CodeCons = dto.CodeCons,
                            Message = $"Erreur : {ex.Message}"
                        });
                        result.LignesEchouees++;
                    }
                }
            }
        }

        /// <summary>
        /// ✨ NOUVEAU : Sauvegarde les lignes échouées dans ArriereeCrashed
        /// </summary>
        private async Task SaveFailedLinesAsync(List<ClientFactureExcelDto> lignesInvalides)
        {
            try
            {
                var arriereesCrashedToAdd = new List<ArriereeCrashed>();

                foreach (var dto in lignesInvalides)
                {
                    // Déterminer le type d'erreur principal
                    string typeErreur = "VALIDATION";
                    if (dto.Erreurs.Any(e => e.Contains("CodeCons") || e.Contains("client trouvé")))
                    {
                        typeErreur = "CODE_CONS_NOT_FOUND";
                    }
                    else if (dto.Erreurs.Any(e => e.Contains("Doublon")))
                    {
                        typeErreur = "DUPLICATE";
                    }
                    else if (dto.Erreurs.Any(e => e.Contains("montant") || e.Contains("Mois") || e.Contains("année")))
                    {
                        typeErreur = "VALIDATION";
                    }

                    // Créer l'objet ArriereeCrashed
                    var arriereeCrashed = new ArriereeCrashed
                    {
                        NumeroLigne = dto.NumeroLigne,
                        CodeCons = dto.CodeCons,
                        Montant = dto.Montant?.ToString(),
                        Mois = dto.Mois,
                        Annees = dto.Annees?.ToString(),
                        IdClient = dto.IdClient,
                        MessageErreur = string.Join("; ", dto.Erreurs),
                        TypeErreur = typeErreur,
                        ErreursJson = JsonSerializer.Serialize(dto.Erreurs),
                        Statut = "EN_ATTENTE",
                        DateCreation = DateTime.Now
                    };

                    // Sauvegarder les données brutes en JSON
                    var donneesBrutes = new
                    {
                        CodeCons = dto.CodeCons,
                        Montant = dto.Montant?.ToString(),
                        Mois = dto.Mois,
                        Annees = dto.Annees?.ToString(),
                        IdClient = dto.IdClient
                    };
                    arriereeCrashed.DonneesBrutesJson = JsonSerializer.Serialize(donneesBrutes);

                    arriereesCrashedToAdd.Add(arriereeCrashed);
                }

                // Sauvegarder en lot pour performance
                if (arriereesCrashedToAdd.Count > 0)
                {
                    await _context.ArriereesCrashed.AddRangeAsync(arriereesCrashedToAdd);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ {Count} ligne(s) échouée(s) sauvegardée(s) dans ArriereeCrashed", arriereesCrashedToAdd.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la sauvegarde des lignes échouées dans ArriereeCrashed");
                // Ne pas faire échouer tout le processus si la sauvegarde des erreurs échoue
            }
        }

        /// <summary>
        /// Génère le message de résultat
        /// </summary>
        private string GenerateResultMessage(BulkClientFactureResult result)
        {
            if (result.LignesReussies == 0 && result.LignesEchouees == 0)
            {
                return "Aucune ligne à traiter";
            }

            if (result.LignesReussies == 0)
            {
                return $"Aucun arriéré créé : {result.LignesEchouees} erreur(s) sur {result.TotalLignes} ligne(s)";
            }

            if (result.LignesEchouees == 0)
            {
                return $"Traitement terminé : {result.LignesReussies} arriéré(s) créé(s) avec succès sur {result.TotalLignes} ligne(s)";
            }

            return $"Traitement terminé : {result.LignesReussies} arriéré(s) créé(s) sur {result.TotalLignes} ligne(s), {result.LignesEchouees} échouée(s)";
        }

        /// <summary>
        /// Génère un template Excel pour faciliter la saisie
        /// </summary>
        public byte[] GenerateTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Arrieres");

                // Ligne d'instructions
                worksheet.Cells[1, 1].Value = "INSTRUCTIONS:";
                worksheet.Cells[1, 2].Value = "Remplissez les colonnes suivantes : CodeCons (obligatoire), Montant (obligatoire, nombre > 0), Mois (obligatoire, 1-12), Annees (obligatoire, ex: 2025). Le CodeCons doit correspondre à un client existant dans le système.";
                using (var instructionRange = worksheet.Cells[1, 1, 1, 4])
                {
                    instructionRange.Merge = true;
                    instructionRange.Style.Font.Italic = true;
                    instructionRange.Style.Font.Size = 10;
                    instructionRange.Style.Font.Color.SetColor(System.Drawing.Color.DarkBlue);
                    instructionRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    instructionRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                    instructionRange.Style.WrapText = true;
                    instructionRange.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;
                }
                worksheet.Row(1).Height = 30;

                // En-têtes (ligne 2)
                worksheet.Cells[2, 1].Value = "CodeCons";
                worksheet.Cells[2, 2].Value = "Montant";
                worksheet.Cells[2, 3].Value = "Mois";
                worksheet.Cells[2, 4].Value = "Annees";

                // Mettre en forme les en-têtes
                using (var range = worksheet.Cells[2, 1, 2, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Largeur des colonnes
                worksheet.Column(1).Width = 20; // CodeCons
                worksheet.Column(2).Width = 15; // Montant
                worksheet.Column(3).Width = 10; // Mois
                worksheet.Column(4).Width = 12; // Annees

                // Exemples de données (ligne 3)
                worksheet.Cells[3, 1].Value = "B/b1/0001";
                worksheet.Cells[3, 2].Value = 100000;
                worksheet.Cells[3, 3].Value = 9;
                worksheet.Cells[3, 4].Value = 2025;

                // Exemple supplémentaire (ligne 4)
                worksheet.Cells[4, 1].Value = "A/a1/0002";
                worksheet.Cells[4, 2].Value = 50000;
                worksheet.Cells[4, 3].Value = 8;
                worksheet.Cells[4, 4].Value = 2025;

                return package.GetAsByteArray();
            }
        }
    }
}
