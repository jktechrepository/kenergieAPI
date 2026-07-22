namespace Kenergie.Helpers
{
    /// <summary>
    /// Normalise le token marchand FlexPay : JWT seul, sans préfixe Bearer.
    /// </summary>
    public static class FlexPayTokenHelper
    {
        public static string Normalize(string? apiToken)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                return string.Empty;

            var token = apiToken.Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token["Bearer ".Length..].Trim();

            return token;
        }
    }
}
