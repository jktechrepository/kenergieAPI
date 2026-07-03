using Kenergie.Data;
using Kenergie.Models;
using KenergieAPI.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KenergieAPI.Services
{
    /// <summary>
    /// Service pour la gestion des appareils utilisateurs (FCM tokens)
    /// </summary>
    public class UserDeviceService : IUserDeviceRepository
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<UserDeviceService> _logger;

        public UserDeviceService(KenergieDbContext context, ILogger<UserDeviceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDevice>> GetAllAsync()
        {
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .Where(ud => ud.Statut == true)
                .OrderByDescending(ud => ud.DateEnregistrement)
                .ToListAsync();
        }

        public async Task<UserDevice?> GetByIdAsync(int id)
        {
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .FirstOrDefaultAsync(ud => ud.IdUserDevice == id);
        }

        public async Task<UserDevice?> GetByFcmTokenAsync(string fcmToken)
        {
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .FirstOrDefaultAsync(ud => ud.FcmToken == fcmToken);
        }

        public async Task<IEnumerable<UserDevice>> GetByUtilisateurIdAsync(int idUtilisateur)
        {
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .Where(ud => ud.IdUtilisateur == idUtilisateur && ud.Statut == true)
                .OrderByDescending(ud => ud.DateDerniereUtilisation)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetActiveTokensByUtilisateurIdAsync(int idUtilisateur)
        {
            return await _context.UserDevices
                .Where(ud => ud.IdUtilisateur == idUtilisateur && ud.Statut == true)
                .Select(ud => ud.FcmToken)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetActiveTokensByRoleAsync(int idRole)
        {
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .Where(ud => ud.Utilisateur.IdRole == idRole && ud.Statut == true)
                .Select(ud => ud.FcmToken)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetActiveTokensBySocieteAsync(int idSociete)
        {
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .Where(ud => ud.Utilisateur.IdSociete == idSociete && ud.Statut == true)
                .Select(ud => ud.FcmToken)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetActiveTokensByClasseAsync(int idClasse)
        {
            // Pour les élèves d'une classe spécifique
            return await _context.UserDevices
                .Include(ud => ud.Utilisateur)
                .Where(ud => ud.Utilisateur.IdSociete != null && 
                           ud.Utilisateur.IdSociete == idClasse && 
                           ud.Statut == true)
                .Select(ud => ud.FcmToken)
                .ToListAsync();
        }

        public async Task<UserDevice> CreateAsync(UserDevice userDevice)
        {
            userDevice.DateEnregistrement = DateTime.Now;
            userDevice.Statut = true;

            _context.UserDevices.Add(userDevice);
            await _context.SaveChangesAsync();
            return userDevice;
        }

        /// <summary>
        /// Crée ou met à jour un device pour un utilisateur
        /// ✅ CORRIGÉ: Utilise le FCM Token comme identifiant unique principal
        /// ✅ Permet à un utilisateur d'avoir plusieurs devices du même type (ex: 2 téléphones Android)
        /// ✅ Si le token existe déjà, met à jour le device existant (même device qui se reconnecte)
        /// ✅ Si le token n'existe pas, crée un nouveau device (nouveau device)
        /// </summary>
        public async Task<UserDevice> CreateOrUpdateAsync(int idUtilisateur, string fcmToken, string? deviceType = null, string? deviceModel = null, string? osVersion = null)
        {
            // 🚨 VALIDATION: Rejeter les valeurs par défaut "string"
            if (string.IsNullOrWhiteSpace(fcmToken) || fcmToken == "string" || fcmToken == "null")
            {
                throw new ArgumentException("FCM Token invalide", nameof(fcmToken));
            }

            if (string.IsNullOrWhiteSpace(deviceType) || deviceType == "string" || deviceType == "null")
            {
                throw new ArgumentException("Device Type invalide", nameof(deviceType));
            }

            // ✅ CORRIGÉ: Chercher d'abord par FCM Token (identifiant unique par device)
            // Chaque device a un token FCM unique, donc on peut avoir plusieurs devices du même type
            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(ud => ud.FcmToken == fcmToken);

            if (existingDevice != null)
            {
                // Le token existe déjà = c'est le même device qui se reconnecte
                // Mettre à jour les informations du device (peuvent avoir changé)
                existingDevice.IdUtilisateur = idUtilisateur; // Au cas où le device change d'utilisateur
                existingDevice.DeviceType = deviceType ?? existingDevice.DeviceType;
                existingDevice.DeviceModel = deviceModel ?? existingDevice.DeviceModel;
                existingDevice.OsVersion = osVersion ?? existingDevice.OsVersion;
                existingDevice.DateDerniereUtilisation = DateTime.Now;
                existingDevice.Statut = true;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ Device mis à jour (token existant) - UserId: {idUtilisateur}, Token: {fcmToken.Substring(0, Math.Min(20, fcmToken.Length))}...");
                return existingDevice;
            }
            else
            {
                // Le token n'existe pas = nouveau device
                // Créer un nouveau device (même si l'utilisateur a déjà un device du même type)
                var newDevice = new UserDevice
                {
                    IdUtilisateur = idUtilisateur,
                    FcmToken = fcmToken,
                    DeviceType = deviceType,
                    DeviceModel = deviceModel ?? "Unknown",
                    OsVersion = osVersion ?? "Unknown",
                    DateEnregistrement = DateTime.Now,
                    DateDerniereUtilisation = DateTime.Now,
                    Statut = true
                };

                _context.UserDevices.Add(newDevice);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"✅ Nouveau device créé - UserId: {idUtilisateur}, Type: {deviceType}, Model: {deviceModel}, Token: {fcmToken.Substring(0, Math.Min(20, fcmToken.Length))}...");
                return newDevice;
            }
        }

        public async Task<UserDevice?> UpdateAsync(UserDevice userDevice)
        {
            var existingDevice = await _context.UserDevices.FindAsync(userDevice.IdUserDevice);
            if (existingDevice == null)
                return null;

            _context.Entry(existingDevice).CurrentValues.SetValues(userDevice);
            await _context.SaveChangesAsync();
            return existingDevice;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var device = await _context.UserDevices.FindAsync(id);
            if (device == null)
                return false;

            _context.UserDevices.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByFcmTokenAsync(string fcmToken)
        {
            var device = await _context.UserDevices
                .FirstOrDefaultAsync(ud => ud.FcmToken == fcmToken);
            
            if (device == null)
                return false;

            _context.UserDevices.Remove(device);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.UserDevices.AnyAsync(ud => ud.IdUserDevice == id);
        }

        public async Task<bool> ExistsByFcmTokenAsync(string fcmToken)
        {
            return await _context.UserDevices.AnyAsync(ud => ud.FcmToken == fcmToken);
        }
    }
}
