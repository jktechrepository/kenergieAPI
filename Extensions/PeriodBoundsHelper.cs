namespace Kenergie.Extensions
{
    /// <summary>
    /// Bornes de période calendaire pour les agrégations mensuelles.
    /// </summary>
    public static class PeriodBoundsHelper
    {
        public static (DateTime Debut, DateTime Fin) GetMoisCourantBounds()
        {
            var debut = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var fin = debut.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
            return (debut, fin);
        }
    }
}
