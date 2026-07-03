using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Client;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour l'import en masse de clients depuis un fichier Excel
    /// </summary>
    public class ExcelClientService
    {
        private readonly KenergieDbContext _context;
        private readonly IClientRepository _clientRepository;
        private readonly ICategorieClientRepository _categorieClientRepository;
        private readonly ILogger<ExcelClientService> _logger;
        private const int MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private const int BatchSize = 50; // Traiter 50 clients à la fois

        public ExcelClientService(
            KenergieDbContext context,
            IClientRepository clientRepository,
            ICategorieClientRepository categorieClientRepository,
            ILogger<ExcelClientService> logger)
        {
            _context = context;
            _clientRepository = clientRepository;
            _categorieClientRepository = categorieClientRepository;
            _logger = logger;
        }

        /// <summary>
        /// Point d'entrée principal pour traiter un fichier Excel
        /// </summary>
        public async Task<BulkClientResult> ProcessExcelFileAsync(
            IFormFile file,
            int idSociete)
        {
            var result = new BulkClientResult();

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

                // Pass 2: Validation des paramètres
                var paramValidation = await ValidateParametersAsync(idSociete);
                if (!paramValidation.IsValid)
                {
                    result.Success = false;
                    result.Message = paramValidation.ErrorMessage;
                    return result;
                }

                // Pass 3: Lecture du fichier Excel
                var rawData = await ReadExcelFileAsync(file);
                if (rawData == null || rawData.Count == 0)
                {
                    result.Success = false;
                    result.Message = "Le fichier Excel est vide ou ne contient pas de données valides.";
                    return result;
                }

                result.TotalLignes = rawData.Count;

                // Pass 4: Conversion et enrichissement
                var enrichedData = await ConvertToClientExcelDtoAsync(rawData, idSociete);

                // Pass 5: Validation des données
                ValidateClients(enrichedData);

                // Pass 6: Déduplication
                DeduplicateInFile(enrichedData, result);

                // Pass 7: Séparation des lignes valides/invalides
                var lignesValides = enrichedData.Where(d => d.Erreurs.Count == 0).ToList();
                var lignesInvalides = enrichedData.Where(d => d.Erreurs.Count > 0).ToList();

                result.LignesAvecErreurs = lignesInvalides.Select(l => new LigneErreurClient
                {
                    NumeroLigne = l.NumeroLigne,
                    NomClient = l.NomClient,
                    Erreurs = l.Erreurs
                }).ToList();

                result.LignesEchouees = lignesInvalides.Count;

                // ✨ NOUVEAU : Stocker les lignes invalides dans clientsCrashed
                if (lignesInvalides.Any())
                {
                    await SaveCrashedClientsAsync(lignesInvalides, idSociete, "VALIDATION");
                }

                // Pass 8: Traitement par lots
                if (lignesValides.Count > 0)
                {
                    await ProcessBatchesAsync(lignesValides, idSociete, result);
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
        /// Valide les paramètres (Pass 2)
        /// </summary>
        private async Task<(bool IsValid, string ErrorMessage)> ValidateParametersAsync(int idSociete)
        {
            var societe = await _context.Societes
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete && s.Statut == true);

            if (societe == null)
            {
                return (false, $"La société {idSociete} n'existe pas ou est désactivée");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Lit le fichier Excel et retourne les données brutes (Pass 3)
        /// </summary>
        private async Task<List<ClientExcelRaw>> ReadExcelFileAsync(IFormFile file)
        {
            var rawData = new List<ClientExcelRaw>();
            var requiredColumns = new[] { "NomClient", "AdresseClient" };

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
                    
                    // Vérifier si la ligne 1 contient "INSTRUCTIONS" (template amélioré)
                    var firstCell = worksheet.Cells[1, 1].Value?.ToString();
                    if (firstCell != null && firstCell.Contains("INSTRUCTIONS", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRow = 2; // Les en-têtes sont à la ligne 2
                        startRow = 3; // Commencer à la ligne 3 si instructions présentes
                    }

                    // Lire les en-têtes depuis la bonne ligne
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
                    
                    // Lire les données (en sautant la ligne d'instructions si elle existe)
                    for (int row = startRow; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var raw = new ClientExcelRaw
                        {
                            NomClient = GetCellValue(worksheet, row, headers, "NomClient"),
                            AdresseClient = GetCellValue(worksheet, row, headers, "AdresseClient"),
                            Telephone = GetCellValue(worksheet, row, headers, "Telephone"),
                            EmailClient = GetCellValue(worksheet, row, headers, "EmailClient"),
                            GenreClient = GetCellValue(worksheet, row, headers, "GenreClient"),
                            CodeCons = GetCellValue(worksheet, row, headers, "CodeCons"),
                            LibelleUsage = GetCellValue(worksheet, row, headers, "LibelleUsage")
                        };

                        // Ne pas ajouter les lignes complètement vides
                        if (!string.IsNullOrWhiteSpace(raw.NomClient) || !string.IsNullOrWhiteSpace(raw.AdresseClient))
                        {
                            // Stocker le numéro de ligne réel dans le fichier Excel pour les erreurs
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

            var cellValue = worksheet.Cells[row, headers[columnName]].Value;
            if (cellValue == null)
                return null;

            // Gérer les dates
            if (cellValue is DateTime dateValue)
            {
                return dateValue.ToString("yyyy-MM-dd");
            }

            var stringValue = cellValue.ToString()?.Trim();
            // ✨ Retourner null si la chaîne est vide après Trim()
            return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
        }

        /// <summary>
        /// Convertit et enrichit les données avec les IDs (Pass 4)
        /// </summary>
        private async Task<List<ClientExcelDto>> ConvertToClientExcelDtoAsync(
            List<ClientExcelRaw> rawData,
            int idSociete)
        {
            var result = new List<ClientExcelDto>();

            // Charger tous les usages de la société en mémoire pour optimiser
            var usages = await _context.Usages
                .Include(u => u.CategorieClient)
                .Where(u => u.CategorieClient != null && u.CategorieClient.IdSociete == idSociete)
                .ToListAsync();
            
            var usagesDict = usages.ToDictionary(
                u => u.Libelle?.Trim() ?? "", 
                u => u, 
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < rawData.Count; i++)
            {
                var raw = rawData[i];
                // ✨ Fonction helper pour normaliser les valeurs vides en null
                // Gère tous les cas : null, chaîne vide, espaces, caractères invisibles
                string? NormalizeToNull(string? value)
                {
                    if (value == null) return null;
                    // Supprimer tous les espaces et caractères invisibles
                    var trimmed = value.Trim();
                    // Vérifier si la chaîne est vide après trim
                    if (string.IsNullOrWhiteSpace(trimmed)) return null;
                    // Vérifier aussi si la chaîne ne contient que des caractères invisibles
                    if (trimmed.Length == 0) return null;
                    return trimmed;
                }

                var dto = new ClientExcelDto
                {
                    NumeroLigne = i + 2, // +2 car ligne 1 = en-têtes/instructions, et index 0-based
                    NomClient = raw.NomClient?.Trim(),
                    AdresseClient = raw.AdresseClient?.Trim(),
                    // ✨ Convertir les chaînes vides en null pour Telephone, EmailClient, GenreClient
                    Telephone = NormalizeToNull(raw.Telephone),
                    EmailClient = NormalizeToNull(raw.EmailClient),
                    GenreClient = NormalizeToNull(raw.GenreClient)?.ToUpper(),
                    CodeCons = raw.CodeCons?.Trim()
                };

                // Résoudre les usages si fournis (format: "Usage1, Usage2" ou "Usage1; Usage2")
                if (!string.IsNullOrWhiteSpace(raw.LibelleUsage))
                {
                    // Séparer les usages par virgule ou point-virgule
                    var libellesUsages = raw.LibelleUsage
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

                    // Résoudre chaque usage (nombreBatiment sera défini à 1 par défaut lors de la création)
                    for (int j = 0; j < libellesUsages.Count; j++)
                    {
                        var libelleUsage = libellesUsages[j];
                        var nombreBat = 1; // Valeur par défaut : 1 bâtiment par usage

                        if (usagesDict.TryGetValue(libelleUsage, out var usage))
                        {
                            dto.Usages.Add(new ClientExcelDto.UsageInfo
                            {
                                IdUsage = usage.IdUsage,
                                Libelle = usage.Libelle,
                                nombreBatiment = nombreBat
                            });
                        }
                        else
                        {
                            // Suggérer des usages similaires
                            var usagesSimilaires = usages
                                .Where(u => u.Libelle != null && 
                                           u.Libelle.Contains(libelleUsage, StringComparison.OrdinalIgnoreCase))
                                .Take(3)
                                .Select(u => u.Libelle)
                                .ToList();

                            var messageErreur = $"L'usage '{libelleUsage}' n'existe pas pour cette société";
                            if (usagesSimilaires.Any())
                            {
                                messageErreur += $". Usages similaires disponibles : {string.Join(", ", usagesSimilaires)}";
                            }
                            else
                            {
                                var tousUsages = usages
                                    .Where(u => u.Libelle != null)
                                    .Select(u => u.Libelle!)
                                    .Take(5)
                                    .ToList();
                                if (tousUsages.Any())
                                {
                                    messageErreur += $". Usages disponibles : {string.Join(", ", tousUsages)}";
                                }
                            }
                            dto.Erreurs.Add(messageErreur);
                        }
                    }
                }

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Valide les données des clients (Pass 5)
        /// </summary>
        private void ValidateClients(List<ClientExcelDto> clients)
        {
            foreach (var client in clients)
            {
                // NomClient (obligatoire)
                if (string.IsNullOrWhiteSpace(client.NomClient))
                {
                    client.Erreurs.Add("Le nom du client est obligatoire");
                }
                else if (client.NomClient.Length > 200)
                {
                    client.Erreurs.Add("Le nom du client ne peut pas dépasser 200 caractères");
                }

                // AdresseClient (obligatoire)
                if (string.IsNullOrWhiteSpace(client.AdresseClient))
                {
                    client.Erreurs.Add("L'adresse du client est obligatoire");
                }
                else if (client.AdresseClient.Length > 500)
                {
                    client.Erreurs.Add("L'adresse du client ne peut pas dépasser 500 caractères");
                }

                // Telephone (optionnel - accepte null et chaîne vide)
                // ✨ Ne valider QUE si le champ n'est pas null et n'est pas vide après trim
                if (client.Telephone != null && !string.IsNullOrWhiteSpace(client.Telephone))
                {
                    var trimmedPhone = client.Telephone.Trim();
                    if (trimmedPhone.Length > 20)
                    {
                        client.Erreurs.Add("Le téléphone ne peut pas dépasser 20 caractères");
                    }
                    else if (!Regex.IsMatch(trimmedPhone, @"^[\d\s\+\-\(\)]+$"))
                    {
                        client.Erreurs.Add("Le format du téléphone n'est pas valide");
                    }
                }
                // Si null ou vide, on accepte sans erreur (pas de validation)

                // EmailClient (optionnel - accepte null et chaîne vide)
                // ✨ Ne valider QUE si le champ n'est pas null et n'est pas vide après trim
                if (client.EmailClient != null && !string.IsNullOrWhiteSpace(client.EmailClient))
                {
                    var trimmedEmail = client.EmailClient.Trim();
                    if (trimmedEmail.Length > 256)
                    {
                        client.Erreurs.Add("L'email ne peut pas dépasser 256 caractères");
                    }
                    else if (!IsValidEmail(trimmedEmail))
                    {
                        client.Erreurs.Add("L'email du client n'est pas valide");
                    }
                }
                // Si null ou vide, on accepte sans erreur (pas de validation)

                // GenreClient (optionnel - accepte null et chaîne vide)
                // ✨ Ne valider QUE si le champ n'est pas null et n'est pas vide après trim
                if (client.GenreClient != null)
                {
                    var trimmedGenre = client.GenreClient.Trim();
                    // Si vide après trim, on accepte sans validation
                    if (string.IsNullOrWhiteSpace(trimmedGenre))
                    {
                        // Accepter null/vide sans erreur
                    }
                    else
                    {
                        // Valider seulement si la valeur n'est pas vide
                        var upperGenre = trimmedGenre.ToUpper();
                        if (upperGenre != "M" && upperGenre != "F")
                        {
                            client.Erreurs.Add("Le genre du client doit être M ou F");
                        }
                    }
                }
                // Si null, on accepte sans erreur (pas de validation)

                // CodeCons (optionnel)
                if (!string.IsNullOrWhiteSpace(client.CodeCons))
                {
                    if (client.CodeCons.Length > 100)
                    {
                        client.Erreurs.Add("Le code consommateur ne peut pas dépasser 100 caractères");
                    }
                }


                // Validation des usages
                if (client.Usages.Count == 0)
                {
                    // Usage non obligatoire mais recommandé - pas d'erreur, juste un avertissement
                    // Si vous voulez rendre l'usage obligatoire, décommentez la ligne suivante:
                    // client.Erreurs.Add("Au moins un usage doit être spécifié");
                }

                // Usage supprimé - maintenant dans CategorieClient
            }
        }

        /// <summary>
        /// Vérifie si un email est valide
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Détecte les doublons dans le fichier (Pass 6)
        /// </summary>
        private void DeduplicateInFile(List<ClientExcelDto> clients, BulkClientResult result)
        {
            var seen = new HashSet<string>();

            foreach (var client in clients)
            {
                // ✨ Clé unique basée sur CodeCons (seul champ unique)
                var key = client.CodeCons?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(key))
                {
                    // Si CodeCons n'est pas fourni, on ne peut pas détecter les doublons ici
                    // Il sera généré automatiquement lors de la création du client
                    continue;
                }

                if (seen.Contains(key))
                {
                    client.Erreurs.Add($"Doublon détecté dans le fichier (même CodeCons déjà traité: {key})");
                    result.DoublonsDetectes++;
                }
                else
                {
                    seen.Add(key);
                }
            }
        }

        /// <summary>
        /// Traite les clients par lots (Pass 8)
        /// </summary>
        private async Task ProcessBatchesAsync(
            List<ClientExcelDto> lignesValides,
            int idSociete,
            BulkClientResult result)
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
                        // ✨ Vérifier si le client existe déjà (par CodeCons - seul champ unique)
                        Client? existingClient = null;

                        if (!string.IsNullOrWhiteSpace(dto.CodeCons))
                        {
                            existingClient = await _clientRepository.GetByCodeConsAsync(dto.CodeCons);
                        }

                        if (existingClient != null)
                        {
                            result.ClientsCrees.Add(new ClientCree
                            {
                                Success = false,
                                NomClient = dto.NomClient,
                                Message = $"Un client avec ce CodeCons existe déjà (CodeCons: {dto.CodeCons}, ID: {existingClient.IdClient})"
                            });
                            result.LignesEchouees++;
                            continue;
                        }

                        // Préparer le client
                        var client = new Client
                        {
                            NomClient = dto.NomClient!,
                            AdresseClient = dto.AdresseClient!,
                            Telephone = dto.Telephone,
                            EmailClient = dto.EmailClient,
                            GenreClient = dto.GenreClient,
                            CodeCons = dto.CodeCons,
                            Statut = true,
                            IsActif = true
                        };

                        // Préparer la liste des usages pour CreateWithUsagesAsync
                        var usagesList = dto.Usages
                            .Select(u => (u.Libelle, u.nombreBatiment, (int?)null))
                            .ToList();

                        // Créer le client avec ses usages en une seule transaction
                        Client created;
                        int usagesAjoutes = 0;
                        
                        if (usagesList.Count > 0)
                        {
                            // Utiliser CreateWithUsagesAsync pour créer le client et ses usages en une transaction
                            created = await _clientRepository.CreateWithUsagesAsync(client, usagesList);
                            usagesAjoutes = usagesList.Count;
                        }
                        else
                        {
                            // Si aucun usage n'est fourni, créer le client sans usages
                            created = await _clientRepository.CreateAsync(client);
                        }

                        result.ClientsCrees.Add(new ClientCree
                        {
                            Success = true,
                            IdClient = created.IdClient,
                            NomClient = created.NomClient,
                            Message = usagesAjoutes > 0 
                                ? $"Client créé avec succès ({usagesAjoutes} usage(s) assigné(s))"
                                : "Client créé avec succès (aucun usage assigné)"
                        });

                        result.LignesReussies++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erreur lors de la création du client ligne {Ligne}", dto.NumeroLigne);
                        
                        // ✨ NOUVEAU : Stocker l'erreur dans clientsCrashed
                        await SaveCrashedClientAsync(dto, idSociete, ex, "DATABASE");
                        
                        result.ClientsCrees.Add(new ClientCree
                        {
                            Success = false,
                            NomClient = dto.NomClient,
                            Message = $"Erreur : {ex.Message}"
                        });
                        result.LignesEchouees++;
                    }
                }
            }
        }

        /// <summary>
        /// Génère le message de résultat
        /// </summary>
        private string GenerateResultMessage(BulkClientResult result)
        {
            if (result.LignesReussies == 0 && result.LignesEchouees == 0)
            {
                return "Aucune ligne à traiter";
            }

            if (result.LignesReussies == 0)
            {
                return $"Aucun client créé : {result.LignesEchouees} erreur(s) sur {result.TotalLignes} ligne(s)";
            }

            if (result.LignesEchouees == 0)
            {
                return $"Traitement terminé : {result.LignesReussies} client(s) créé(s) avec succès sur {result.TotalLignes} ligne(s)";
            }

            return $"Traitement terminé : {result.LignesReussies} client(s) créé(s) sur {result.TotalLignes} ligne(s), {result.LignesEchouees} échouée(s)";
        }

        /// <summary>
        /// Génère un template Excel pour faciliter la saisie
        /// </summary>
        public byte[] GenerateTemplate()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Clients");

                // Ligne d'instructions (optionnelle - peut être supprimée par l'utilisateur)
                worksheet.Cells[1, 1].Value = "INSTRUCTIONS:";
                worksheet.Cells[1, 2].Value = "Pour LibelleUsage, vous pouvez mettre plusieurs usages séparés par virgule (,) ou point-virgule (;). Exemple: 'Résidentiel, Commercial' ou 'Résidentiel; Commercial'. Le nombre de bâtiments sera défini à 1 par défaut pour chaque usage. Les usages seront créés en même temps que le client dans une transaction atomique. Vous pourrez modifier le nombre de bâtiments ultérieurement via l'API.";
                using (var instructionRange = worksheet.Cells[1, 1, 1, 7])
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
                worksheet.Row(1).Height = 30; // Hauteur pour le texte sur plusieurs lignes

                // En-têtes (ligne 2)
                worksheet.Cells[2, 1].Value = "NomClient";
                worksheet.Cells[2, 2].Value = "AdresseClient";
                worksheet.Cells[2, 3].Value = "Telephone";
                worksheet.Cells[2, 4].Value = "EmailClient";
                worksheet.Cells[2, 5].Value = "GenreClient";
                worksheet.Cells[2, 6].Value = "CodeCons";
                worksheet.Cells[2, 7].Value = "LibelleUsage";

                // Mettre en forme les en-têtes
                using (var range = worksheet.Cells[2, 1, 2, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Largeur des colonnes
                worksheet.Column(1).Width = 25; // NomClient
                worksheet.Column(2).Width = 40; // AdresseClient
                worksheet.Column(3).Width = 15; // Telephone
                worksheet.Column(4).Width = 30; // EmailClient
                worksheet.Column(5).Width = 12; // GenreClient
                worksheet.Column(6).Width = 20; // CodeCons
                worksheet.Column(7).Width = 30; // LibelleUsage

                // Exemple de données (ligne 3) - Basé sur le format réel fourni
                worksheet.Cells[3, 1].Value = "KAMITUGA ELIAS WATANGA";
                worksheet.Cells[3, 2].Value = "KIKINDI";
                worksheet.Cells[3, 3].Value = "+243900000000";
                worksheet.Cells[3, 4].Value = "kamituga@email.com";
                worksheet.Cells[3, 5].Value = "M";
                worksheet.Cells[3, 6].Value = "A/a1/0001";
                worksheet.Cells[3, 7].Value = "Résidentiel"; // Exemple avec un seul usage

                // Exemple avec plusieurs usages (ligne 4) - Basé sur le format réel fourni
                worksheet.Cells[4, 1].Value = "MULONDA SAFARI";
                worksheet.Cells[4, 2].Value = "KIKINDI";
                worksheet.Cells[4, 3].Value = "+243900000001";
                worksheet.Cells[4, 4].Value = "mulonda@email.com";
                worksheet.Cells[4, 5].Value = "M";
                worksheet.Cells[4, 6].Value = "A/a1/0002";
                worksheet.Cells[4, 7].Value = "Résidentiel, Commercial"; // Exemple avec plusieurs usages séparés par virgule

                // Exemple supplémentaire (ligne 5) - Basé sur le format réel fourni
                worksheet.Cells[5, 1].Value = "BURUME MANEMA";
                worksheet.Cells[5, 2].Value = "KIKINDI";
                worksheet.Cells[5, 3].Value = "+243900000002";
                worksheet.Cells[5, 4].Value = "burume@email.com";
                worksheet.Cells[5, 5].Value = "M";
                worksheet.Cells[5, 6].Value = "A/a1/0003";
                worksheet.Cells[5, 7].Value = "Industriel";

                return package.GetAsByteArray();
            }
        }

        /// <summary>
        /// ✨ NOUVEAU : Sauvegarde une ligne échouée dans clientsCrashed
        /// </summary>
        private async Task SaveCrashedClientAsync(
            ClientExcelDto dto,
            int idSociete,
            Exception exception,
            string typeErreur)
        {
            try
            {
                // Récupérer les données brutes originales (si disponibles)
                var donneesBrutes = new
                {
                    NomClient = dto.NomClient,
                    AdresseClient = dto.AdresseClient,
                    Telephone = dto.Telephone,
                    EmailClient = dto.EmailClient,
                    GenreClient = dto.GenreClient,
                    CodeCons = dto.CodeCons,
                    LibelleUsage = string.Join(", ", dto.Usages.Select(u => u.Libelle))
                };

                var clientCrashed = new ClientCrashed
                {
                    IdSociete = idSociete,
                    NumeroLigne = dto.NumeroLigne,
                    NomClient = dto.NomClient,
                    AdresseClient = dto.AdresseClient,
                    Telephone = dto.Telephone,
                    EmailClient = dto.EmailClient,
                    GenreClient = dto.GenreClient,
                    CodeCons = dto.CodeCons,
                    LibelleUsage = string.Join(", ", dto.Usages.Select(u => u.Libelle)),
                    DonneesBrutesJson = JsonSerializer.Serialize(donneesBrutes),
                    MessageErreur = exception.Message,
                    TypeErreur = typeErreur,
                    ErreursJson = dto.Erreurs.Any() 
                        ? JsonSerializer.Serialize(dto.Erreurs) 
                        : JsonSerializer.Serialize(new[] { exception.Message }),
                    Statut = "EN_ATTENTE",
                    DateCreation = DateTime.Now
                };

                _context.ClientsCrashed.Add(clientCrashed);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Ligne échouée sauvegardée dans clientsCrashed: Ligne {Ligne}, Type: {TypeErreur}", 
                    dto.NumeroLigne, typeErreur);
            }
            catch (Exception saveEx)
            {
                // Ne pas faire échouer l'import si la sauvegarde de l'erreur échoue
                _logger.LogError(saveEx, "❌ Erreur lors de la sauvegarde dans clientsCrashed pour la ligne {Ligne}", dto.NumeroLigne);
            }
        }

        /// <summary>
        /// ✨ NOUVEAU : Sauvegarde plusieurs lignes échouées (validation) dans clientsCrashed
        /// </summary>
        private async Task SaveCrashedClientsAsync(
            List<ClientExcelDto> lignesInvalides,
            int idSociete,
            string typeErreur)
        {
            if (!lignesInvalides.Any())
                return;

            try
            {
                var clientsCrashedToAdd = new List<ClientCrashed>();

                foreach (var dto in lignesInvalides)
                {
                    var donneesBrutes = new
                    {
                        NomClient = dto.NomClient,
                        AdresseClient = dto.AdresseClient,
                        Telephone = dto.Telephone,
                        EmailClient = dto.EmailClient,
                        GenreClient = dto.GenreClient,
                        CodeCons = dto.CodeCons,
                        LibelleUsage = string.Join(", ", dto.Usages.Select(u => u.Libelle))
                    };

                    var clientCrashed = new ClientCrashed
                    {
                        IdSociete = idSociete,
                        NumeroLigne = dto.NumeroLigne,
                        NomClient = dto.NomClient,
                        AdresseClient = dto.AdresseClient,
                        Telephone = dto.Telephone,
                        EmailClient = dto.EmailClient,
                        GenreClient = dto.GenreClient,
                        CodeCons = dto.CodeCons,
                        LibelleUsage = string.Join(", ", dto.Usages.Select(u => u.Libelle)),
                        DonneesBrutesJson = JsonSerializer.Serialize(donneesBrutes),
                        MessageErreur = string.Join("; ", dto.Erreurs),
                        TypeErreur = typeErreur,
                        ErreursJson = JsonSerializer.Serialize(dto.Erreurs),
                        Statut = "EN_ATTENTE",
                        DateCreation = DateTime.Now
                    };

                    clientsCrashedToAdd.Add(clientCrashed);
                }

                if (clientsCrashedToAdd.Any())
                {
                    _context.ClientsCrashed.AddRange(clientsCrashedToAdd);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ {Count} ligne(s) échouée(s) sauvegardée(s) dans clientsCrashed", clientsCrashedToAdd.Count);
                }
            }
            catch (Exception ex)
            {
                // Ne pas faire échouer l'import si la sauvegarde des erreurs échoue
                _logger.LogError(ex, "❌ Erreur lors de la sauvegarde en batch dans clientsCrashed");
            }
        }
    }
}
