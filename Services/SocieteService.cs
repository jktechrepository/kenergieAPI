using System;
using System.Collections.Generic;
using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using KenergieAPI.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Kenergie.Models.DTOs.Pagination;
using System.Linq;

namespace Kenergie.Services
{
    public class SocieteService : ISocieteRepository
    {
        private readonly KenergieDbContext _context;
        private readonly IEmailService _emailService;

        public SocieteService(KenergieDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IEnumerable<Societe>> GetAllAsync()
        {
            return await _context.Societes
               // .Include(e => e.Classes)
              //  .Include(e => e.Utilisateurs)
                //.Include(e => e.Tuteurs)
               // .Include(e => e.Agents)
               // .Include(e => e.Sections)
               // .Include(e => e.AnneeScolaires)
               // .Include(e => e.Inscriptions)
               // .Include(e => e.GroupesMessages)
                .Where(e => e.Statut == true) // ? Filtrer uniquement les �coles actives
                .ToListAsync();
        }

        public async Task<Societe> GetByIdAsync(int id)
        {
            return await _context.Societes
               // .Include(e => e.Classes)
               // .Include(e => e.Utilisateurs)
               // .Include(e => e.Tuteurs)
               // .Include(e => e.Agents)
               // .Include(e => e.Sections)
               // .Include(e => e.AnneeScolaires)
               // .Include(e => e.Inscriptions)
               // .Include(e => e.GroupesMessages)
                .Where(e => e.Statut == true) // ? Filtrer uniquement les �coles actives
                .FirstOrDefaultAsync(e => e.IdSociete == id);
        }

        public async Task<Societe> GetByNomAsync(string nom)
        {
            return await _context.Societes
                //.Include(e => e.Classes)
               // .Include(e => e.Utilisateurs)
               // .Include(e => e.Tuteurs)
               // .Include(e => e.Agents)
               // .Include(e => e.Sections)
               // .Include(e => e.AnneeScolaires)
               // .Include(e => e.Inscriptions)
               // .Include(e => e.GroupesMessages)
                .Where(e => e.Statut == true) // ? Filtrer uniquement les �coles actives
                .FirstOrDefaultAsync(e => e.Nom == nom);
        }

        //public async Task<Societe> GetByCodeAsync(string code)
        //{
        //    return await _context.Societes
        //        .Include(e => e.Classes)
        //        .Include(e => e.Utilisateurs)
        //        .Include(e => e.Tuteurs)
        //        .Include(e => e.Caissiers)
        //        .Include(e => e.Sections)
        //        .Include(e => e.AnneeScolaires)
        //        .Include(e => e.Inscriptions)
        //        .Include(e => e.GroupesMessages)
        //        .FirstOrDefaultAsync(e => e.Code == code);
        //}

        //public async Task<IEnumerable<Societe>> GetByStatutAsync(bool statut)
        //{
        //    return await _context.Societes
        //        .Include(e => e.Classes)
        //        .Include(e => e.Utilisateurs)
        //        .Include(e => e.Tuteurs)
        //        .Include(e => e.Caissiers)
        //        .Include(e => e.Sections)
        //        .Include(e => e.AnneeScolaires)
        //        .Include(e => e.Inscriptions)
        //        .Include(e => e.GroupesMessages)
        //        .Where(e => e.Statut == statut)
        //        .ToListAsync();
        //}

        public async Task<Societe> CreateAsync(Societe societe)
        {
            societe.DateCreation = DateTime.Now;
            if (string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale))
                societe.CodeDevisePrincipale = "CDF";
            
            _context.Societes.Add(societe);
            await _context.SaveChangesAsync();

            var codePrincipale = societe.CodeDevisePrincipale.Trim().ToUpperInvariant();
            var deviseExists = await _context.DevisesMonetaires
                .AnyAsync(d => d.IdSociete == societe.IdSociete && d.CodeDevise == codePrincipale);
            if (!deviseExists)
            {
                _context.DevisesMonetaires.Add(new DeviseMonetaire
                {
                    IdSociete = societe.IdSociete,
                    CodeDevise = codePrincipale,
                    Libelle = codePrincipale == "CDF" ? "Franc congolais" : codePrincipale,
                    Symbole = codePrincipale == "CDF" ? "FC" : codePrincipale,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            
            // ? LOGIQUE CORRIG�E : Cr�er d'abord un Agent (gérant), puis un Utilisateur li� � cet Agent
            await CreateDefaultGerantAgentAsync(societe);
            
            return societe;
        }

        public async Task<Societe> UpdateAsync(Societe societe)
        {
            var existingSociete = await _context.Societes.FindAsync(societe.IdSociete);
            if (existingSociete == null)
                return null;

            _context.Entry(existingSociete).CurrentValues.SetValues(societe);
            await _context.SaveChangesAsync();
            return existingSociete;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var societe = await _context.Societes.FindAsync(id);
            if (societe == null)
                return false;

            _context.Societes.Remove(societe);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Societes.AnyAsync(e => e.IdSociete == id);
        }

        public async Task<bool> ExistsByNomAsync(string nom)
        {
            return await _context.Societes.AnyAsync(e => e.Nom == nom);
        }

        //public async Task<bool> ExistsByCodeAsync(string code)
        //{
        //    return await _context.Societes.AnyAsync(e => e.Code == code);
        //}

        public async Task<IEnumerable<Utilisateur>> GetUtilisateursAsync(int idSociete)
        {
            return await _context.Utilisateurs
                .Include(u => u.Role)
                .Where(u => u.IdSociete == idSociete)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agent>> GetAgentsAsync(int idSociete)
        {
            return await _context.Agents
                .Where(e => e.IdSociete == idSociete)
                .ToListAsync();
        }

        public async Task<PagedResult<Agent>> GetAgentsByRoleAsync(int idSociete, string roleNom, PagedRequest request)
        {
            request ??= new PagedRequest();

            if (string.IsNullOrWhiteSpace(roleNom))
            {
                return new PagedResult<Agent>(new List<Agent>(), 0, request.PageNumber, request.PageSize);
            }

            var normalizedRole = roleNom.Trim().ToLower();

            var query = _context.Agents
                .Where(a => a.IdSociete == idSociete &&
                            a.Statut == true &&
                            !string.IsNullOrEmpty(a.RoleAgent) &&
                            a.RoleAgent.ToLower() == normalizedRole);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    (a.NomComplet ?? string.Empty).ToLower().Contains(term) ||
                    (a.EmailAgent ?? string.Empty).ToLower().Contains(term) ||
                    (a.TelephoneAgent ?? string.Empty).ToLower().Contains(term) ||
                    (a.Fonction ?? string.Empty).ToLower().Contains(term));
            }

            query = request.SortBy switch
            {
                "NomComplet" => request.SortDescending ? query.OrderByDescending(a => a.NomComplet) : query.OrderBy(a => a.NomComplet),
                "DateCreation" => request.SortDescending ? query.OrderByDescending(a => a.DateCreation) : query.OrderBy(a => a.DateCreation),
                _ => request.SortDescending ? query.OrderByDescending(a => a.IdAgent) : query.OrderBy(a => a.IdAgent)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Agent>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// ? NOUVELLE LOGIQUE : Cr�e automatiquement un Agent (Manager G�n�ral) et son compte Utilisateur
        /// lors de la cr�ation d'une �cole
        /// 
        /// PROCESSUS :
        /// 1. Cr�er un Agent avec la fonction "Manager G�n�ral"
        /// 2. Cr�er un Utilisateur li� � cet Agent avec le r�le "Admin"
        /// 
        /// Cette approche respecte la logique m�tier :
        /// - Un Utilisateur est soit un Agent, soit un Technicien
        /// - Le Manager G�n�ral est un Agent avec des droits Admin sur toute l'�cole
        /// </summary>
        private async Task CreateDefaultGerantAgentAsync(Societe societe)
        {
            try
            {
                // ? V�RIFICATION UNICIT� EMAIL : V�rifier si l'email existe d�j�
                string emailGerant = societe.EmailContact?.Trim() ?? "";
                
                if (!string.IsNullOrEmpty(emailGerant))
                {
                    var emailExists = await _context.Utilisateurs.AnyAsync(u => u.Email == emailGerant);
                    if (emailExists)
                    {
                        Console.WriteLine($"?? Un utilisateur avec l'email '{emailGerant}' existe d�j�. " +
                                        $"Agent gérant non cr�� pour l'�cole '{societe.Nom}'.");
                        return;
                    }
                }

                // 1?? CR�ER L'AGENT MANAGER G�N�RAL
                string nomCompletResponsable = societe.NomCompletResponsable?.Trim() ?? "Manager General";

                var managerAgent = new Agent
                {
                    NomComplet = nomCompletResponsable,
                    Genre = societe.GenreResponsable ?? "Masculin",
                    DateNaissance = DateTime.Now.AddYears(-35), // Age par d�faut : 35 ans
                    TelephoneAgent = societe.Telephone,
                    EmailAgent = emailGerant,
                    Statut = true,
                    EtatCivil = "Mari�",
                    Fonction = "Manager G�n�ral", // ? Fonction Manager G�n�ral de l'�cole
                    RoleAgent = "Administrateur",
                    IdSociete = societe.IdSociete,
                    // Note: L'adresse n'est plus copiée car Agent n'hérite plus de Adresse
                    // L'agent peut avoir son AdresseResidence défini séparément si nécessaire
                    DateCreation = DateTime.Now
                };

                // G�n�rer le matricule pour l'agent manager
                string matricule = await GenerateMatriculeManagerGeneral(societe);
                managerAgent.Matricule = matricule;

                _context.Agents.Add(managerAgent);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"? Agent Manager G�n�ral cr�� : {nomCompletResponsable} - Matricule: {matricule}");

                // 2?? CR�ER L'UTILISATEUR LI� � CET AGENT
                await CreateDefaultAdminUserForAgentAsync(managerAgent, societe);
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire �chouer la cr�ation de l'�cole
                Console.WriteLine($"? Erreur lors de la cr�ation du gérant/admin par d�faut: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// G�n�re un matricule unique pour le Manager G�n�ral (Agent)
        /// Format: [NAT][Ann�e(2)]-[GUID(6)]
        /// </summary>
        private async Task<string> GenerateMatriculeManagerGeneral(Societe societe)
        {
            string matricule;
            
            do
            {
                // Pr�fixe national pour tous les agents
                string annee = DateTime.Now.Year.ToString().Substring(2);
                string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = $"NAT{annee}-{guid}";
                
            } while (await _context.Agents.AnyAsync(a => a.Matricule == matricule));
            
            return matricule;
        }

        /// <summary>
        /// Cr�e un compte Utilisateur Admin li� � l'Agent Manager G�n�ral
        /// </summary>
        private async Task CreateDefaultAdminUserForAgentAsync(Agent managerAgent, Societe societe)
        {
            try
            {
                // R�cup�rer le r�le Admin
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");
                if (adminRole == null)
                {
                    // Cr�er le r�le Admin s'il n'existe pas
                    adminRole = new Role
                    {
                        Nom = "Admin",
                        DateCreation = DateTime.Now,
                        Statut = true
                    };
                    _context.Roles.Add(adminRole);
                    await _context.SaveChangesAsync();
                }

                string emailAdmin = managerAgent.EmailAgent ?? "";
                string nomComplet = managerAgent.NomComplet ?? "Manager Général";
                
                // ? V�rification finale de l'email (double s�curit�)
                if (!string.IsNullOrEmpty(emailAdmin))
                {
                    var emailExists = await _context.Utilisateurs.AnyAsync(u => u.Email == emailAdmin);
                    if (emailExists)
                    {
                        Console.WriteLine($"?? Email '{emailAdmin}' d�j� utilis�. Utilisateur admin non cr��.");
                        return;
                    }
                }
                
                // ? G�n�rer un username unique
                string defaultUsername = await GenerateUniqueUsernameAsync(nomComplet);
                
                // Mot de passe par d�faut
                string motDePasseParDefaut = "Admin";
                
                // Cr�er l'utilisateur Admin li� � l'agent Manager G�n�ral
                var adminUser = new Utilisateur
                {
                    IdAgent = managerAgent.IdAgent, // ? LIEN AVEC L'AGENT MANAGER G�N�RAL
                    ReferenceUtilisateur = Guid.NewGuid(),
                    NomComplet = managerAgent.NomComplet,
                    Email = emailAdmin,
                    DefaultUsername = defaultUsername,
                    Telephone = managerAgent.TelephoneAgent,
                    PhotoUrl = managerAgent.PhotoUrl,
                    DateNaissance = managerAgent.DateNaissance,
                    Genre = managerAgent.Genre,
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut),
                    Statut = true,
                    DateCreation = DateTime.Now,
                    IsConnecte = false,
                    DoitChangerMotDePasse = true, // ? Doit changer le mot de passe � la premi�re connexion
                    IdRole = adminRole.IdRole,
                    IdSociete = societe.IdSociete
                    // Note: L'adresse de l'agent n'est plus copiée car Agent n'hérite plus de Adresse
                    // L'utilisateur peut avoir sa propre adresse via les champs hérités de Adresse
                };

                _context.Utilisateurs.Add(adminUser);
                await _context.SaveChangesAsync();
                
                // ✅ Créer aussi l’entrée UserRole pour activer le rôle côté authentification
                var userRole = new UserRole
                {
                    IdUtilisateur = adminUser.IdUtilisateur,
                    IdRole = adminRole.IdRole,
                    IsPrimary = true,
                    Statut = true,
                    DateAttribution = DateTime.Now
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"? Utilisateur Admin cr�� pour le Manager G�n�ral '{nomComplet}' - Email: {emailAdmin}");
                
                // Envoyer l'email de bienvenue (si email fourni)
                if (!string.IsNullOrWhiteSpace(emailAdmin))
                {
                    string nomSociete = societe.Nom ?? "Kenergie";
                    
                    // Envoi asynchrone (ne bloque pas si �chec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendWelcomeEmailAsync(
                                emailAdmin,
                                nomComplet,
                                defaultUsername,
                                managerAgent.TelephoneAgent ?? "",
                                motDePasseParDefaut,
                                "Manager G�n�ral/Administrateur",
                                nomSociete,
                                managerAgent.Genre,
                                "Manager G�n�ral", // Fonction
                                managerAgent.Matricule // Matricule
                            );
                            
                            Console.WriteLine($"?? Email de bienvenue envoy� au Manager G�n�ral : {emailAdmin}");
                        }
                        catch (Exception emailEx)
                        {
                            Console.WriteLine($"?? �chec de l'envoi de l'email � {emailAdmin}: {emailEx.Message}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"?? Aucun email fourni pour le Manager G�n�ral '{nomComplet}'.");
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire �chouer la cr�ation de l'�cole
                Console.WriteLine($"? Erreur lors de la cr�ation de l'utilisateur Admin pour l'agent Manager G�n�ral: {ex.Message}");
            }
        }

        /// <summary>
        /// ? AM�LIORATION : G�n�re un nom d'utilisateur UNIQUE avec v�rification en boucle
        /// Format: [NomResponsable][NombreAleatoire]
        /// Exemple: "Peter Tendayo" ? "PeterTendayo123"
        /// Garantit l'unicit� en v�rifiant dans la base de donn�es
        /// </summary>
        private async Task<string> GenerateUniqueUsernameAsync(string nomComplet)
        {
            if (string.IsNullOrWhiteSpace(nomComplet))
            {
                nomComplet = "Admin";
            }
            
            // Supprimer les espaces et les caract�res sp�ciaux
            string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
            
            // Limiter � 20 caract�res pour le nom de base
            if (baseUsername.Length > 20)
            {
                baseUsername = baseUsername.Substring(0, 20);
            }
            
            string username;
            int attempts = 0;
            int maxAttempts = 100; // Limite de s�curit� pour �viter une boucle infinie
            
            do
            {
                // G�n�rer un nombre al�atoire entre 1 et 9999 (plus large pour r�duire les collisions)
                Random random = new Random(Guid.NewGuid().GetHashCode()); // Seed unique pour meilleure randomisation
                int randomNumber = random.Next(1, 10000);
                
                // Combiner le nom de base avec le nombre al�atoire
                username = $"{baseUsername}{randomNumber}";
                
                attempts++;
                
                // V�rifier l'unicit� dans la base de donn�es
                var usernameExists = await _context.Utilisateurs.AnyAsync(u => u.DefaultUsername == username);
                
                if (!usernameExists)
                {
                    Console.WriteLine($"? Username unique g�n�r�: {username} (tentative {attempts})");
                    break; // Username unique trouv� !
                }
                
                if (attempts >= maxAttempts)
                {
                    // Si on a d�pass� le nombre max de tentatives, ajouter un GUID partiel
                    string guidSuffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                    username = $"{baseUsername}{guidSuffix}";
                    Console.WriteLine($"?? Max tentatives atteint. Username avec GUID g�n�r�: {username}");
                    break;
                }
                
            } while (true);
            
            return username;
        }

        /// <summary>
        /// [DEPRECATED] Ancienne m�thode sans v�rification d'unicit� - conserv�e pour r�f�rence
        /// Utilisez GenerateUniqueUsernameAsync() � la place
        /// </summary>
        [Obsolete("Utilisez GenerateUniqueUsernameAsync() pour garantir l'unicit�")]
        private string GenerateUsernameFromName(string nomComplet)
        {
            if (string.IsNullOrWhiteSpace(nomComplet))
            {
                nomComplet = "Admin";
            }
            
            // Supprimer les espaces et les caract�res sp�ciaux
            string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
            
            // Limiter � 20 caract�res pour le nom de base
            if (baseUsername.Length > 20)
            {
                baseUsername = baseUsername.Substring(0, 20);
            }
            
            // G�n�rer un nombre al�atoire entre 1 et 999
            Random random = new Random();
            int randomNumber = random.Next(1, 1000);
            
            // Combiner le nom de base avec le nombre al�atoire
            string username = $"{baseUsername}{randomNumber}";
            
            return username;
        }

        // ? SOFT DELETE: Toggle le statut d'une �cole (actif <-> inactif)
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var societe = await _context.Societes.FindAsync(id);
            if (societe == null)
                return false;

            societe.Statut = societe.Statut != true;
            await _context.SaveChangesAsync();
            return true;
        }
        
        // ? SOFT DELETE: D�finir une valeur sp�cifique pour le statut d'une �cole
        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            var societe = await _context.Societes.FindAsync(id);
            if (societe == null)
                return false;

            societe.Statut = statut;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
