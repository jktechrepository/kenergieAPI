namespace Kenergie.Helpers
{
    /// <summary>
    /// Interprétation des statuts transaction FlexPay (check API / callback).
    /// </summary>
    public static class FlexPayTransactionStatusHelper
    {
        private static readonly HashSet<string> ConfirmedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "0", "SUCCESS", "SUCCESSFUL", "COMPLETED", "PAID", "APPROVED", "OK"
        };

        private static readonly HashSet<string> PendingStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "1", "PENDING", "INITIATED", "IN_PROGRESS", "PROCESSING", "WAITING", "SENT"
        };

        private static readonly HashSet<string> FailedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "2", "FAILED", "DECLINED", "CANCELLED", "CANCELED", "REJECTED", "ERROR"
        };

        public static bool IsConfirmed(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;
            return ConfirmedStatuses.Contains(status.Trim());
        }

        public static bool IsPending(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;
            return PendingStatuses.Contains(status.Trim());
        }

        public static bool IsFailed(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;
            return FailedStatuses.Contains(status.Trim());
        }
    }
}
