using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kenergie.Data;
using Kenergie.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services.Notifications
{
    public class NotificationDispatcher : INotificationDispatcher
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<NotificationDispatcher> _logger;

        public NotificationDispatcher(
            KenergieDbContext context,
            ILogger<NotificationDispatcher> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Méthodes de préparation de notifications supprimées car les modèles associés ont été supprimés
    }
}

