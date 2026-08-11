using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Kenergie.Helpers;
using Kenergie.Models.Configuration;
using Kenergie.Models.DTOs.FlexPay;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kenergie.Services.FlexPay
{
    public interface IFlexPayHttpService
    {
        Task<FlexPayInitResult> InitierMobileMoneyAsync(
            string apiToken,
            string codeMarchand,
            string reference,
            string phone,
            decimal montant,
            string currency,
            string callbackUrl,
            CancellationToken ct = default);

        Task<FlexPayInitResult> InitierCarteAsync(
            string apiToken,
            string codeMarchand,
            string reference,
            decimal montant,
            string currency,
            string description,
            string callbackUrl,
            string approveUrl,
            string cancelUrl,
            string declineUrl,
            CancellationToken ct = default);

        Task<FlexPayCheckResult> VerifierTransactionAsync(
            string apiToken,
            string orderNumber,
            CancellationToken ct = default);
    }

    public class FlexPayHttpService : IFlexPayHttpService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FlexPayOptions _options;
        private readonly ILogger<FlexPayHttpService> _logger;

        public FlexPayHttpService(
            IHttpClientFactory httpClientFactory,
            IOptions<FlexPayOptions> options,
            ILogger<FlexPayHttpService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<FlexPayInitResult> InitierMobileMoneyAsync(
            string apiToken,
            string codeMarchand,
            string reference,
            string phone,
            decimal montant,
            string currency,
            string callbackUrl,
            CancellationToken ct = default)
        {
            var amountStr = FormatAmount(montant, currency);
            var body = new Dictionary<string, string>
            {
                ["merchant"] = codeMarchand,
                ["type"] = "1",
                ["reference"] = reference,
                ["phone"] = phone,
                ["amount"] = amountStr,
                ["currency"] = currency.ToUpperInvariant(),
                ["callbackUrl"] = callbackUrl,
                ["return_url"] = callbackUrl
            };

            return await PostInitAsync(_options.MobileMoneyUrl, apiToken, body, includeAuthHeader: true, ct);
        }

        public async Task<FlexPayInitResult> InitierCarteAsync(
            string apiToken,
            string codeMarchand,
            string reference,
            decimal montant,
            string currency,
            string description,
            string callbackUrl,
            string approveUrl,
            string cancelUrl,
            string declineUrl,
            CancellationToken ct = default)
        {
            var token = FlexPayTokenHelper.Normalize(apiToken);
            object body = new
            {
                authorization = $"Bearer {token}",
                merchant = codeMarchand,
                reference,
                amount = currency.Equals("CDF", StringComparison.OrdinalIgnoreCase)
                    ? (object)(long)Math.Round(montant, 0, MidpointRounding.AwayFromZero)
                    : (object)Math.Round(montant, 2, MidpointRounding.AwayFromZero),
                currency = currency.ToUpperInvariant(),
                description,
                callback_url = callbackUrl,
                approve_url = approveUrl,
                cancel_url = cancelUrl,
                decline_url = declineUrl
            };

            return await PostInitAsync(_options.CardPaymentUrl, token, body, includeAuthHeader: false, ct);
        }

        public async Task<FlexPayCheckResult> VerifierTransactionAsync(
            string apiToken,
            string orderNumber,
            CancellationToken ct = default)
        {
            var token = FlexPayTokenHelper.Normalize(apiToken);
            var client = _httpClientFactory.CreateClient("FlexPay");
            var url = $"{_options.CheckTransactionUrl.TrimEnd('/')}/{orderNumber}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await client.SendAsync(request, ct);
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
                var root = doc.RootElement;

                var code = GetString(root, "code") ?? "1";
                JsonElement tx = default;
                var hasTx = root.TryGetProperty("transaction", out tx);
                var status = hasTx ? GetString(tx, "status") : GetString(root, "status");
                var providerReference = hasTx
                    ? GetString(tx, "providerReference") ?? GetString(tx, "provider_reference")
                    : GetString(root, "providerReference") ?? GetString(root, "provider_reference");

                var isPending = FlexPayTransactionStatusHelper.IsPending(status);
                var isConfirmed = FlexPayTransactionStatusHelper.IsConfirmed(status)
                    || (code == "0"
                        && !isPending
                        && !string.IsNullOrWhiteSpace(providerReference));

                return new FlexPayCheckResult
                {
                    Success = isConfirmed,
                    IsConfirmed = isConfirmed,
                    IsPending = isPending && !isConfirmed,
                    Code = isConfirmed ? "0" : code,
                    TransactionStatus = status,
                    ProviderReference = providerReference,
                    OrderNumber = orderNumber,
                    Reference = GetString(root, "reference") ?? (hasTx ? GetString(tx, "reference") : null),
                    Amount = GetString(root, "amount") ?? (hasTx ? GetString(tx, "amount") : null),
                    Currency = GetString(root, "currency") ?? (hasTx ? GetString(tx, "currency") : null),
                    Message = GetString(root, "message") ?? string.Empty,
                    RawJson = json
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur check FlexPay orderNumber={OrderNumber}", orderNumber);
                return new FlexPayCheckResult
                {
                    Success = false,
                    Code = "1",
                    OrderNumber = orderNumber,
                    Message = ex.Message
                };
            }
        }

        private async Task<FlexPayInitResult> PostInitAsync(
            string url,
            string apiToken,
            object body,
            bool includeAuthHeader,
            CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("FlexPay");
            var json = JsonSerializer.Serialize(body);
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            if (includeAuthHeader)
            {
                var token = FlexPayTokenHelper.Normalize(apiToken);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {
                using var response = await client.SendAsync(request, ct);
                var responseJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(responseJson) ? "{}" : responseJson);
                var root = doc.RootElement;

                var code = GetString(root, "code") ?? "1";
                var accepted = code == "0";
                var paymentUrl = FlexPayUrlHelper.ResolvePaymentUrl(
                    GetString(root, "paymentUrl"),
                    GetString(root, "redirectUrl"),
                    GetString(root, "url"));

                return new FlexPayInitResult
                {
                    Accepted = accepted,
                    Code = code,
                    Message = GetString(root, "message") ?? response.ReasonPhrase ?? string.Empty,
                    OrderNumber = GetString(root, "orderNumber"),
                    PaymentUrl = paymentUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur initiation FlexPay url={Url}", url);
                return new FlexPayInitResult
                {
                    Accepted = false,
                    Code = "1",
                    Message = ex.Message
                };
            }
        }

        private static string FormatAmount(decimal montant, string currency)
        {
            if (currency.Equals("CDF", StringComparison.OrdinalIgnoreCase))
                return ((long)Math.Round(montant, 0, MidpointRounding.AwayFromZero)).ToString();
            return Math.Round(montant, 2, MidpointRounding.AwayFromZero).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string? GetString(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p))
                return null;
            return p.ValueKind switch
            {
                JsonValueKind.String => p.GetString(),
                JsonValueKind.Number => p.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => p.ToString()
            };
        }
    }
}
