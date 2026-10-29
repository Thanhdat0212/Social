using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.Interfaces.Marketplace.Payments;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Payments;

public sealed class PayOsPaymentGateway : IPaymentGateway
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOsPaymentGateway> _logger;

    private readonly string _clientId;
    private readonly string _apiKey;
    private readonly string _checksumKey;
    private readonly string _frontendBaseUrl;

    public PayOsPaymentGateway(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<PayOsPaymentGateway> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;

        _clientId = CleanConfigValue(_configuration["PAYOS_CLIENT_ID"]
            ?? _configuration["PayOS:ClientId"]
            ?? _configuration["PayOs__ClientId"]
            ?? Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID"));

        _apiKey = CleanConfigValue(_configuration["PAYOS_API_KEY"]
            ?? _configuration["PayOS:ApiKey"]
            ?? _configuration["PayOs__ApiKey"]
            ?? Environment.GetEnvironmentVariable("PAYOS_API_KEY"));

        _checksumKey = CleanConfigValue(_configuration["PAYOS_CHECKSUM_KEY"]
            ?? _configuration["PayOS:ChecksumKey"]
            ?? _configuration["PayOs__ChecksumKey"]
            ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY"));

        _frontendBaseUrl = CleanConfigValue(_configuration["App:FrontendBaseUrl"]
            ?? _configuration["App__FrontendBaseUrl"]
            ?? "http://localhost:5173").TrimEnd('/');

        if (!string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogInformation("Khởi tạo PayOsPaymentGateway với ClientId: {ClientId}", _clientId[..Math.Min(8, _clientId.Length)] + "...");
        }
        else
        {
            _logger.LogWarning("Chưa tìm thấy cấu hình Key của PayOS trong .env hoặc appsettings.");
        }
    }

    private static string CleanConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.Trim();
        if ((trimmed.StartsWith('"') && trimmed.EndsWith('"')) ||
            (trimmed.StartsWith('\'') && trimmed.EndsWith('\'')))
        {
            trimmed = trimmed[1..^1].Trim();
        }
        return trimmed;
    }

    public PaymentProvider Provider => PaymentProvider.PayOs;

    public async Task<GatewayPaymentLinkResult> CreatePaymentLinkAsync(
        Guid checkoutId,
        string checkoutNo,
        long amount,
        string description,
        CancellationToken cancellationToken = default)
    {
        var orderCode = GenerateOrderCode();
        var orderCodeStr = orderCode.ToString();

        // PayOS giới hạn mô tả tối đa 25 ký tự không dấu hoặc mã đơn
        var sanitizedDesc = $"Don {checkoutNo}";
        if (sanitizedDesc.Length > 25) sanitizedDesc = sanitizedDesc[..25];

        var cancelUrl = $"{_frontendBaseUrl}/checkout/{checkoutId}?status=cancelled";
        var returnUrl = $"{_frontendBaseUrl}/checkout/{checkoutId}?status=success";

        if (!string.IsNullOrWhiteSpace(_clientId) && !string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_checksumKey))
        {
            string lastResponseContent = string.Empty;
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    orderCode = GenerateOrderCode() + (attempt > 1 ? Random.Shared.Next(100, 999) : 0);
                    orderCodeStr = orderCode.ToString();

                    // Chuỗi dữ liệu tính chữ ký HMAC-SHA256 theo thứ tự alphabet: amount -> cancelUrl -> description -> orderCode -> returnUrl
                    var signData = $"amount={amount}&cancelUrl={cancelUrl}&description={sanitizedDesc}&orderCode={orderCode}&returnUrl={returnUrl}";
                    var signature = ComputeHmacSha256(signData, _checksumKey);

                    var payload = new
                    {
                        orderCode = orderCode,
                        amount = amount,
                        description = sanitizedDesc,
                        cancelUrl = cancelUrl,
                        returnUrl = returnUrl,
                        signature = signature
                    };

                    using var request = new HttpRequestMessage(HttpMethod.Post, "https://api-merchant.payos.vn/v2/payment-requests")
                    {
                        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                    };
                    request.Headers.Add("x-client-id", _clientId);
                    request.Headers.Add("x-api-key", _apiKey);

                    var response = await _httpClient.SendAsync(request, cancellationToken);
                    lastResponseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(lastResponseContent);
                        var root = doc.RootElement;
                        var code = root.TryGetProperty("code", out var cElem) ? cElem.GetString() : null;

                        if (code == "00" && root.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object)
                        {
                            var checkoutUrl = dataElem.TryGetProperty("checkoutUrl", out var cUrl) ? cUrl.GetString() : null;
                            var qrCode = dataElem.TryGetProperty("qrCode", out var qr) ? qr.GetString() : null;
                            var accNumber = dataElem.TryGetProperty("accountNumber", out var aNum) ? aNum.GetString() : null;
                            var accName = dataElem.TryGetProperty("accountName", out var aName) ? aName.GetString() : null;
                            var payOsDesc = dataElem.TryGetProperty("description", out var dProp) ? dProp.GetString() : sanitizedDesc;

                            // Chuyển chuỗi EMVCo VietQR của PayOS thành URL ảnh QR thật nếu chưa có prefix http
                            string? qrImageUrl = null;
                            if (!string.IsNullOrWhiteSpace(qrCode))
                            {
                                qrImageUrl = qrCode.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                    ? qrCode
                                    : $"https://api.qrserver.com/v1/create-qr-code/?size=350x350&data={Uri.EscapeDataString(qrCode)}";
                            }

                            _logger.LogInformation("Tạo link thanh toán PayOS thành công cho đơn {OrderCode}", orderCode);

                            return new GatewayPaymentLinkResult
                            {
                                ProviderTxnId = orderCodeStr,
                                PaymentUrl = checkoutUrl,
                                QrCodeUrl = qrImageUrl,
                                AccountNumber = accNumber,
                                AccountName = accName,
                                Description = payOsDesc ?? sanitizedDesc,
                                PaymentCode = payOsDesc ?? orderCodeStr
                            };
                        }

                        // Nếu PayOS trả về lỗi kèm mã lỗi cụ thể (ví dụ 20: mô tả quá dài, 231: đơn đã tồn tại)
                        var desc = root.TryGetProperty("desc", out var dElem) ? dElem.GetString() : lastResponseContent;
                        if (code == "231" && attempt < 2)
                        {
                            _logger.LogWarning("PayOS báo đơn {OrderCode} đã tồn tại. Thử lại với orderCode mới...", orderCode);
                            continue;
                        }

                        _logger.LogError("PayOS trả về mã lỗi {Code}: {Desc}", code, desc);
                        throw new InvalidOperationException($"Lỗi từ cổng PayOS ({code}): {desc}");
                    }

                    // Nếu PayOS báo đơn đã tồn tại (code 231) -> thử lại với orderCode mới
                    if (lastResponseContent.Contains("\"231\"") && attempt < 2)
                    {
                        _logger.LogWarning("PayOS báo đơn {OrderCode} đã tồn tại. Thử lại với orderCode mới...", orderCode);
                        continue;
                    }

                    _logger.LogError("Gọi API PayOS thất bại ({StatusCode}): {Response}", response.StatusCode, lastResponseContent);
                    throw new InvalidOperationException($"Lỗi từ cổng PayOS: {lastResponseContent}");
                }
                catch (Exception ex) when (ex is not InvalidOperationException)
                {
                    _logger.LogError(ex, "Lỗi kết nối tới cổng thanh toán PayOS.");
                    throw new InvalidOperationException($"Không thể kết nối đến cổng thanh toán PayOS: {ex.Message}", ex);
                }
            }

            throw new InvalidOperationException($"Lỗi từ cổng PayOS: {lastResponseContent}");
        }

        throw new InvalidOperationException("Chưa cấu hình đầy đủ PAYOS_CLIENT_ID, PAYOS_API_KEY, PAYOS_CHECKSUM_KEY trong file .env.");
    }

    public async Task<GatewayVerifyResult> VerifyPaymentAsync(string providerTxnId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return new GatewayVerifyResult { IsPaid = false, Status = "PENDING", TransactionReference = providerTxnId };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api-merchant.payos.vn/v2/payment-requests/{providerTxnId}");
            request.Headers.Add("x-client-id", _clientId);
            request.Headers.Add("x-api-key", _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object)
                {
                    var status = dataElem.TryGetProperty("status", out var s) ? s.GetString() : null;
                    var amountPaid = dataElem.TryGetProperty("amountPaid", out var ap) ? ap.GetInt64() : 0;
                    var isPaid = string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase);

                    return new GatewayVerifyResult
                    {
                        IsPaid = isPaid,
                        AmountPaid = amountPaid,
                        Status = status ?? string.Empty,
                        PaidAt = isPaid ? DateTime.UtcNow : null,
                        TransactionReference = providerTxnId
                    };
                }
            }

            _logger.LogWarning("Verify PayOS thất bại ({StatusCode}): {Content}", response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API verification PayOS cho mã {TxnId}", providerTxnId);
        }

        return new GatewayVerifyResult { IsPaid = false, Status = "PENDING", TransactionReference = providerTxnId };
    }

    public bool VerifyWebhookSignature(string webhookBody, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature) && !webhookBody.Contains("\"signature\"")) return false;

        try
        {
            using var doc = JsonDocument.Parse(webhookBody);
            var root = doc.RootElement;

            var actualSig = signature;
            if (string.IsNullOrWhiteSpace(actualSig) && root.TryGetProperty("signature", out var sigElem))
            {
                actualSig = sigElem.GetString() ?? string.Empty;
            }

            if (!root.TryGetProperty("data", out var dataElem) || dataElem.ValueKind != JsonValueKind.Object) return false;

            // Thu thập và sắp xếp các thuộc tính trong data theo thứ tự bảng chữ cái A-Z
            var dict = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var prop in dataElem.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "",
                    _ => prop.Value.GetRawText()
                };
            }

            var signData = string.Join("&", dict.Select(kv => $"{kv.Key}={kv.Value}"));
            var computedSig = ComputeHmacSha256(signData, _checksumKey);

            return string.Equals(computedSig, actualSig.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kiểm tra chữ ký Webhook PayOS.");
            return false;
        }
    }

    public string? ParseWebhookTransactionId(string webhookBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(webhookBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Object)
            {
                if (dataElem.TryGetProperty("orderCode", out var orderCodeElem))
                {
                    return orderCodeElem.ValueKind == JsonValueKind.Number
                        ? orderCodeElem.GetInt64().ToString()
                        : (orderCodeElem.GetString() ?? orderCodeElem.GetRawText());
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy orderCode từ Webhook PayOS.");
        }
        return null;
    }

    private static long GenerateOrderCode()
    {
        // Unix timestamp (mili-giây) đảm bảo số nguyên int64 duy nhất tăng dần và nhỏ hơn 9007199254740991
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
