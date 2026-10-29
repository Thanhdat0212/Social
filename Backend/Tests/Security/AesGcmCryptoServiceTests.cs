using System.Security.Cryptography;
using FluentAssertions;
using Infrastructure.External.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Security;

public class AesGcmCryptoServiceTests
{
    private readonly AesGcmCryptoService _cryptoService;

    public AesGcmCryptoServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Marketplace:EncryptionKey"] = "SuperSecretKeyForTestingAesGcm256BitLength123456",
                ["Marketplace:HashSalt"] = "TestSalt_1234567890_Unique"
            })
            .Build();

        _cryptoService = new AesGcmCryptoService(config, NullLogger<AesGcmCryptoService>.Instance);
    }

    [Fact]
    public void Encrypt_And_Decrypt_ShouldReturnOriginalPlainText_Roundtrip()
    {
        // Arrange
        const string plainText = "GIFT-CARD-XYZ-9999-SECRET-TOKEN";

        // Act
        var encrypted = _cryptoService.Encrypt(plainText);
        var decrypted = _cryptoService.Decrypt(encrypted);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Encrypt_SamePlainTextTwice_ShouldProduceDifferentCiphertexts_DueToRandomNonce()
    {
        // Arrange
        const string plainText = "REPEATABLE-TEST-STRING";

        // Act
        var cipher1 = _cryptoService.Encrypt(plainText);
        var cipher2 = _cryptoService.Encrypt(plainText);

        // Assert
        cipher1.Should().NotBe(cipher2);
        _cryptoService.Decrypt(cipher1).Should().Be(plainText);
        _cryptoService.Decrypt(cipher2).Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ShouldThrowCryptographicException()
    {
        // Arrange
        const string plainText = "DIGITAL-LICENSE-KEY-ABC-123";
        var encrypted = _cryptoService.Encrypt(plainText);

        var rawBytes = Convert.FromBase64String(encrypted);
        // Tamper with the last byte of ciphertext/tag
        rawBytes[^1] ^= 0xFF;
        var tamperedBase64 = Convert.ToBase64String(rawBytes);

        // Act & Assert
        var act = () => _cryptoService.Decrypt(tamperedBase64);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void ComputeHash_SameInputWithDifferentWhitespace_ShouldProduceIdenticalDeduplicationHash()
    {
        // Arrange
        const string code1 = "STEAM-WALLET-500K";
        const string code2 = "  STEAM-WALLET-500K \t ";

        // Act
        var hash1 = _cryptoService.ComputeHash(code1);
        var hash2 = _cryptoService.ComputeHash(code2);

        // Assert
        hash1.Should().HaveLength(64); // SHA-256 hex string
        hash1.Should().Be(hash2);
    }

    [Theory]
    [InlineData("123", "***")]
    [InlineData("1234", "****")]
    [InlineData("ABCD-1234-EFGH", "******-EFGH")]
    [InlineData("STEAM-WALLET-CODE-9999", "******-9999")]
    public void MaskValue_ShouldHideSecretContentAndRetainLast4Characters(string input, string expected)
    {
        // Act
        var masked = _cryptoService.MaskValue(input);

        // Assert
        masked.Should().Be(expected);
    }
}

