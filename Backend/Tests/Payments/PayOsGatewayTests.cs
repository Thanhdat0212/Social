using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Marketplace.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Payments;

public class PayOsGatewayTests
{
    private const string ChecksumKey = "test_payos_checksum_secret_key_123456789";
    private readonly PayOsPaymentGateway _gateway;

    public PayOsGatewayTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PAYOS_CLIENT_ID"] = "test-client-id",
                ["PAYOS_API_KEY"] = "test-api-key",
                ["PAYOS_CHECKSUM_KEY"] = ChecksumKey
            })
            .Build();

        _gateway = new PayOsPaymentGateway(
            config,
            new HttpClient(),
            NullLogger<PayOsPaymentGateway>.Instance);
    }

    [Fact]
    public void VerifyWebhookSignature_WhenSignatureMatchesHmacSha256_ShouldReturnTrue()
    {
        // Arrange
        // Thuộc tính trong data được sắp xếp theo A-Z: amount, description, orderCode
        const string rawDataSign = "amount=200000&description=Don Hang CK123&orderCode=12345678";
        var expectedSig = ComputeHmacSha256(rawDataSign, ChecksumKey);

        var webhookPayload = JsonSerializer.Serialize(new
        {
            code = "00",
            desc = "Success",
            data = new
            {
                orderCode = 12345678,
                amount = 200000,
                description = "Don Hang CK123"
            },
            signature = expectedSig
        });

        // Act
        var isValid = _gateway.VerifyWebhookSignature(webhookPayload, expectedSig);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyWebhookSignature_WhenPayloadAmountTampered_ShouldReturnFalse()
    {
        // Arrange
        const string rawDataSign = "amount=200000&description=Don Hang CK123&orderCode=12345678";
        var originalSig = ComputeHmacSha256(rawDataSign, ChecksumKey);

        // Kẻ gian sửa đổi số tiền từ 200,000 xuống 20,000 nhưng giữ nguyên signature cũ
        var tamperedPayload = JsonSerializer.Serialize(new
        {
            code = "00",
            desc = "Success",
            data = new
            {
                orderCode = 12345678,
                amount = 20000, // Đã bị can thiệp
                description = "Don Hang CK123"
            },
            signature = originalSig
        });

        // Act
        var isValid = _gateway.VerifyWebhookSignature(tamperedPayload, originalSig);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyWebhookSignature_WhenSignatureIsInvalid_ShouldReturnFalse()
    {
        // Arrange
        var webhookPayload = JsonSerializer.Serialize(new
        {
            code = "00",
            desc = "Success",
            data = new
            {
                orderCode = 99999,
                amount = 50000
            },
            signature = "completely_invalid_forged_signature_hex"
        });

        // Act
        var isValid = _gateway.VerifyWebhookSignature(webhookPayload, "completely_invalid_forged_signature_hex");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ParseWebhookTransactionId_WhenOrderCodePresent_ShouldExtractSuccessfully()
    {
        // Arrange
        const long expectedOrderCode = 1729001234567;
        var webhookPayload = JsonSerializer.Serialize(new
        {
            data = new
            {
                orderCode = expectedOrderCode,
                amount = 100000
            }
        });

        // Act
        var extractedId = _gateway.ParseWebhookTransactionId(webhookPayload);

        // Assert
        extractedId.Should().Be(expectedOrderCode.ToString());
    }

    [Fact]
    public void ParseWebhookTransactionId_WhenJsonMalformed_ShouldReturnNullGracefully()
    {
        // Arrange
        const string malformedJson = "{ invalid-json-payload-broken }";

        // Act
        var extractedId = _gateway.ParseWebhookTransactionId(malformedJson);

        // Assert
        extractedId.Should().BeNull();
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

