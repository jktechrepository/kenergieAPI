using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using KenergieAPI.Services.Repositories;
using Kenergie.Models;

namespace KenergieAPI.Services
{
    /// <summary>
    /// Service pour envoyer des notifications push via Firebase Cloud Messaging
    /// </summary>
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly IUserDeviceRepository _userDeviceRepository;
        private readonly ILogger<FirebaseNotificationService> _logger;
        private static bool _firebaseInitialized = false;
        private static readonly object _lock = new object();

        public FirebaseNotificationService(
            IUserDeviceRepository userDeviceRepository,
            ILogger<FirebaseNotificationService> logger)
        {
            _userDeviceRepository = userDeviceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Initialise Firebase Admin SDK avec les credentials (appelé une seule fois au démarrage)
        /// </summary>
        public static void InitializeFirebase(string credentialsPath)
        {
            if (!_firebaseInitialized)
            {
                lock (_lock)
                {
                    if (!_firebaseInitialized && FirebaseApp.DefaultInstance == null)
                    {
                        try
                        {
                            FirebaseApp.Create(new AppOptions
                            {
                                Credential = GoogleCredential.FromFile(credentialsPath)
                            });
                            
                            _firebaseInitialized = true;
                            Console.WriteLine("✅ Firebase Admin SDK initialisé avec succès");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Erreur lors de l'initialisation de Firebase Admin SDK: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Envoie une notification à un utilisateur spécifique (tous ses devices actifs)
        /// </summary>
        public async Task<bool> EnvoyerNotificationAUtilisateurAsync(
            int idUtilisateur,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                // Vérifier que Firebase est initialisé
                if (FirebaseApp.DefaultInstance == null)
                {
                    _logger.LogError($"❌ Firebase n'est pas initialisé. Impossible d'envoyer la notification à l'utilisateur {idUtilisateur}");
                    return false;
                }

                _logger.LogInformation($"📲 Tentative d'envoi de notification push à l'utilisateur {idUtilisateur}");

                // Récupérer tous les tokens actifs de l'utilisateur
                var tokens = await _userDeviceRepository.GetActiveTokensByUtilisateurIdAsync(idUtilisateur);

                // ✅ FIX: Vérifier null avant Any()
                if (tokens == null || !tokens.Any())
                {
                    _logger.LogWarning($"⚠️ Aucun token FCM actif trouvé pour l'utilisateur {idUtilisateur}");
                    return false;
                }

                _logger.LogInformation($"📱 {tokens.Count()} token(s) FCM trouvé(s) pour l'utilisateur {idUtilisateur}");

                // Créer le message
                var message = new MulticastMessage
                {
                    Tokens = tokens.ToList(),
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps
                    },
                    Data = donnees ?? new Dictionary<string, string>()
                };

                // Envoyer la notification
                _logger.LogInformation($"📤 Envoi de la notification Firebase à {tokens.Count()} device(s)...");
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                _logger.LogInformation($"✅ Notification envoyée à l'utilisateur {idUtilisateur}. Succès: {response.SuccessCount}/{tokens.Count()}, Échecs: {response.FailureCount}");

                // Désactiver les tokens invalides
                if (response.FailureCount > 0)
                {
                    _logger.LogWarning($"⚠️ {response.FailureCount} échec(s) détecté(s) pour l'utilisateur {idUtilisateur}");
                    await DesactiverTokensInvalidesAsync(response, tokens.ToList());
                }

                return response.SuccessCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification à l'utilisateur {idUtilisateur}");
                return false;
            }
        }

        /// <summary>
        /// Envoie une notification à tous les utilisateurs ayant un rôle spécifique
        /// </summary>
        public async Task<int> EnvoyerNotificationParRoleAsync(
            int idRole,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var tokens = await _userDeviceRepository.GetActiveTokensByRoleAsync(idRole);

                if (!tokens.Any())
                {
                    _logger.LogWarning($"Aucun token FCM actif trouvé pour le rôle {idRole}");
                    return 0;
                }

                var message = new MulticastMessage
                {
                            Tokens = tokens.ToList(),
                            Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps
                    },
                    Data = donnees ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                _logger.LogInformation($"Notification envoyée au rôle {idRole}. Succès: {response.SuccessCount}/{tokens.Count()}");

                await DesactiverTokensInvalidesAsync(response, tokens.ToList());

                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification au rôle {idRole}");
                return 0;
            }
        }

        /// <summary>
        /// Envoie une notification à tous les utilisateurs d'une école
        /// </summary>
        public async Task<int> EnvoyerNotificationParSocieteAsync(
            int idSociete,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var tokens = await _userDeviceRepository.GetActiveTokensBySocieteAsync(idSociete);

                if (!tokens.Any())
                {
                    _logger.LogWarning($"Aucun token FCM actif trouvé pour l'école {idSociete}");
                    return 0;
                }

                var message = new MulticastMessage
                {
                            Tokens = tokens.ToList(),
                            Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps
                    },
                    Data = donnees ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                _logger.LogInformation($"Notification envoyée à l'école {idSociete}. Succès: {response.SuccessCount}/{tokens.Count()}");

                await DesactiverTokensInvalidesAsync(response, tokens.ToList());

                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification à l'école {idSociete}");
                return 0;
            }
        }

        /// <summary>
        /// Envoie une notification à tous les utilisateurs d'une classe
        /// </summary>
        public async Task<int> EnvoyerNotificationParClasseAsync(
            int idClasse,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                var tokens = await _userDeviceRepository.GetActiveTokensByClasseAsync(idClasse);

                if (!tokens.Any())
                {
                    _logger.LogWarning($"Aucun token FCM actif trouvé pour la classe {idClasse}");
                    return 0;
                }

                var message = new MulticastMessage
                {
                            Tokens = tokens.ToList(),
                            Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps
                    },
                    Data = donnees ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

                _logger.LogInformation($"Notification envoyée à la classe {idClasse}. Succès: {response.SuccessCount}/{tokens.Count()}");

                await DesactiverTokensInvalidesAsync(response, tokens.ToList());

                return response.SuccessCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification à la classe {idClasse}");
                return 0;
            }
        }

        /// <summary>
        /// Envoie une notification à un token FCM spécifique
        /// </summary>
        public async Task<bool> EnvoyerNotificationATokenAsync(
            string fcmToken,
            string titre,
            string corps,
            Dictionary<string, string>? donnees = null)
        {
            try
            {
                        var message = new FirebaseAdmin.Messaging.Message
                {
                    Token = fcmToken,
                            Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = titre,
                        Body = corps
                    },
                    Data = donnees ?? new Dictionary<string, string>()
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                _logger.LogInformation($"Notification envoyée au token {fcmToken}. Response: {response}");

                return !string.IsNullOrEmpty(response);
            }
            catch (FirebaseMessagingException fmEx)
            {
                _logger.LogError(fmEx, $"Erreur Firebase lors de l'envoi au token {fcmToken}");
                
                // Si le token est invalide, le désactiver
                if (fmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                    fmEx.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    await _userDeviceRepository.DeleteByFcmTokenAsync(fcmToken);
                    _logger.LogInformation($"Token FCM invalide supprimé: {fcmToken}");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification au token {fcmToken}");
                return false;
            }
        }

        /// <summary>
        /// Envoie une notification push personnalisée avec des paramètres avancés
        /// </summary>
        public async Task<bool> EnvoyerNotificationAvanceeAsync(
            string fcmToken,
            string titre,
            string corps,
            string? imageUrl = null,
            string? clickAction = null,
            Dictionary<string, string>? donnees = null,
            string? sound = null,
            string? badge = null)
        {
            try
            {
                var notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = titre,
                    Body = corps,
                    ImageUrl = imageUrl
                };

                        var message = new FirebaseAdmin.Messaging.Message
                {
                    Token = fcmToken,
                    Notification = notification,
                    Data = donnees ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = sound ?? "default",
                            ClickAction = clickAction
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = sound ?? "default",
                            Badge = string.IsNullOrEmpty(badge) ? null : int.Parse(badge)
                        }
                    },
                    Webpush = new WebpushConfig
                    {
                        Notification = new WebpushNotification
                        {
                            Title = titre,
                            Body = corps,
                            Icon = imageUrl
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                _logger.LogInformation($"Notification avancée envoyée. Response: {response}");

                return !string.IsNullOrEmpty(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de notification avancée");
                return false;
            }
        }

        /// <summary>
        /// Désactive les tokens FCM qui sont invalides ou expirés
        /// </summary>
        private async Task DesactiverTokensInvalidesAsync(BatchResponse response, List<string> tokens)
        {
            for (int i = 0; i < response.Responses.Count; i++)
            {
                var sendResponse = response.Responses[i];
                if (!sendResponse.IsSuccess)
                {
                    var exception = sendResponse.Exception;
                    if (exception is FirebaseMessagingException fmEx)
                    {
                        if (fmEx.MessagingErrorCode == MessagingErrorCode.InvalidArgument ||
                            fmEx.MessagingErrorCode == MessagingErrorCode.Unregistered)
                        {
                            await _userDeviceRepository.DeleteByFcmTokenAsync(tokens[i]);
                            _logger.LogInformation($"Token FCM invalide supprimé: {tokens[i]}");
                        }
                    }
                }
            }
        }
    }
}
